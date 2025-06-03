namespace Interceptions.Internal;
public enum MouseFlag : ushort
{
    MoveRelative = 0x000,
    MoveAbsolute = 0x001,
    VirtualDesktop = 0x002,
    AttributesChanged = 0x004,
    MoveNoCoalesce = 0x008,
    TerminalServerSrcShadow = 0x100,
}