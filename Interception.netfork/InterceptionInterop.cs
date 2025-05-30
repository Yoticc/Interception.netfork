global using OldDevice = int;
using System.Runtime.InteropServices;

namespace Interception;
public unsafe static class InterceptionInterop
{
    public delegate bool Predicate(Context context, OldDevice device);

    [DllImport("kernel32")] static extern nint CreateFileA(byte* fileName, int desiredAccess, int shareMode, void* securityAttributes, int creationDisposition, int flagsAndAttributes, nint templateFile);
    [DllImport("kernel32")] static extern bool DeviceIoControl(nint device, int ioControlCode, void* inBuffer, long inBufferSize, void* outBuffer, long outBufferSize, int* bytesReturned, void* overlapped);
    [DllImport("kernel32")] static extern nint CreateEventA(nint eventAttributes, bool manualReset, bool initialState, byte* name);
    [DllImport("kernel32")] static extern int  WaitForMultipleObjects(int count, nint* handles, bool waitAll, int milliseconds);
    [DllImport("kernel32")] static extern bool CloseHandle(nint objectHandle);

    const nint INVALID_HANDLE_VALUE = -1;
    const int FILE_DEVICE_UNKNOWN = 0x22;
    const int METHOD_BUFFERED = 0;
    const int FILE_ANY_ACCESS = 0;
    const int INFINITE = unchecked((int)0xFFFFFFFF);
    const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);
    const int WAIT_TIMEOUT = 0x102;
    const int ACCESS_MASK_GENERIC_READ = unchecked((int)0x80000000);
    const int FILE_CREATION_OPEN_EXISTS = 3;

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

    [StructLayout(LayoutKind.Explicit, Size = 0x0C)]
    public struct KeyStroke
    {
        [FieldOffset(0x02)] public ushort Code;
        [FieldOffset(0x04)] public KeyState State;
        [FieldOffset(0x08)] public uint Information;
    }

    [StructLayout(LayoutKind.Explicit, Size = 0x18)]
    public struct MouseStroke
    {
        [FieldOffset(0x02)] public MouseFlag Flags;
        [FieldOffset(0x04)] public MouseState State;
        [FieldOffset(0x06)] public short Rolling;
        [FieldOffset(0x0C)] public int X;
        [FieldOffset(0x10)] public int Y;
        [FieldOffset(0x14)] public uint Information;
    }

    public struct Device
    {
        public nint FileHandle;
        public nint EventHandle;
    }

    public struct Mouse
    {
        public Device Device;

        public void Send(MouseStroke* stroke) => DeviceIoControl(Device.FileHandle, IOCTL_WRITE, stroke, sizeof(MouseStroke), null, 0, null, null);
        public bool Receive(MouseStroke* stroke) => DeviceIoControl(Device.FileHandle, IOCTL_READ, null, 0, stroke, sizeof(MouseStroke), null, null);
    }

    public struct Keyboard
    {
        public Device Device;

        public void Send(KeyStroke* stroke) => DeviceIoControl(Device.FileHandle, IOCTL_WRITE, stroke, sizeof(KeyStroke), null, 0, null, null);
        public bool Receive(KeyStroke* stroke) => DeviceIoControl(Device.FileHandle, IOCTL_READ, null, 0, stroke, sizeof(KeyStroke), null, null);
    }

    public struct Context
    {
        public readonly Device* Devices;
        public Keyboard* Keyboards => (Keyboard*)Devices;
        public Mouse* Mouses => (Mouse*)(Devices + MaxMouses);

        public Device* FirstDevice => Devices;
        public Device* LastDevice => FirstDevice + MaxDevices - 1;

        public bool IsValid => Devices is not null;

        public int GetIndexOfDevice(Device* device) => (int)(Devices - device);

        public void Destroy()
        {
            for (var device = FirstDevice; device <= LastDevice; device++)
            {
                if (device->FileHandle != INVALID_HANDLE_VALUE)
                    CloseHandle(device->FileHandle);

                if (device->EventHandle != default)
                    CloseHandle(device->EventHandle);
            }

            Marshal.FreeCoTaskMem((nint)Devices);
        }

        public static Context Create()
        {
            var allocatedContext = Marshal.AllocCoTaskMem(MaxDevices * sizeof(Device));
            var context = *(Context*)&allocatedContext;

            var readonlyDeviceName = "\\\\.\\interception00\0"u8;
            var deviceName = stackalloc byte[readonlyDeviceName.Length];
            var deviceNameIndex = (short*)(deviceName + readonlyDeviceName.Length - 3);
            readonlyDeviceName.CopyTo(new Span<byte>(deviceName, readonlyDeviceName.Length));

            var device = context.FirstDevice;
            var eventHandleAligned = stackalloc nint[2];
            for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++, device++)
            {
                *deviceNameIndex = (short)(('0' + deviceIndex / 10) | ('0' + deviceIndex % 10) << 8);

                if ((device->FileHandle = CreateFileA(deviceName, ACCESS_MASK_GENERIC_READ, default, null, FILE_CREATION_OPEN_EXISTS, default, default)) != INVALID_HANDLE_VALUE)
                    if ((device->EventHandle = *eventHandleAligned = CreateEventA(default, true, false, null)) != default)
                        if (DeviceIoControl(device->FileHandle, IOCTL_SET_EVENT, eventHandleAligned, sizeof(nint) * 2, null, default, null, null))
                            continue;

                context.Destroy();
                return default;
            }

            return context;
        }
    }

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

    public enum FilterKeyState : ushort
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

    public enum MouseState : ushort
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

    public enum FilterMouseState : ushort
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

    public enum MouseFlag : ushort
    {
       MoveRelative            = 0x000,
       MoveAbsolute            = 0x001,
       VirtualDesktop          = 0x002,
       AttributesChanged       = 0x004,
       MoveNoCoalesce          = 0x008,
       TerminalServerSrcShadow = 0x100,
    }

    public static Filter interception_get_filter(Context context, OldDevice device)
    {
        var devices = context.Devices;
        var filter = default(Filter);

        DeviceIoControl(devices[device].FileHandle, IOCTL_GET_FILTER, null, 0, &filter, sizeof(Filter), null, null);

        return filter;
    }

    public static void interception_set_filter(Context context, Predicate interception_predicate, Filter filter)
    {
        var devices = context.Devices;

        for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++)
            if ( interception_predicate(context, deviceIndex) != false)
                DeviceIoControl(devices[deviceIndex].FileHandle, IOCTL_SET_FILTER, &filter, sizeof(Filter), null, 0, null, null);
    }

    public static OldDevice interception_wait(Context context) => interception_wait_with_timeout(context, INFINITE);

    public static OldDevice interception_wait_with_timeout(Context context, int milliseconds)
    {
        var devices = context.Devices;
        var waitHandles = stackalloc nint[MaxDevices];

        int deviceIndex, count, waitResult;
        for (deviceIndex = 0, count = 0; deviceIndex < MaxDevices; deviceIndex++)
            if (devices[deviceIndex].EventHandle != default)
                waitHandles[count++] = devices[deviceIndex].EventHandle;

        waitResult = WaitForMultipleObjects(count, waitHandles, false, milliseconds);

        if (waitResult == WAIT_FAILED || waitResult == WAIT_TIMEOUT) 
            return -1;

        for (deviceIndex = 0, count = 0; deviceIndex < MaxDevices; deviceIndex++)
            if (devices[deviceIndex].EventHandle != default)
                if (waitResult == count++)
                    break;

        return deviceIndex;
    }

    public static int interception_get_hardware_id(Context context, OldDevice device, void* hardware_id_buffer, uint buffer_size)
    {
        var outputSize = 0;

        DeviceIoControl(context.Devices[device].FileHandle, IOCTL_GET_HARDWARE_ID, null, 0, hardware_id_buffer, buffer_size, &outputSize, null);

        return outputSize;
    }

    public static bool interception_is_keyboard(Context context, OldDevice device) => device is >= 0 and < MaxKeyboards;

    public static bool interception_is_mouse(Context context, OldDevice device) => device is >= MaxKeyboards and < MaxDevices;
}