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