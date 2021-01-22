using GHLib.Utils;
using MemoryLib;
using ProcessLib.Utils;
using ProcessLib.Utils.Extensions;

namespace GHLib
{
    public static class Globals
    {
        public static readonly Memory GHMemory = new Memory();


        private static SigScan ghsigscan;

        public static SigScan GHSigScan
        {
            get => ghsigscan;
            set
            {
                ghsigscan = value;
                Is64Bit = Check64bit.is64bitOS && Check64bit.is64bitProcess;
                if (Is64Bit)
                    Is64Bit = !value.Process.IsWin64Emulator();

                value.Process.Exited += HackTools.Process_Exited;

                HackTools.AutoUpdateHacksTask();
            }
        }

        public static bool Is64Bit;

        internal static int PtrSize => Is64Bit ? 0x8 : 0x4;
    }
}