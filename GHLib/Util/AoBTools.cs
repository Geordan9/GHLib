using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GHLib.Core.AoB;
using GHLib.Core.Hack;
using MemoryLib.Core;
using static GHLib.Util.PointerTools;

namespace GHLib.Util;

public static class AoBTools
{
    public static bool ValidateAoBScript(HackScanner scanner, AoBScript aobs, bool is64Bit = false,
        bool reverseScan = false)
    {
        return ValidateAoBScript(scanner, aobs, IntPtr.Zero, is64Bit, reverseScan);
    }

    public static bool ValidateAoBScript(HackScanner scanner, AoBScript aobs, IntPtr address, bool is64Bit = false,
        bool reverseScan = false)
    {
        var sigScan = string.IsNullOrWhiteSpace(aobs.Module)
            ? scanner.GetSigScan()
            : scanner.GetSigScans(aobs.Module).ElementAtOrDefault(aobs.ModuleIndex);

        var wildcard = CreateWildcard(aobs.AoBString);

        aobs.AoB = CreateAoBWithWildcard(aobs.AoBString, wildcard);

        var mask = CreateMask(aobs.AoB, wildcard);

        var AoB = new byte[aobs.AoB.Length];

        Array.Copy(aobs.AoB, AoB, aobs.AoB.Length);

        if (reverseScan && aobs.AoBReplacements != null && aobs.AoBReplacements.Length != 0)
            foreach (var replacement in aobs.AoBReplacements)
                Array.Copy(replacement.ReplaceAoB, 0, AoB, replacement.Offset, replacement.ReplaceAoB.Length);

        aobs.Address = sigScan.FindAoB(AoB, mask, aobs.IsRelative ? address : IntPtr.Zero, 0);

        if (aobs.Address == IntPtr.Zero)
            return false;

        aobs.Address += aobs.Offset;

        var aobExists = true;

        var bytes = scanner.Memory.ReadMemoryBytes(aobs.Address, aobs.AoB.Length);

        if (bytes != null)
        {
            if (aobs.AoBPointer != null)
            {
                var aobp = aobs.AoBPointer;
                aobs.Address = GetAoBPointer(scanner.Memory, aobs.Address + aobp.Offset, address, aobp.PointerType, 0,
                    scanner.Memory.Is64Bit(is64Bit));
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
                if (string.IsNullOrWhiteSpace(caobs.Module))
                    caobs.Module = aobs.Module;

                aobExists = ValidateAoBScript(scanner, caobs, aobs.Address, reverseScan);
                if (!aobExists && reverseScan)
                {
                    aobExists = ValidateAoBScript(scanner, caobs, aobs.Address, reverseScan);
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
        var aobScriptList = new List<AoBScript>();
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

    #region FindAoB

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        IntPtr address)
    {
        return sigScans.FindAoB(Module, AoBString, address, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        IntPtr address, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, address, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        long length)
    {
        return sigScans.FindAoB(Module, AoBString, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        long length, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        IntPtr address, long length, int offset)
    {
        var wildcard = CreateWildcard(AoBString);

        var AoB = CreateAoBWithWildcard(AoBString, wildcard);

        return sigScans.FindAoB(Module, AoB, wildcard, address, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask)
    {
        return sigScans.FindAoB(Module, AoBString, mask, IntPtr.Zero, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, mask, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, IntPtr address)
    {
        return sigScans.FindAoB(Module, AoBString, mask, address, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, IntPtr address, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, mask, address, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, long length)
    {
        return sigScans.FindAoB(Module, AoBString, mask, length, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, long length, int offset)
    {
        return sigScans.FindAoB(Module, AoBString, mask, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module,
        string AoBString,
        string mask, IntPtr address, long length,
        int offset)
    {
        var AoB = CreateAoBWithWildcard(AoBString, 0x00);

        return sigScans.FindAoB(Module, AoB, mask, address, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        int offset)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        IntPtr address)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, address, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        IntPtr address, int offset)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, address, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        long length)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        long length, int offset)
    {
        return sigScans.FindAoB(Module, AoB, wildcard, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        byte wildcard,
        IntPtr address, long length, int offset)
    {
        var mask = CreateMask(AoB, wildcard);

        return sigScans.FindAoB(Module, AoB, mask, address, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask)
    {
        return sigScans.FindAoB(Module, AoB, mask, IntPtr.Zero, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        int offset)
    {
        return sigScans.FindAoB(Module, AoB, mask, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        IntPtr address)
    {
        return sigScans.FindAoB(Module, AoB, mask, address, 0, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        IntPtr address, int offset)
    {
        return sigScans.FindAoB(Module, AoB, mask, address, 0, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        long length)
    {
        return sigScans.FindAoB(Module, AoB, mask, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        long length, int offset)
    {
        return sigScans.FindAoB(Module, AoB, mask, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this Dictionary<ProcessModule, SigScan> sigScans, ProcessModule Module, byte[] AoB,
        string mask,
        IntPtr address, long length, int offset)
    {
        return sigScans.ContainsKey(Module)
            ? sigScans[Module].FindAoB(AoB, mask, address, length, offset)
            : IntPtr.Zero;
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString)
    {
        return sigScan.FindAoB(AoBString, IntPtr.Zero);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, int offset)
    {
        return sigScan.FindAoB(AoBString, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, IntPtr address)
    {
        return sigScan.FindAoB(AoBString, address, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, IntPtr address, int offset)
    {
        return sigScan.FindAoB(AoBString, address, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, long length)
    {
        return sigScan.FindAoB(AoBString, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, long length, int offset)
    {
        return sigScan.FindAoB(AoBString, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, IntPtr address, long length, int offset)
    {
        var wildcard = CreateWildcard(AoBString);

        var AoB = CreateAoBWithWildcard(AoBString, wildcard);

        return sigScan.FindAoB(AoB, wildcard, address, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask)
    {
        return sigScan.FindAoB(AoBString, mask, IntPtr.Zero, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, int offset)
    {
        return sigScan.FindAoB(AoBString, mask, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, IntPtr address)
    {
        return sigScan.FindAoB(AoBString, mask, address, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, IntPtr address, int offset)
    {
        return sigScan.FindAoB(AoBString, mask, address, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, long length)
    {
        return sigScan.FindAoB(AoBString, mask, length, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, long length, int offset)
    {
        return sigScan.FindAoB(AoBString, mask, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, string AoBString, string mask, IntPtr address, long length,
        int offset)
    {
        var AoB = CreateAoBWithWildcard(AoBString, 0x00);

        return sigScan.FindAoB(AoB, mask, address, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard)
    {
        return sigScan.FindAoB(AoB, wildcard, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, int offset)
    {
        return sigScan.FindAoB(AoB, wildcard, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, IntPtr address)
    {
        return sigScan.FindAoB(AoB, wildcard, address, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, IntPtr address, int offset)
    {
        return sigScan.FindAoB(AoB, wildcard, address, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, long length)
    {
        return sigScan.FindAoB(AoB, wildcard, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, long length, int offset)
    {
        return sigScan.FindAoB(AoB, wildcard, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, byte wildcard, IntPtr address, long length,
        int offset)
    {
        var mask = CreateMask(AoB, wildcard);

        return sigScan.FindAoB(AoB, mask, address, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask)
    {
        return sigScan.FindAoB(AoB, mask, IntPtr.Zero, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, int offset)
    {
        return sigScan.FindAoB(AoB, mask, IntPtr.Zero, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, IntPtr address)
    {
        return sigScan.FindAoB(AoB, mask, address, 0, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, IntPtr address, int offset)
    {
        return sigScan.FindAoB(AoB, mask, address, 0, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, long length)
    {
        return sigScan.FindAoB(AoB, mask, IntPtr.Zero, length, 0);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, long length, int offset)
    {
        return sigScan.FindAoB(AoB, mask, IntPtr.Zero, length, offset);
    }

    public static IntPtr FindAoB(this SigScan sigScan, byte[] AoB, string mask, IntPtr address, long length, int offset)
    {
        return sigScan.FindPattern(AoB, mask, (long) address, length, offset);
    }

    #endregion
}