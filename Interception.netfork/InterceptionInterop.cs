using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Interception;
public unsafe struct Device
{
    public nint FileHandle, EventHandle;

    public bool Initialize(byte* deviceName, nint* eventHandleAligned) =>
        (FileHandle = CreateFileA(deviceName, ACCESS_MASK_GENERIC_READ, default, null, FILE_CREATION_OPEN_EXISTS, default, default)) != INVALID_HANDLE_VALUE &&
        (EventHandle = *eventHandleAligned = CreateEventA(default, true, false, null)) != default &&
        DeviceIoControl(FileHandle, IOCTL_SET_EVENT, eventHandleAligned, sizeof(nint) * 2, null, default, null, null);

    public void Send<T>(T* stroke) where T : unmanaged => DeviceIoControl(FileHandle, IOCTL_WRITE, stroke, sizeof(T), null, 0, null, null);
    public bool Receive<T>(T* stroke) where T : unmanaged => DeviceIoControl(FileHandle, IOCTL_READ, null, 0, stroke, sizeof(T), null, null);

    public void SetFilter(Filter filter) => DeviceIoControl(FileHandle, IOCTL_SET_FILTER, &filter, sizeof(Filter), null, 0, null, null);

    public void Destoy()
    {
        if (FileHandle != default && FileHandle != INVALID_HANDLE_VALUE)
            CloseHandle(FileHandle);

        if (EventHandle != default)
            CloseHandle(EventHandle);
    }

    const int IOCTL_SET_FILTER = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x804 << 2 | METHOD_BUFFERED;
    const int IOCTL_SET_EVENT = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x810 << 2 | METHOD_BUFFERED;
    const int IOCTL_WRITE = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x820 << 2 | METHOD_BUFFERED;
    const int IOCTL_READ = FILE_DEVICE_UNKNOWN << 16 | FILE_ANY_ACCESS << 14 | 0x840 << 2 | METHOD_BUFFERED;
    const int ACCESS_MASK_GENERIC_READ = unchecked((int)0x80000000);
    const int FILE_CREATION_OPEN_EXISTS = 3;
    const nint INVALID_HANDLE_VALUE = -1;
    const int FILE_DEVICE_UNKNOWN = 0x22;
    const int FILE_ANY_ACCESS = 0;
    const int METHOD_BUFFERED = 0;

    [DllImport("kernel32")] static extern bool DeviceIoControl(nint device, int ioControlCode, void* inBuffer, long inBufferSize, void* outBuffer, long outBufferSize, int* bytesReturned, void* overlapped);
    [DllImport("kernel32")] static extern nint CreateFileA(byte* fileName, int desiredAccess, int shareMode, void* securityAttributes, int creationDisposition, int flags, nint template);
    [DllImport("kernel32")] static extern nint CreateEventA(nint eventAttributes, bool manualReset, bool initialState, byte* name);
    [DllImport("kernel32")] static extern bool CloseHandle(nint handle);
}

public unsafe struct Mouse
{
    public Device Device;

    public void Send(MouseStroke* stroke) => Device.Send(stroke);
    public bool Receive(MouseStroke* stroke) => Device.Receive(stroke);
}

public unsafe struct Keyboard
{
    public Device Device;

    public void Send(KeyStroke* stroke) => Device.Send(stroke);
    public bool Receive(KeyStroke* stroke) => Device.Receive(stroke);
}

public unsafe struct Context
{
    const int MaxKeyboards = 10;
    const int MaxMouses = 10;
    const int MaxDevices = MaxKeyboards + MaxMouses;

    public readonly Device* Devices;

    public Device* FirstDevice => Devices;
    public Device* LastDevice => FirstDevice + MaxDevices - 1;

    public Keyboard* Keyboards => (Keyboard*)Devices;
    public Keyboard* FirstKeyboard => Keyboards;
    public Keyboard* LastKeyboard => Keyboards + MaxKeyboards - 1;

    public Mouse* Mouses => (Mouse*)(Devices + MaxMouses);
    public Mouse* FirstMouse => Mouses;
    public Mouse* LastMouse => Mouses + MaxMouses - 1;

    public int GetIndexOfDevice(Device* device) => (int)(Devices - device);

    public Mouse* WaitMouseInput() => (Mouse*)WaitDeviceInput((Device*)FirstMouse, (Device*)LastMouse, INFINITE);
    public Keyboard* WaitKeyboardInput() => (Keyboard*)WaitDeviceInput((Device*)FirstKeyboard, (Device*)LastKeyboard, INFINITE);

    public void SetFilter(Filter filter) => SetFilter(FirstDevice, LastDevice, filter);
    public void SetMouseFilter(Filter filter) => SetFilter((Device*)FirstMouse, (Device*)LastMouse, filter);
    public void SetKeyboardFilter(Filter filter) => SetFilter((Device*)FirstKeyboard, (Device*)LastKeyboard, filter);

    void SetFilter(Device* firstDevice, Device* lastDevice, Filter filter)
    {
        for (var device = firstDevice; device < lastDevice; device++)
            device->SetFilter(filter);
    }

    [SkipLocalsInit]
    Device* WaitDeviceInput(Device* firstDevice, Device* lastDevice, int milliseconds)
    {
        var count = (int)(lastDevice - firstDevice + 1);
        var handles = stackalloc nint[count];
        var handle = handles;
        for (var device = firstDevice; device <= lastDevice; device++, handle++)
            *handle = device->EventHandle;

        var waitResult = WaitForMultipleObjects(count, handles, false, milliseconds);
        if (waitResult == WAIT_FAILED || waitResult == WAIT_TIMEOUT)
            return null;

        return firstDevice + waitResult;
    }

    public void Destroy()
    {
        for (var device = FirstDevice; device <= LastDevice; device++)
            device->Destoy();

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

        var eventHandleAligned = stackalloc nint[2];
        for (var deviceIndex = 0; deviceIndex < MaxDevices; deviceIndex++)
        {
            *deviceNameIndex = (short)('0' + deviceIndex / 10 | '0' + deviceIndex % 10 << 8);

            if (!context.Devices[deviceIndex].Initialize(deviceName, eventHandleAligned))
            {
                context.Destroy();
                return default;
            }
        }

        return context;
    }

    const int INFINITE = unchecked((int)0xFFFFFFFF);
    const int WAIT_FAILED = unchecked((int)0xFFFFFFFF);
    const int WAIT_TIMEOUT = 0x102;

    [DllImport("kernel32")] static extern int WaitForMultipleObjects(int count, nint* handles, bool waitAll, int milliseconds);
}