using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InterceptionInternal;
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

    [SkipLocalsInit]
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