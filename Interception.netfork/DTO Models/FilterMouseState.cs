namespace InterceptionInternal;
public enum FilterMouseState : ushort
{
    None = 0x0000,
    All = 0xFFFF,

    LeftButtonDown = MouseState.LeftButtonDown,
    LeftButtonUp = MouseState.LeftButtonUp,
    RightButtonDown = MouseState.RightButtonDown,
    RightButtonUp = MouseState.RightButtonUp,
    MiddleButtonDown = MouseState.MiddleButtonDown,
    MiddleButtonUp = MouseState.MiddleButtonUp,

    Button4Down = MouseState.Button4Down,
    Button4Up = MouseState.Button4Up,
    Button5Down = MouseState.Button5Down,
    Button5Up = MouseState.Button5Up,

    Wheel = MouseState.Wheel,
    HWheel = MouseState.HWheel,

    Move = 0x1000
}