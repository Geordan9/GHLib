using System.Globalization;

namespace GHLib.Util.Extensions;

internal static class StringExtension
{
    public static bool IsDecimalFormat(this string input)
    {
        return decimal.TryParse(input, out _);
    }

    public static bool IsHexFormat(this string input)
    {
        return long.TryParse(input.Replace(" ", string.Empty), NumberStyles.HexNumber, null, out _);
    }
}