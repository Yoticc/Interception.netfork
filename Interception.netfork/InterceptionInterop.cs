global using OldDevice = int;
global using Precedence = int;
using System.Runtime.InteropServices;
using static Kernel32;

#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0044 // Add readonly modifier
namespace Interception;
public unsafe static class InterceptionInterop
{
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
    
    public struct KeyStroke
    {
        ushort UnitID;
        public ushort Code;
        public KeyState State;
        ushort Reserved;
        public uint Information;
    }

    public struct MouseStroke
    {
        ushort UnitId;
        public MouseFlag Flags;
        public MouseState State;
        public short Rolling;
        uint RawButtons;
        public int X;
        public int Y;
        public uint Information;
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

        public int GetIndexOfDevice(Device* device) => (int)(Devices - device);

        public void Destroy() => Marshal.FreeCoTaskMem((nint)Devices);

        public static Context Create()
        {
            var context = Marshal.AllocCoTaskMem(MaxDevices * sizeof(Device));
            return *(Context*)&context;
        }
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
    }

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
    }

    public enum MouseState : short
    {
        LeftButtonDown   = 0x001,
        LeftButtonUp     = 0x002,
        RightButtonDown  = 0x004,
        RightButtonUp    = 0x008,
        MiddleButtonDown = 0x010,
        MiddleButtonUp   = 0x020,

        Button4Down = 0x040,
        Button4Up   = 0x080,
        Button5Down = 0x100,
        Button5Up   = 0x200,

        Wheel  = 0x400,
        HWheel = 0x800
    }

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

        Button4Down = MouseState.Button4Down,
        Button4Up   = MouseState.Button4Up,
        Button5Down = MouseState.Button5Down,
        Button5Up   = MouseState.Button5Up,

        Wheel  = MouseState.Wheel,
        HWheel = MouseState.HWheel,

        Move = 0x1000
    }

    public enum MouseFlag : short
    {
       MoveRelative            = 0x000,
       MoveAbsolute            = 0x001,
       VirtualDesktop          = 0x002,
       AttributesChanged       = 0x004,
       MoveNoCoalesce          = 0x008,
       TerminalServerSrcShadow = 0x100,
    }

    public static Context interception_create_context()
    {
        var readonlyDeviceName = "\\\\.\\interception00\0"u8;
        var deviceName = stackalloc byte[readonlyDeviceName.Length];
        var deviceNameIndex = (short*)(deviceName + readonlyDeviceName.Length - 3);
        readonlyDeviceName.CopyTo(new Span<byte>(deviceName, readonlyDeviceName.Length));

        var context = Context.Create();
        var device = context.FirstDevice;
        var eventHandleAligned = stackalloc nint[2];
        for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++, device++)
        {
            *deviceNameIndex = (short)(('0' + deviceIndex / 10) | ('0' + deviceIndex % 10) << 8);

            if ((device->FileHandle = CreateFileA(deviceName, AccessMask.GenericRead, FileShareMode.None, null, FileCreationDisposition.OpenExisting, default, default)) != INVALID_HANDLE_VALUE)
                if ((device->EventHandle = *eventHandleAligned = CreateEventA(default, true, false, null)) != default)
                    if (DeviceIoControl(device->FileHandle, IOCTL_SET_EVENT, eventHandleAligned, sizeof(nint) * 2, null, default, null, null))
                        continue;

            interception_destroy_context(context);
            return default;
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

    public static void interception_send_mouse(Context context, OldDevice device, MouseStroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || devices[device].FileHandle == default)
            return;

        DeviceIoControl(devices[device].FileHandle, IOCTL_WRITE, stroke, sizeof(MouseStroke), null, 0, null, null);
    }

    public static void interception_send_keyboard(Context context, OldDevice device, KeyStroke* stroke)
    {
        var devices = context.Devices;

        if(!context.IsValid || devices[device].FileHandle == default)
            return;

        DeviceIoControl(devices[device].FileHandle, IOCTL_WRITE, stroke, sizeof(KeyStroke), null, 0, null, null);
    }

    public static bool interception_receive_keyboard(Context context, OldDevice device, KeyStroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || devices[device - 1].FileHandle == default)
            return false;

        if (!DeviceIoControl(devices[device].FileHandle, IOCTL_READ, null, 0, stroke, sizeof(KeyStroke), null, null))
            return false;

        return true;
    }

    public static bool interception_receive_mouse(Context context, OldDevice device, MouseStroke* stroke)
    {
        var devices = context.Devices;

        if (!context.IsValid || devices[device].FileHandle == default)
            return false;

        var rawStrokes = stackalloc MouseStroke[1];
        if (!DeviceIoControl(devices[device].FileHandle, IOCTL_READ, null, 0, stroke, sizeof(MouseStroke), null, null))
            return false;

        return true;
    }

    public static int interception_get_hardware_id(Context context, OldDevice device, void* hardware_id_buffer, uint buffer_size)
    {
        var devices = context.Devices;
        var outputSize = 0;

        if (!context.IsValid || devices[device].FileHandle == default) 
            return 0;

        DeviceIoControl(devices[device].FileHandle, IOCTL_GET_HARDWARE_ID, null, 0, hardware_id_buffer, buffer_size, &outputSize, null);

        return outputSize;
    }

    public static bool interception_is_keyboard(OldDevice device) => device is >= 0 and < MaxKeyboards;

    public static bool interception_is_mouse(OldDevice device) => device is >= MaxKeyboards and < MaxDevices;
}