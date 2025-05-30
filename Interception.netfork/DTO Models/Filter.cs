namespace Interception;
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
