using GHLib.Common.Enums;

namespace GHLib.Core.AoB;

public class AoBReplacement
{
    public byte[] ReplaceAoB { get; set; } = new byte[0];

    public int RandomLength { get; set; }

    public RandomType? RandomType { get; set; }

    public string RandomID { get; set; } = string.Empty;

    public int Offset { get; set; }
}