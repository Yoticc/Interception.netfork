using System.Runtime.InteropServices;

namespace InterceptionInternal;

[StructLayout(LayoutKind.Explicit, Size = 0x0C)]
public struct KeyStroke
{
    [FieldOffset(0x02)] public ushort Code;
    [FieldOffset(0x04)] public KeyState State;
    [FieldOffset(0x08)] public uint Information;
}