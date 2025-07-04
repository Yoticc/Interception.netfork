﻿using Interceptions.Internal;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Interceptions;
public static unsafe partial class Interception
{
    public static int screenX, screenY;
    public static Keyboard* Keyboard;
    public static Mouse* Mouse;
    public static Context Context;
    static long* keyStates = (long*)Marshal.AllocCoTaskMem(0x80) + 0x08;

    static Interception()
    {
        if (Debugger.IsAttached)
            return;

        var resolution = stackalloc int[4];
        GetWindowRect(GetDesktopWindow(), resolution);
        (screenX, screenY) = (resolution[2], resolution[3]);

        Context = Context.Create();
        Context.SetFilter(Filter.All);

        // updater thread stack usage is only 20kb ususally, so we allocate 64kb
        new Thread(KeyboardUpdater, 64 * 1024) { Priority = ThreadPriority.Highest}.Start();
        new Thread(MouseUpdater, 64 * 1024) { Priority = ThreadPriority.Highest }.Start();
    }

    static void MarkKeyIsDown(Key key) => keyStates[(int)key / 64] |= 1L << ((int)key % 64);
    static void MarkKeyIsUp(Key key) => keyStates[(int)key / 64] &= ~(1L << ((int)key % 64));
    public static bool IsKeyDown(Key key) => (keyStates[(int)key / 64] & (1L << ((int)key % 64))) != 0;
    public static bool IsKeyUp(Key key) => (keyStates[(int)key / 64] & (1L << ((int)key % 64))) == 0;

    static void KeyboardUpdater()
    {
        KeyStroke stroke;
        while ((Keyboard = Context.WaitKeyboardInput()) != null)
            try
            {
                while (Keyboard->Receive(&stroke))
                {
                    var key = (Key)((stroke.State & KeyState.E0) != 0 ? stroke.Code + 0x100 : stroke.Code);
                    if ((stroke.State & KeyState.Up) == 0
                        ? IsKeyUp(key) 
                          ? InternalOnKeyDown(key) 
                          : InternalOnKeyIsPress(key)
                        : InternalOnKeyUp(key))
                        Keyboard->Send(&stroke);
                }
            }
            catch { Keyboard->Send(&stroke); }
    }

    static void MouseUpdater()
    {
        MouseStroke stroke;
        while ((Mouse = Context.WaitMouseInput()) != null)
            try
            {
                while (Mouse->Receive(&stroke))
                {
                    if (stroke.X != 0 || stroke.Y != 0)
                        if (!InternalOnMouseMove(stroke.X, stroke.Y))
                            *(long*)&stroke.X = 0;

                    if (stroke.State != default)
                    {
                        if ((stroke.State & MouseState.Wheel) != 0)
                            if (!InternalOnMouseWheel(stroke.Rolling))
                                stroke.State &= ~MouseState.Wheel;

                        for (var keyIndex = 0; keyIndex < 5; keyIndex++)
                            for (var stateIndex = 0; stateIndex < 2; stateIndex++)
                            {
                                var mask = 1 << keyIndex * 2 + stateIndex;
                                if (((int)stroke.State & mask) != 0)
                                    if (stateIndex == 0 ? !InternalOnKeyDown((Key)(-100 + keyIndex)) : !InternalOnKeyUp((Key)(-100 + keyIndex)))
                                        stroke.State = (MouseState)((int)stroke.State & ~mask);
                            }
                    }

                    Mouse->Send(&stroke);
                }
            }
            catch { Mouse->Send(&stroke); }
    }

    static void ToKeyStroke(KeyStroke* stroke, Key key, bool down)
    {
        if (!down)
            stroke->State = KeyState.Up;

        var code = (short)key;
        if (code >= 0x100)
        {
            code -= 0x100;
            stroke->State |= KeyState.E0;
        }
        else if (code < 0)
        {
            code += 100;
            stroke->State |= KeyState.E0;
        }
        stroke->Code = (ushort)code;
    }

    public static Action<int, int>? OnMouseMove;
    public static Action<int>? OnMouseWheel;
    public static Action<Key>? OnKeyDown;
    public static Action<Key>? OnKeyIsPress;
    public static Action<Key>? OnKeyUp;
    public static Func<int, int, bool>? CancelableOnMouseMove;
    public static Func<int, bool>? CancelableOnMouseWheel;
    public static Func<Key, bool>? CancelableOnKeyDown;
    public static Func<Key, bool>? CancelableOnKeyIsPress;
    public static Func<Key, bool>? CancelableOnKeyUp;

    static bool InternalOnMouseMove(int x, int y)
    {
        OnMouseMove?.Invoke(x, y);
        if (CancelableOnMouseMove is not null)
            return CancelableOnMouseMove(x, y);
        return true;
    }

    static bool InternalOnMouseWheel(int rolling)
    {
        OnMouseWheel?.Invoke(rolling);
        if (CancelableOnMouseWheel is not null)
            return CancelableOnMouseWheel(rolling);
        return true;
    }

    static bool InternalOnKeyDown(Key key)
    {
        OnKeyDown?.Invoke(key);
        if (CancelableOnKeyDown is not null)
            if (!CancelableOnKeyDown(key))
                return false;

        if (InternalOnKeyIsPress(key))
        {
            MarkKeyIsDown(key);
            return true;
        }
        return false;
    }

    static bool InternalOnKeyUp(Key key)
    {
        OnKeyUp?.Invoke(key);
        if (CancelableOnKeyUp is not null)
        {
            if (CancelableOnKeyUp(key))
            {
                MarkKeyIsUp(key);
                return true;
            }
            return false;
        }

        MarkKeyIsUp(key);
        return true;
    }

    static bool InternalOnKeyIsPress(Key key)
    {
        OnKeyIsPress?.Invoke(key);
        if (CancelableOnKeyIsPress is not null)
            return CancelableOnKeyIsPress(key);
        return true;
    }

    public static void KeyUp(params IEnumerable<Key> keys)
    {
        foreach (var key in keys)
            KeyUp(key);
    }

    public static void KeyUp(Key key)
    {
        if (key < Key.None)
        {
            if (Mouse is null)
                return;

            var stroke = new MouseStroke();
            for (var bitshift = 0; bitshift < 5; bitshift++)
                if (key == (Key)((int)Key.MouseLeft + bitshift))
                {
                    stroke.State = (MouseState)(1 << bitshift * 2 + 1);
                    break;
                }

            MarkKeyIsUp(key);
            Mouse->Send(&stroke);
        }
        else
        {
            if (Keyboard is null)
                return;

            var stroke = new KeyStroke();
            ToKeyStroke(&stroke, key, false);
            MarkKeyIsUp(key);
            Keyboard->Send(&stroke);
        }
    }

    public static void KeyDown(params IEnumerable<Key> keys)
    {
        foreach (var key in keys)
            KeyDown(key);
    }

    public static void KeyDown(Key key)
    {
        if (key < Key.None)
        {
            if (Mouse is null)
                return;

            var stroke = new MouseStroke();
            for (var bitshift = 0; bitshift < 5; bitshift++)
                if (key == (Key)((int)Key.MouseLeft + bitshift))
                {
                    stroke.State = (MouseState)(1 << bitshift * 2);
                    break;
                }

            MarkKeyIsDown(key);
            Mouse->Send(&stroke);
        }
        else
        {
            if (Keyboard is null)
                return;

            var stroke = new KeyStroke();
            ToKeyStroke(&stroke, key, true);
            MarkKeyIsDown(key);
            Keyboard->Send(&stroke);
        }
    }

    public static void ScrollMouse(short rolling)
    {
        if (Mouse is null)
            return;

        var stroke = new MouseStroke { State = MouseState.Wheel, Rolling = rolling };
        Mouse->Send(&stroke);
    }

    public static void MoveMouse(int x, int y)
    {
        if (Mouse is null)
            return;

        var stroke = new MouseStroke { X = x, Y = y, Flags = MouseFlag.MoveRelative };
        Mouse->Send(&stroke);
    }

    public static void SetMouse(int x, int y) => SetMouse((float)x / screenX, (float)y / screenY);

    public static void SetMouse(float x, float y)
    {
        if (Mouse is null)
            return;

        var tx = (int)(x * 0xFFFF);
        var ty = (int)(y * 0xFFFF);
        var stroke = new MouseStroke { X = tx, Y = ty, Flags = MouseFlag.MoveAbsolute };
        Mouse->Send(&stroke);
    }

    [LibraryImport("user32")]
    internal static partial nint GetDesktopWindow();

    [LibraryImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(nint hwnd, int* rect);
}