namespace InterceptionInternal;
public enum MouseState : ushort
{
    LeftButtonDown = 0x001,
    LeftButtonUp = 0x002,
    RightButtonDown = 0x004,
    RightButtonUp = 0x008,
    MiddleButtonDown = 0x010,
    MiddleButtonUp = 0x020,

    Button4Down = 0x040,
    Button4Up = 0x080,
    Button5Down = 0x100,
    Button5Up = 0x200,

    Wheel = 0x400,
    HWheel = 0x800
}