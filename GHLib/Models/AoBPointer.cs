using GHLib.Common.Enums;

namespace GHLib.Models
{
    public class AoBPointer
    {
        public int Offset { get; set; }

        public AoBPointerType? PointerType { get; set; }
    }
}