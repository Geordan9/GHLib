using GHLib.Models;
using static GHLib.Utils.HackTools;

namespace GHLib.Utils.Extensions
{
    public static class HackItemValueExtensions
    {
        public static bool IsValid(this HackInput hackItemValue)
        {
            return ValidateValue(hackItemValue);
        }

        public static void Update(this HackInput hackItemValue, bool changed = false)
        {
            UpdateValue(hackItemValue, changed);
        }
    }
}