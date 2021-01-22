using System;

namespace GHLib.Models
{
    public class AoBScript
    {
        public IntPtr Address { get; set; }

        public string AoBString { get; set; }

        public byte[] AoB { get; set; }

        public AoBReplacement[] AoBReplacements { get; set; }

        public AoBScript[] AoBScripts { get; set; }

        public AoBPointer AoBPointer { get; set; }

        public bool IsRelative { get; set; } = true;

        public AoBScript Clone()
        {
            return (AoBScript)MemberwiseClone();
        }
    }
}