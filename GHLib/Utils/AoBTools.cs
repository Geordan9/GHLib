using System;
using System.Linq;
using GHLib.Models;
using static GHLib.Globals;
using static GHLib.Utils.PointerTools;
using static GHLib.Utils.MemoryTools;

namespace GHLib.Utils
{
    public static class AoBTools
    {
        #region FindAoB

        public static IntPtr FindAoB(string AoBString)
        {
            return FindAoB(AoBString, IntPtr.Zero, 0, 0);
        }

        public static IntPtr FindAoB(string AoBString, int offset)
        {
            return FindAoB(AoBString, IntPtr.Zero, 0, offset);
        }

        public static IntPtr FindAoB(string AoBString, IntPtr address)
        {
            return FindAoB(AoBString, address, 0, 0);
        }

        public static IntPtr FindAoB(string AoBString, IntPtr address, int offset)
        {
            return FindAoB(AoBString, address, 0, offset);
        }

        public static IntPtr FindAoB(string AoBString, long length)
        {
            return FindAoB(AoBString, IntPtr.Zero, length, 0);
        }

        public static IntPtr FindAoB(string AoBString, long length, int offset)
        {
            return FindAoB(AoBString, IntPtr.Zero, length, offset);
        }

        public static IntPtr FindAoB(string AoBString, IntPtr address, long length, int offset)
        {
            var wildcard = CreateWildcard(AoBString);

            var AoB = CreateAoBWithWildcard(AoBString, wildcard);

            return FindAoB(AoB, wildcard, address, length, offset);
        }

        public static IntPtr FindAoB(string AoBString, string mask)
        {
            return FindAoB(AoBString, mask, IntPtr.Zero, 0, 0);
        }

        public static IntPtr FindAoB(string AoBString, string mask, int offset)
        {
            return FindAoB(AoBString, mask, IntPtr.Zero, 0, offset);
        }

        public static IntPtr FindAoB(string AoBString, string mask, IntPtr address)
        {
            return FindAoB(AoBString, mask, address, 0, 0);
        }

        public static IntPtr FindAoB(string AoBString, string mask, IntPtr address, int offset)
        {
            return FindAoB(AoBString, mask, address, 0, offset);
        }

        public static IntPtr FindAoB(string AoBString, string mask, long length)
        {
            return FindAoB(AoBString, mask, IntPtr.Zero, length, 0);
        }

        public static IntPtr FindAoB(string AoBString, string mask, long length, int offset)
        {
            return FindAoB(AoBString, mask, IntPtr.Zero, length, offset);
        }

        public static IntPtr FindAoB(string AoBString, string mask, IntPtr address, long length, int offset)
        {
            var AoB = CreateAoBWithWildcard(AoBString, 0x00);

            return FindAoB(AoB, mask, address, length, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard)
        {
            return FindAoB(AoB, wildcard, IntPtr.Zero, 0, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, int offset)
        {
            return FindAoB(AoB, wildcard, IntPtr.Zero, 0, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, IntPtr address)
        {
            return FindAoB(AoB, wildcard, address, 0, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, IntPtr address, int offset)
        {
            return FindAoB(AoB, wildcard, address, 0, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, long length)
        {
            return FindAoB(AoB, wildcard, IntPtr.Zero, length, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, long length, int offset)
        {
            return FindAoB(AoB, wildcard, IntPtr.Zero, length, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, byte wildcard, IntPtr address, long length, int offset)
        {
            var mask = CreateMask(AoB, wildcard);

            return FindAoB(AoB, mask, address, length, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask)
        {
            return FindAoB(AoB, mask, IntPtr.Zero, 0, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, int offset)
        {
            return FindAoB(AoB, mask, IntPtr.Zero, 0, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, IntPtr address)
        {
            return FindAoB(AoB, mask, address, 0, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, IntPtr address, int offset)
        {
            return FindAoB(AoB, mask, address, 0, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, long length)
        {
            return FindAoB(AoB, mask, IntPtr.Zero, length, 0);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, long length, int offset)
        {
            return FindAoB(AoB, mask, IntPtr.Zero, length, offset);
        }

        public static IntPtr FindAoB(byte[] AoB, string mask, IntPtr address, long length, int offset)
        {
            return GHSigScan.FindPattern(AoB, mask, (long) address, length, offset);
        }

        #endregion

        public static bool CheckAoBScript(AoBScript aobs, bool reverseScan = false)
        {
            return CheckAoBScript(aobs, IntPtr.Zero, reverseScan);
        }

        public static bool CheckAoBScript(AoBScript aobs, IntPtr address,
            bool reverseScan = false)
        {
            var wildcard = CreateWildcard(aobs.AoBString);

            aobs.AoB = CreateAoBWithWildcard(aobs.AoBString, wildcard);

            var mask = CreateMask(aobs.AoB, wildcard);

            var AoB = new byte[aobs.AoB.Length];

            Array.Copy(aobs.AoB, AoB, aobs.AoB.Length);

            if (reverseScan && aobs.AoBReplacements != null && aobs.AoBReplacements.Length != 0)
                foreach (var replacement in aobs.AoBReplacements)
                    Array.Copy(replacement.ReplaceAoB, 0, AoB, replacement.Offset, replacement.ReplaceAoB.Length);

            aobs.Address = FindAoB(AoB, mask, aobs.IsRelative ? address : IntPtr.Zero, 0);

            if (aobs.Address == IntPtr.Zero)
                return false;

            var aobExists = true;

            var bytes = ReadMemoryBytes(aobs.Address, aobs.AoB.Length);

            if (bytes != null)
            {
                if (aobs.AoBPointer != null)
                {
                    var aobp = aobs.AoBPointer;
                    aobs.Address = GetAoBPointer(aobs.Address + aobp.Offset, address, aobp.PointerType);
                }

                if (reverseScan && aobs.AoBReplacements != null && aobs.AoBReplacements.Length != 0)
                    foreach (var replacement in aobs.AoBReplacements)
                        Array.Copy(aobs.AoB, replacement.Offset, bytes, replacement.Offset,
                            replacement.ReplaceAoB.Length);

                aobs.AoB = bytes;
            }

            if (aobs.AoBScripts != null && aobs.AoBScripts.Length != 0)
                Array.ForEach(aobs.AoBScripts, caobs =>
                {
                    aobExists = CheckAoBScript(caobs, aobs.Address, reverseScan);
                    if (!aobExists && reverseScan)
                    {
                        aobExists = CheckAoBScript(caobs, aobs.Address, reverseScan);
                        if (!aobExists)
                            return;
                    }
                });

            return aobExists;
        }

        internal static AoBScript GetLastAoBScript(AoBScript aobs)
        {
            if (aobs.AoBScripts != null && aobs.AoBScripts.Length != 0)
                return GetLastAoBScript(aobs.AoBScripts.Last());
            return aobs;
        }

        public static AoBScript[] GetAllAobScripts(AoBScript[] aobScripts)
        {
            var aobScriptList = new System.Collections.Generic.List<AoBScript>();
            foreach (var aobs in aobScripts)
            {
                aobScriptList.Add(aobs);
                if (aobs.AoBScripts != null && aobs.AoBScripts.Length != 0)
                    aobScriptList.AddRange(GetAllAobScripts(aobs.AoBScripts));
            }

            return aobScriptList.ToArray();
        }

        private static byte CreateWildcard(string AoBString)
        {
            byte wildcard = 0x00;

            var bytes = AoBString.Split(' ')
                .Where(bc => bc != "*" && bc != "?")
                .Select(bc => Convert.ToByte(bc, 16)).ToArray();

            for (byte i = 0xFF; i > 0x00; i--)
                if (!Array.Exists(bytes, b => b == i))
                {
                    wildcard = i;
                    break;
                }

            return wildcard;
        }

        private static byte[] CreateAoBWithWildcard(string AoBString, byte wildcard)
        {
            return AoBString.Split(' ').Select(bc =>
            {
                if (bc == "*" || bc == "?") return wildcard;
                return Convert.ToByte(bc, 16);
            }).ToArray();
        }

        private static string CreateMask(byte[] AoB, byte wildcard)
        {
            var mask = string.Empty;
            Array.ForEach(AoB, b =>
                mask += b == wildcard ? '?' : 'x'
            );
            return mask;
        }
    }
}