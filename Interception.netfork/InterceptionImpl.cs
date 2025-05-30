using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Interception;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
public static unsafe class InterceptionImpl
{
    public static Keyboard* Keyboard;
    public static Mouse* Mouse;
    public static Context Context;

    static InterceptionImpl()
    {
        if (Debugger.IsAttached)
            return;

        Context = Context.Create();
        Context.SetFilter(Filter.All);

        new Thread(DriverKeyboardUpdater)
        {
            Priority = ThreadPriority.Highest
        }.Start();

        new Thread(DriverMouseUpdater)
        {
            Priority = ThreadPriority.Highest
        }.Start();
    }

    static void DriverKeyboardUpdater()
    {
        var stroke = stackalloc KeyStroke[1];
        Keyboard* keyboard;
        while (true)
        {
            try
            {
                while (true)
                {
                    if ((keyboard = Context.WaitKeyboardInput()) == null)
                        continue;

                    Keyboard = keyboard;
                    while (Keyboard->Receive(stroke))
                    {
                        var key = ToKey(stroke);

                        var processed = false;
                        if ((stroke->State & KeyState.Up) == 0)
                        {
                            switch (IsKeyUp(key))
                            {
                                case true:
                                    SetKeyIsDown(key);
                                    processed = InternalOnKeyDown(key, false);
                                    break;
                                case false:
                                    processed = InternalOnKeyDown(key, true);
                                    break;
                            }
                        }
                        else
                        {
                            SetKeyIsUp(key);
                            processed = InternalOnKeyUp(key);
                        }

                        if (!processed)
                            Keyboard->Send(stroke);
                    }
                }
            }
            catch 
            {
                try
                {
                    Keyboard->Send(stroke);
                }
                catch { }
            }
        }
    }

    static void DriverMouseUpdater()
    {
        var stroke = stackalloc MouseStroke[1];
        Mouse* mouse;
        while (true)
        {
            try
            {
                while (true)
                {
                    if ((mouse = Context.WaitMouseInput()) == null)
                        continue;

                    Mouse = mouse;
                    while (Mouse->Receive(stroke))
                    {
                        var state = stroke->State;
                        var processed = false;

                        if (state == default)
                        {
                            var (x, y) = (stroke->X, stroke->Y);
                            if (x != 0 || y != 0)
                                if(!InternalOnMouseMove(x, y))
                                    Mouse->Send(stroke);
                        }
                        else
                        {
                            if ((state & MouseState.LeftButtonDown) != 0)
                                InternalOnKeyDown(Key.MouseLeft, false);

                            if ((state & MouseState.LeftButtonUp) != 0)
                                InternalOnKeyUp(Key.MouseLeft);

                            if ((state & MouseState.RightButtonDown) != 0)
                                InternalOnKeyDown(Key.MouseRight, false);

                            if ((state & MouseState.RightButtonUp) != 0)
                                InternalOnKeyUp(Key.MouseRight);

                            if ((state & MouseState.MiddleButtonDown) != 0)
                                InternalOnKeyDown(Key.MouseMiddle, false);

                            if ((state & MouseState.MiddleButtonUp) != 0)
                                InternalOnKeyUp(Key.MouseMiddle);

                            if ((state & MouseState.Button4Down) != 0)
                                InternalOnKeyDown(Key.Button1, false);

                            if ((state & MouseState.Button4Up) != 0)
                                InternalOnKeyUp(Key.Button1);

                            if ((state & MouseState.Button5Down) != 0)
                                InternalOnKeyDown(Key.Button2, false);

                            if ((state & MouseState.Button5Up) != 0)
                                InternalOnKeyUp(Key.Button2);

                            if ((state & MouseState.Wheel) != 0)
                                InternalOnMouseWheel(stroke->Rolling);

                            if (state != default)
                            {
                                stroke->State = state;
                                Mouse->Send(stroke);
                            }
                        }
                    }
                }
            }
            catch 
            {
                try
                {
                    Mouse->Send(stroke);
                }
                catch { }
            }
        }
    }

    static long* keyStates = (long*)Marshal.AllocCoTaskMem(0x80) + 0x08;

    static void SetKeyIsDown(Key key) => keyStates[(int)key / 64] |= 1L << ((int)key % 64);
    static void SetKeyIsUp(Key key) => keyStates[(int)key / 64] &= ~(1L << ((int)key % 64));

    static Key ToKey(KeyStroke* keyStroke)
    {
        var result = keyStroke->Code;
        if ((keyStroke->State & KeyState.E0) != 0)
            result += 0x100;
        return (Key)result;
    }

    static KeyStroke ToKeyStroke(Key key, bool down)
    {
        var result = new KeyStroke();
        if (!down)
            result.State = KeyState.Up;
        var code = (short)key;
        if (code >= 0x100)
        {
            code -= 0x100;
            result.State |= KeyState.E0;
        }
        else if (code < 0)
        {
            code += 100;
            result.State |= KeyState.E0;
        }
        result.Code = (ushort)code;
        return result;
    }

    public delegate bool OnMouseMoveDelegate(int x, int y);
    public static OnMouseMoveDelegate? OnMouseMove;

    public delegate bool OnMouseWheelDelegate(int rolling);
    public static OnMouseWheelDelegate? OnMouseWheel;

    public delegate bool OnKeyDownDelegate(Key key, bool repeat);
    public static OnKeyDownDelegate? OnKeyDown;

    public delegate bool OnKeyUpDelegate(Key key);
    public static OnKeyUpDelegate? OnKeyUp;

    static bool InternalOnMouseMove(int x, int y)
    {
        if (OnMouseMove != null)
            return OnMouseMove(x, y);
        return false;
    }

    static bool InternalOnMouseWheel(int rolling)
    {
        if (OnMouseWheel != null)
            return OnMouseWheel(rolling);
        return false;
    }

    static bool InternalOnKeyDown(Key key, bool repeat)
    {
        SetKeyIsDown(key);
        if (OnKeyDown != null)
            return OnKeyDown(key, repeat);
        return false;
    }

    static bool InternalOnKeyUp(Key key)
    {
        SetKeyIsUp(key);
        if (OnKeyUp != null)
            return OnKeyUp(key);
        return false;
    }

    public static bool IsKeyDown(Key key) => IsKeyDown((int)key);
    static bool IsKeyDown(int key) => (keyStates[key / 64] & (1L << (key % 64))) != 0;

    public static bool IsKeyUp(Key key) => !IsKeyDown(key);

    public static void KeyUp(params Key[] keys)
    {
        foreach (var key in keys)
            KeyUp(key);
    }

    public static void KeyUp(Key key)
    {
        SetKeyIsUp(key);
        if (((short)key) < 0)
        {
            var stroke = stackalloc MouseStroke[1];
            stroke->State = key switch
            {
                Key.MouseLeft => MouseState.LeftButtonUp,
                Key.MouseRight => MouseState.RightButtonUp,
                Key.MouseMiddle => MouseState.MiddleButtonUp,
                Key.Button1 => MouseState.Button4Up,
                Key.Button2 => MouseState.Button5Up,
                _ => default
            };

            Mouse->Send(stroke);
        }
        else
        {
            var stroke = stackalloc KeyStroke[1];
            *stroke = ToKeyStroke(key, false);
            Keyboard->Send(stroke);
        }
    }

    public static void KeyDown(params Key[] keys)
    {
        foreach (var key in keys)
            KeyDown(key);
    }

    public static void KeyDown(Key key)
    {
        SetKeyIsDown(key);

        if (((short)key) < 0)
        {
            var stroke = stackalloc MouseStroke[1];
            stroke->State = key switch
            {
                Key.MouseLeft => MouseState.LeftButtonDown,
                Key.MouseRight => MouseState.RightButtonDown,
                Key.MouseMiddle => MouseState.MiddleButtonDown,
                Key.Button1 => MouseState.Button4Down,
                Key.Button2 => MouseState.Button5Down,
                _ => default
            };

            Mouse->Send(stroke);
        }
        else
        {
            var stroke = stackalloc KeyStroke[1];
            *stroke = ToKeyStroke(key, true);

            Keyboard->Send(stroke);
        }
    }

    public static void ScrollMouse(short rolling)
    {
        var stroke = stackalloc MouseStroke[1];
        stroke->State = MouseState.Wheel;
        stroke->Rolling = rolling;

        Mouse->Send(stroke);
    }

    public static void MoveMouse(int x, int y)
    {
        var stroke = stackalloc MouseStroke[1];
        stroke->X = x;
        stroke->Y = y;
        stroke->Flags = MouseFlag.MoveRelative;

        Mouse->Send(stroke);
    }

    public static void SetMouse(int x, int y)
    {
        var stroke = stackalloc MouseStroke[1];
        stroke->X = x;
        stroke->Y = y;
        stroke->Flags = MouseFlag.MoveAbsolute;

        Mouse->Send(stroke);
    }
}