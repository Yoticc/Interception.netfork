namespace Interception;
public enum FilterKeyState : ushort
{
    None = 0x0000,
    All = 0xFFFF,

    Down = KeyState.Up,
    Up = KeyState.Up << 1,
    E0 = KeyState.E0 << 1,
    E1 = KeyState.E1 << 1,

    TerminalServerSetLed = KeyState.TerminalServerSetLed << 1,
    TerminalServerShadow = KeyState.TerminalServerShadow << 1,
    TerminalServerVkPacket = KeyState.TerminalServerVkPacket << 1
}