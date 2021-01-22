using GHLib.Models;
using static GHLib.Utils.HackTools;

namespace GHLib.Utils.Extensions
{
    public static class HackItemExtensions
    {
        public static void Initialize(this Hack hackItem)
        {
            InitializeHack(hackItem);
        }
    }
}