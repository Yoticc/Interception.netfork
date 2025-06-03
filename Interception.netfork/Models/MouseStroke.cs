using System.Runtime.InteropServices;

namespace Interceptions.Internal;
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