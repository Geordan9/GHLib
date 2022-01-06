using GHLib.Common.Enums;

namespace GHLib.Core.AoB;

public class AoBPointer
{
    public int Offset { get; set; }

    public AoBPointerType? PointerType { get; set; }
}