using System;
using GHLib.Utils;

namespace GHLib.Models
{
    public class HackSettings
    {
        private bool autoUpdateHacks = true;

        public bool AutoUpdateHacks
        {
            get => autoUpdateHacks;
            set
            {
                if (autoUpdateHacks != value)
                    autoUpdateHacks = value;

                if (Globals.GHSigScan == null) return;

                if (Globals.GHSigScan.Process == null) return;

                HackTools.AutoUpdateHacksTask();
            }
        }

        public TimeSpan ValueUpdateInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        public bool ReverseScan { get; set; }
    }
}