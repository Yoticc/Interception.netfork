using System.Runtime.InteropServices;

static unsafe class Kernel32
{
    const string kernel = "kernel32";

    [DllImport(kernel)] public static extern bool CloseHandle(nint objectHandle);
    [DllImport(kernel)] public static extern nint CreateFileA(
        byte* fileName,
        AccessMask desiredAccess, 
        FileShareMode shareMode, 
        SecurityAttributes* securityAttributes, 
        FileCreationDisposition creationDisposition, 
        FileFlagsAndAttributes flagsAndAttributes, 
        nint templateFile);
    [DllImport(kernel)] public static extern nint CreateEventA(nint eventAttributes, bool manualReset, bool initialState, byte* name);
    [DllImport(kernel)] public static extern bool DeviceIoControl(
        nint device, 
        int ioControlCode, 
        void* inBuffer,
        long inBufferSize,
        void* outBuffer,
        long outBufferSize,
        int* bytesReturned,
        NativeOverlapped* overlapped);
    [DllImport(kernel)] public static extern int WaitForMultipleObjects(int count, IntPtr* handles, bool waitAll, int milliseconds);
}