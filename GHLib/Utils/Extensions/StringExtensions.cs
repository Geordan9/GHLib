using System.Globalization;

namespace GHLib.Utils.Extensions
{
    internal static class StringExtensions
    {
        public static bool IsDecimalFormat(this string input)
        {
            long dummy;
            return long.TryParse(input, out dummy);
        }

        public static bool IsHexFormat(this string input)
        {
            long dummy;
            return long.TryParse(input.Replace(" ", string.Empty), NumberStyles.HexNumber, null, out dummy);
        }
    }
}