namespace Interception;
public enum KeyState : ushort
{
    Down = 0x00,
    Up = 0x01,
    E0 = 0x02,
    E1 = 0x04,

    TerminalServerSetLed = 0x08,
    TerminalServerShadow = 0x10,
    TerminalServerVkPacket = 0x20,
}