global using OldDevice = int;
global using Precedence = int;
using System.Numerics;
using System.Runtime.InteropServices;
using static Kernel32;

#pragma warning disable IDE0044 // Add readonly modifier
namespace Interception;
public unsafe static class InterceptionInterop
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool Predicate(OldDevice device);

    const nint INVALID_HANDLE_VALUE = -1;
    const int FILE_DEVICE_UNKNOWN = 0x22;
    const int METHOD_BUFFERED = 0;
    const int FILE_ANY_ACCESS = 0;
    const int INFINITE = unchecked((int)0xFFFFFFFF);
    const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);
    const int WAIT_TIMEOUT = 258;

    const int MaxKeyboards = 10;
    const int MaxMouses = 10;
    const int MaxDevices = MaxKeyboards + MaxMouses;

    const int IOCTL_SET_PRECEDENCE  = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x801 << 2 | METHOD_BUFFERED;
    const int IOCTL_GET_PRECEDENCE  = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x802 << 2 | METHOD_BUFFERED;
    const int IOCTL_SET_FILTER      = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x804 << 2 | METHOD_BUFFERED;
    const int IOCTL_GET_FILTER      = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x808 << 2 | METHOD_BUFFERED;
    const int IOCTL_SET_EVENT       = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x810 << 2 | METHOD_BUFFERED;
    const int IOCTL_WRITE           = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x820 << 2 | METHOD_BUFFERED;
    const int IOCTL_READ            = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x840 << 2 | METHOD_BUFFERED;
    const int IOCTL_GET_HARDWARE_ID = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x880 << 2 | METHOD_BUFFERED;
    
    public struct KeyboardInputData
    {
        public ushort UnitID;
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public uint ExtraInformation;
    }

    public struct MouseInputData
    {
        public ushort UnitId;
        public ushort Flags;
        public ushort ButtonFlags;
        public ushort ButtonData;
        public uint RawButtons;
        public int LastX;
        public int LastY;
        public uint ExtraInformation;
    }

    public struct Device
    {
        public nint FileHandle;
        public nint EventHandle;
    }

    public struct Context
    {
        public readonly Device* Devices;

        public Device* FirstDevice => Devices;
        public Device* LastDevice => FirstDevice + MaxDevices;

        public nint Address => (nint)Devices;
        public bool IsValid => Devices is not null;
    }

    [Flags]
    public enum Filter : ushort
    {
        None = FilterKeyState.None,
        All = FilterKeyState.All,

        KDown = FilterKeyState.Down,
        KUp = FilterKeyState.Up,
        KE0 = FilterKeyState.E0,
        KE1 = FilterKeyState.E1,
        KTermSrvSetLED = FilterKeyState.TerminalServerSetLed,
        KTermSrvShadow = FilterKeyState.TerminalServerShadow,
        KTermSrvVKPacket = FilterKeyState.TerminalServerVkPacket,

        MLeftButtonDown = FilterMouseState.LeftButtonDown,
        MLeftButtonUp = FilterMouseState.LeftButtonUp,
        MRightButtonDown = FilterMouseState.RightButtonDown,
        MRightButtonUp = FilterMouseState.RightButtonUp,
        MMiddleButtonDown = FilterMouseState.MiddleButtonDown,
        MMiddleButtonUp = FilterMouseState.MiddleButtonUp,

        MButton1Down = FilterMouseState.Button1Down,
        MButton1Up = FilterMouseState.Button1Up,
        MButton2Down = FilterMouseState.Button2Down,
        MButton2Up = FilterMouseState.Button2Up,
        MButton3Down = FilterMouseState.Button3Down,
        MButton3Up = FilterMouseState.Button3Up,

        MButton4Down = FilterMouseState.Button4Down,
        MButton4Up = FilterMouseState.Button4Up,
        MButton5Down = FilterMouseState.Button5Down,
        MButton5Up = FilterMouseState.Button5Up,

        MWheel = FilterMouseState.Wheel,
        MHWheel = FilterMouseState.HWheel,

        MMove = FilterMouseState.Move
    }

    public enum KeyState : ushort
    {
        Down = 0x00,
        Up   = 0x01,
        E0   = 0x02,
        E1   = 0x04,

        TerminalServerSetLed   = 0x08,
        TerminalServerShadow   = 0x10,
        TerminalServerVkPacket = 0x20,
    };

    [Flags]
    public enum FilterKeyState
    {
        None = 0x0000,
        All  = 0xFFFF,

        Down = KeyState.Up,
        Up   = KeyState.Up << 1,
        E0   = KeyState.E0 << 1,
        E1   = KeyState.E1 << 1,

        TerminalServerSetLed   = KeyState.TerminalServerSetLed << 1,
        TerminalServerShadow   = KeyState.TerminalServerShadow << 1,
        TerminalServerVkPacket = KeyState.TerminalServerVkPacket << 1
    };

    public enum MouseState
    {
        LeftButtonDown   = 0x001,
        LeftButtonUp     = 0x002,
        RightButtonDown  = 0x004,
        RightButtonUp    = 0x008,
        MiddleButtonDown = 0x010,
        MiddleButtonUp   = 0x020,

        Button1Down = LeftButtonDown,
        Button1Up   = LeftButtonUp,
        Button2Down = RightButtonDown,
        Button2Up   = RightButtonUp,
        Button3Down = MiddleButtonDown,
        Button3Up   = MiddleButtonUp,

        Button4Down = 0x040,
        Button4Up   = 0x080,
        Button5Down = 0x100,
        Button5Up   = 0x200,

        Wheel  = 0x400,
        HWheel = 0x800
    };

    [Flags]
    public enum FilterMouseState
    {
        None = 0x0000,
        All = 0xFFFF,

        LeftButtonDown   = MouseState.LeftButtonDown,
        LeftButtonUp     = MouseState.LeftButtonUp,
        RightButtonDown  = MouseState.RightButtonDown,
        RightButtonUp    = MouseState.RightButtonUp,
        MiddleButtonDown = MouseState.MiddleButtonDown,
        MiddleButtonUp   = MouseState.MiddleButtonUp,

        Button1Down = MouseState.Button1Down,
        Button1Up   = MouseState.Button1Up,
        Button2Down = MouseState.Button2Down,
        Button2Up   = MouseState.Button2Up,
        Button3Down = MouseState.Button3Down,
        Button3Up   = MouseState.Button3Up,

        Button4Down = MouseState.Button4Down,
        Button4Up   = MouseState.Button4Up,
        Button5Down = MouseState.Button5Down,
        Button5Up   = MouseState.Button5Up,

        Wheel  = MouseState.Wheel,
        HWheel = MouseState.HWheel,

        Move = 0x1000
    };

    public enum MouseFlag
    {
       MoveRelative            = 0x000,
       MoveAbsolute            = 0x001,
       VirtualDesktop          = 0x002,
       AttributesChanged       = 0x004,
       MoveNoCoalesce          = 0x008,
       TerminalServerSrcShadow = 0x100,
    };

    public struct MouseStroke
    {
        public MouseState State;
        public MouseFlag Flags;
        public short Rolling;
        public int X;
        public int Y;
        public uint Information;
    }

    public struct KeyStroke
    {
        public ushort Code;
        public KeyState State;
        public uint Information;

        public bool IsKeyDown => (State & KeyState.Up) == 0;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Stroke
    {
        [FieldOffset(0x00)]
        public MouseStroke Mouse;

        [FieldOffset(0x00)]
        public KeyStroke Key;
    }

    public static Context interception_create_context()
    {
        var readonlyDeviceName = "\\\\.\\interception00\0"u8;
        var deviceName = stackalloc byte[readonlyDeviceName.Length];
        var deviceNameIndex = (ushort*)(deviceName + readonlyDeviceName.Length - 3);
        readonlyDeviceName.CopyTo(new Span<byte>(deviceName, readonlyDeviceName.Length));
        
        var devices = (Device*)Marshal.AllocCoTaskMem(MaxDevices * sizeof(Device));
        var context = *(Context*)&devices;
        if (devices is not null)
        {
            var device = devices;
            var eventHandleAligned = stackalloc nint[2];
            for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++, device++)
            {
                *deviceNameIndex = (ushort)(('0' + (deviceIndex / 10)) | ('0' + (deviceIndex % 10)) << 8);

                if ((device->FileHandle = CreateFileA(deviceName, AccessMask.GenericRead, FileShareMode.None, null, FileCreationDisposition.OpenExisting, default, default)) != INVALID_HANDLE_VALUE)
                    if ((device->EventHandle = eventHandleAligned[0] = CreateEventA(default, true, false, null)) != default)
                        if (DeviceIoControl(device->FileHandle, IOCTL_SET_EVENT, eventHandleAligned, sizeof(nint) * 2, null, default, null, null))
                            continue;

                interception_destroy_context(context);
                return default;
            }
        }

        return context;
    }

    public static void interception_destroy_context(Context context)
    {
        if (!context.IsValid)
            return;

        var device = context.Devices;
        for (var i = 0; i < MaxDevices; i++, device++)
        {
            if (device->FileHandle != INVALID_HANDLE_VALUE)
                CloseHandle(device->FileHandle);

            if (device->EventHandle != default)
                CloseHandle(device->EventHandle);
        }

        Marshal.FreeCoTaskMem(context.Address);
    }

    public static Precedence interception_get_precedence(Context context, OldDevice device)
    {
        var devices = context.Devices;
        var precedence = default(Precedence);

        if (context.IsValid && devices[device].FileHandle != default)
            DeviceIoControl(devices[device].FileHandle, IOCTL_GET_PRECEDENCE, null, 0, &precedence, sizeof(Precedence), null, null);

        return precedence;
    }

    public static void interception_set_precedence(Context context, OldDevice device, Precedence precedence)
    {
        var devices = context.Devices;

        if (context.IsValid && devices[device].FileHandle != default)
            DeviceIoControl(devices[device].FileHandle, IOCTL_SET_PRECEDENCE, &precedence, sizeof(Precedence), null, 0, null, null);
    }

    public static Filter interception_get_filter(Context context, OldDevice device)
    {
        var devices = context.Devices;
        var filter = default(Filter);

        if (context.IsValid && devices[device].FileHandle != default)
            DeviceIoControl(devices[device].FileHandle, IOCTL_GET_FILTER, null, 0, &filter, sizeof(Filter), null, null);

        return filter;
    }

    public static void interception_set_filter(Context context, Predicate interception_predicate, Filter filter)
    {
        var devices = context.Devices;

        if (context.IsValid)
            for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++)
                if (devices[deviceIndex].FileHandle != default && interception_predicate(deviceIndex) != default)
                    DeviceIoControl(devices[deviceIndex].FileHandle, IOCTL_SET_FILTER, &filter, sizeof(Filter), null, 0, null, null);
    }

    public static OldDevice interception_wait(Context context) => interception_wait_with_timeout(context, INFINITE);

    public static OldDevice interception_wait_with_timeout(Context context, int milliseconds)
    {
        var devices = context.Devices;
        var waitHandles = stackalloc nint[MaxDevices];

        if (!context.IsValid) 
            return default;

        int deviceIndex, count, waitResult;
        for (deviceIndex = 0, count = 0; deviceIndex < MaxDevices; deviceIndex++)
            if (devices[deviceIndex].EventHandle != default)
                waitHandles[count++] = devices[deviceIndex].EventHandle;

        waitResult = WaitForMultipleObjects(count, waitHandles, false, milliseconds);

        if (waitResult == WAIT_FAILED || waitResult == WAIT_TIMEOUT) 
            return default;

        for (deviceIndex = 0, count = 0; deviceIndex < MaxDevices; deviceIndex++)
            if (devices[deviceIndex].EventHandle != default)
                if (waitResult == count++)
                    break;

        return deviceIndex;
    }

    public static void interception_send_mouse(Context context, OldDevice device, Stroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || interception_is_invalid(device) || devices[device].FileHandle == default)
            return;

        var rawStrokes = stackalloc MouseInputData[1];

        var mouseStroke = (MouseStroke*)stroke;
        var rawStroke = rawStrokes;
        rawStroke->UnitId = 0;
        rawStroke->Flags = (ushort)mouseStroke->Flags;
        rawStroke->ButtonFlags = (ushort)mouseStroke->State;
        rawStroke->ButtonData = (ushort)mouseStroke->Rolling;
        rawStroke->RawButtons = 0;
        rawStroke->LastX = mouseStroke->X;
        rawStroke->LastY = mouseStroke->Y;
        rawStroke->ExtraInformation = mouseStroke->Information;

        DeviceIoControl(devices[device].FileHandle, IOCTL_WRITE, rawStrokes, sizeof(MouseInputData), null, 0, null, null);
    }

    public static void interception_send_keyboard(Context context, OldDevice device, Stroke* stroke)
    {
        var devices = context.Devices;

        if(!context.IsValid || interception_is_invalid(device) || devices[device].FileHandle == default)
            return;

        var rawStrokes = stackalloc KeyboardInputData[1];

        var keyStroke = (KeyStroke*)stroke;
        var rawStroke = rawStrokes;
        rawStroke->UnitID = 0;
        rawStroke->MakeCode = keyStroke->Code;
        rawStroke->Flags = (ushort)keyStroke->State;
        rawStroke->Reserved = 0;
        rawStroke->ExtraInformation = keyStroke->Information;

        DeviceIoControl(devices[device].FileHandle, IOCTL_WRITE, rawStrokes, sizeof(KeyboardInputData), null, 0, null, null);
    }

    public static bool interception_receive_keyboard(Context context, OldDevice device, Stroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || interception_is_invalid(device) || devices[device - 1].FileHandle == default)
            return false;

        var rawStrokes = stackalloc KeyboardInputData[1];

        if (!DeviceIoControl(devices[device].FileHandle, IOCTL_READ, null, 0, rawStrokes, sizeof(KeyboardInputData), null, null))
            return false;

        var keyStroke = (KeyStroke*)stroke;
        keyStroke->Code = rawStrokes->MakeCode;
        keyStroke->State = (KeyState)rawStrokes->Flags;
        keyStroke->Information = rawStrokes->ExtraInformation;

        return true;
    }

    public static bool interception_receive_mouse(Context context, OldDevice device, Stroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || interception_is_invalid(device) || devices[device].FileHandle == default)
            return false;

        var rawStrokes = stackalloc MouseInputData[1];

        if (!DeviceIoControl(devices[device].FileHandle, IOCTL_READ, null, 0, rawStrokes, sizeof(MouseInputData), null, null))
            return false;

        var mouseStroke = (MouseStroke*)stroke;
        mouseStroke->Flags = (MouseFlag)rawStrokes->Flags;
        mouseStroke->State = (MouseState)rawStrokes->ButtonFlags;
        mouseStroke->Rolling = (short)rawStrokes->ButtonData;
        mouseStroke->X = rawStrokes->LastX;
        mouseStroke->Y = rawStrokes->LastY;
        mouseStroke->Information = rawStrokes->ExtraInformation;

        return true;
    }

    public static int interception_get_hardware_id(Context context, OldDevice device, void* hardware_id_buffer, uint buffer_size)
    {
        var devices = context.Devices;
        var outputSize = 0;

        if (!context.IsValid || interception_is_invalid(device) || devices[device].FileHandle == default) 
            return 0;

        DeviceIoControl(devices[device].FileHandle, IOCTL_GET_HARDWARE_ID, null, 0, hardware_id_buffer, buffer_size, &outputSize, null);

        return outputSize;
    }

    public static bool interception_is_invalid(OldDevice device) => !interception_is_keyboard(device) && !interception_is_mouse(device);

    static int INTERCEPTION_KEYBOARD(int index) => index;

    public static bool interception_is_keyboard(OldDevice device) => device >= INTERCEPTION_KEYBOARD(0) && device <= INTERCEPTION_KEYBOARD(MaxKeyboards - 1);

    static int INTERCEPTION_MOUSE(int index) => MaxKeyboards + index;

    public static bool interception_is_mouse(OldDevice device) => device >= INTERCEPTION_MOUSE(0) && device <= INTERCEPTION_MOUSE(MaxMouses - 1);
}