[Flags]
enum FileShareMode : uint
{
    None   = 0x00000000,
    Delete = 0x00000004,
    Read   = 0x00000001,
    Write  = 0x00000002,
}