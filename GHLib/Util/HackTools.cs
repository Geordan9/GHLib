using System;
using System.Collections.Generic;
using System.Linq;
using GHLib.Common.Enums;
using GHLib.Core.AoB;
using GHLib.Core.Hack;
using GHLib.Util.Extensions;
using static GHLib.Util.AoBTools;
using static GHLib.Util.MemoryTools;
using static GHLib.Util.PointerTools;

namespace GHLib.Util;

public static class HackTools
{
    private static readonly Random GHRNG = new();

    private static Dictionary<string, Tuple<byte[], RandomType>> tempRandomIDValueDictionary;

    public static Hack[] Hacks { get; set; }

    public static string MemoryValueToString(HackMemory memory, HackValue hackValue)
    {
        if (hackValue.ByteSize == 0) hackValue.ByteSize = GetByteSize(hackValue);

        return MemoryTools.MemoryValueToString(memory, hackValue.Address, hackValue.ByteSize, hackValue.MemType,
            hackValue.MemValMod, hackValue.MemTypeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, HackValue hackValue)
    {
        if (hackValue.ByteSize == 0) hackValue.ByteSize = GetByteSize(hackValue);

        return MemoryTools.MemoryStringToBytes(memory, hackValue.Value, hackValue.Address, hackValue.ByteSize,
            hackValue.MemType, hackValue.MemValMod, hackValue.MemTypeModifiers);
    }

    public static IntPtr OffsetHack(HackMemory memory, IntPtr address, HackOffset hackOffset, bool is64Bit = false)
    {
        if (hackOffset.IsPointer)
            return GetPointer(memory, address, hackOffset.Offset, is64Bit);
        return address + hackOffset.Offset;
    }

    public static IntPtr OffsetHack(HackMemory memory, IntPtr address, HackOffset[] hackOffsets, bool is64Bit = false)
    {
        var addr = address;
        for (var i = 0; i < hackOffsets.Length; i++) addr = OffsetHack(memory, addr, hackOffsets[i], is64Bit);
        return addr;
    }

    public static HackValue[] GetHackValues(Hack[] hacks)
    {
        var hackValueList = new List<HackValue>();
        foreach (var h in hacks)
        {
            if (h is HackValue hackValue)
                hackValueList.Add(hackValue);
            if (h.ChildHacks != null && h.ChildHacks.Length != 0)
                hackValueList.AddRange(GetHackValues(h.ChildHacks));
        }

        return hackValueList.ToArray();
    }

    public static Hack[] GetAllHacks(Hack[] hacks)
    {
        var hackList = new List<Hack>();
        foreach (var h in hacks)
        {
            hackList.Add(h);
            if (h.ChildHacks != null && h.ChildHacks.Length != 0)
                hackList.AddRange(GetAllHacks(h.ChildHacks));
        }

        return hackList.ToArray();
    }

    public static void ReadjustHackParents(Hack hack)
    {
        if (hack.ChildHacks == null)
            return;

        var cha = hack.ChildHacks;
        hack.ChildHacks = new Hack[cha.Length];
        for (var i = 0; i < cha.Length; i++)
        {
            var h = cha[i];
            if (h is HackValue hInput)
            {
                var hi = new HackValue
                {
                    Name = h.Name,
                    Address = h.Address,
                    RelativeAddress = h.RelativeAddress,
                    AoBScripts = h.AoBScripts,
                    Offsets = h.Offsets,
                    Options = h.Options,
                    Parent = h.Parent,
                    ChildHacks = h.ChildHacks,
                    IsReadOnly = hInput.IsReadOnly,
                    MemType = hInput.MemType,
                    MemTypeModifiers = hInput.MemTypeModifiers,
                    MemValMod = hInput.MemValMod,
                    ByteSize = hInput.ByteSize
                };
                hack.ChildHacks[i] = hi;
            }
            else
            {
                hack.ChildHacks[i] = new Hack
                {
                    Name = h.Name,
                    Address = h.Address,
                    RelativeAddress = h.RelativeAddress,
                    AoBScripts = h.AoBScripts,
                    Offsets = h.Offsets,
                    Options = h.Options,
                    Parent = h.Parent,
                    ChildHacks = h.ChildHacks
                };
            }
        }

        foreach (var ch in hack.ChildHacks)
        {
            ReadjustHackParents(ch);
            ch.Parent = hack;
        }
    }

    public static int GetByteSize(HackValue hackValue)
    {
        return MemoryTools.GetByteSize((MemValueType) hackValue.MemType, hackValue.MemTypeModifiers);
    }

    public static bool ValidateValue(HackMemory memory, HackValue hackValue)
    {
        if (hackValue.Address == IntPtr.Zero)
        {
            DisableHack(memory, hackValue);
            return false;
        }

        var bytes = memory.ReadMemoryBytes(hackValue.Address, hackValue.ByteSize);

        return bytes != null;
    }

    public static void UpdateValue(HackMemory memory, HackValue hackValue, bool changed = false)
    {
        var value = MemoryValueToString(memory, hackValue);
        if (hackValue.Value != value &&
            value != null)
        {
            var hex = hackValue.MemValMod == MemValueModifier.Hexadecimal;
            if (changed && hackValue.Value != null &&
                (hackValue.Value.IsDecimalFormat() && !hex ||
                 hackValue.Value.IsHexFormat() && hex ||
                 hackValue.MemType == MemValueType.String))
            {
                memory.WriteMemoryBytes(hackValue.Address, MemoryStringToBytes(memory, hackValue));
                return;
            }

            hackValue.SetValue(memory, value);
        }
    }

    public static void UpdatePointer(HackMemory memory, HackValue hackValue, bool is64Bit = false)
    {
        if (hackValue.Parent == null && hackValue.Offsets != null && hackValue.Offsets.Length != 0)
            return;

        hackValue.Address = OffsetHack(memory, hackValue.Parent.Address, hackValue.Offsets, is64Bit);

        if (hackValue.Address == IntPtr.Zero)
            DisableHack(memory, hackValue);
    }

    public static void InitializeHack(HackScanner scanner, Hack hack, bool is64Bit = false,
        bool reverseScan = false)
    {
        is64Bit = scanner.Memory.Is64Bit(is64Bit);

        var hackValue = hack as HackValue;

        if (hack.Parent != null)
            if (hack.RelativeAddress && hack.Parent.Enabled)
                hack.Address = hack.Parent.Address;

        if (hack.AoBScripts != null && hack.AoBScripts.Length != 0)
        {
            var disabled = false;

            if (!hack.Enabled)
                Array.ForEach(hack.AoBScripts, aobs =>
                {
                    var enabled = ValidateAoBScript(scanner, aobs, hack.Address);
                    if (!enabled && reverseScan)
                    {
                        enabled = ValidateAoBScript(scanner, aobs, hack.Address, reverseScan);
                        if (enabled && hackValue == null) hack.Activated = true;
                    }

                    disabled |= !enabled;

                    if (!enabled)
                        return;

                    if (hackValue != null)
                        hackValue.Address = GetLastAoBScript(aobs).Address;
                });

            hack.Enabled = !disabled;

            if (hack.Address == IntPtr.Zero)
                hack.Address = hack.AoBScripts[0].Address;
        }

        if (hack.Offsets != null && hack.Offsets.Length != 0 && hack.Address != IntPtr.Zero)
            hack.Address = OffsetHack(scanner.Memory, hack.Address, hack.Offsets, is64Bit);

        if (hackValue != null)
        {
            if (hackValue.Address == IntPtr.Zero)
            {
                hackValue.Initialized = true;
                if (hackValue.ChildHacks != null && hackValue.ChildHacks.Length != 0)
                    foreach (var h in GetAllHacks(hackValue.ChildHacks))
                        h.Initialized = true;
                hackValue.Enabled = false;
                return;
            }

            if (hackValue.MemType != null)
            {
                var maxshift = is64Bit ? 63 : 31;
                if (hackValue.MemTypeModifiers != null && hackValue.MemTypeModifiers.Length > 1)
                    hackValue.MemTypeModifiers[1] = hackValue.MemTypeModifiers[1] > maxshift
                        ? maxshift
                        : hackValue.MemTypeModifiers[1];

                hackValue.Enabled = hackValue.IsValid(scanner.Memory);
                if (hackValue.Enabled) hackValue.Update(scanner.Memory);
            }
            else
            {
                hackValue.Enabled = true;
            }
        }

        hack.Initialized = true;

        if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
            foreach (var h in hack.ChildHacks)
                InitializeHack(scanner, h, reverseScan);
    }

    public static void ToggleHack(HackMemory memory, Hack hack, bool disableChildren = false)
    {
        var isHackValue = hack is HackValue;
        if (!isHackValue && (!hack.Enabled || !hack.Activated && disableChildren))
            return;

        if (disableChildren)
            hack.Activated = false;

        if (isHackValue)
        {
            if (((HackValue) hack).MemType != null && !disableChildren)
                hack.Activated = !hack.Activated;
        }
        else
        {
            tempRandomIDValueDictionary = new Dictionary<string, Tuple<byte[], RandomType>>();
            if (hack.AoBScripts != null && hack.AoBScripts.Length != 0)
                Array.ForEach(hack.AoBScripts, aobs =>
                {
                    if (aobs.AoB == null)
                    {
                        hack.Activated = !hack.Activated;
                        return;
                    }

                    ToggleAoB(memory, aobs, hack.Activated);
                });

            tempRandomIDValueDictionary = null;
        }

        if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
            foreach (var h in hack.ChildHacks)
                if (h.Activated || isHackValue)
                    ToggleHack(memory, h, true);
    }

    private static void ToggleAoB(HackMemory memory, AoBScript aobs, bool activated)
    {
        if (aobs.AoBReplacements != null && aobs.AoBReplacements.Length != 0)
            Array.ForEach(aobs.AoBReplacements, aobr =>
            {
                var offset = aobr.Offset;
                var newLength = aobr.RandomLength > 0 ? aobr.RandomLength : aobr.ReplaceAoB.Length;
                var oldbytes = new byte[newLength];
                Array.Copy(aobs.AoB, offset, oldbytes, 0, newLength);
                var replaceBytes = aobr.ReplaceAoB;
                if (!activated)
                {
                    replaceBytes = oldbytes;
                }
                else if (aobr.RandomLength > 0)
                {
                    replaceBytes = new byte[aobr.RandomLength];
                    if (!string.IsNullOrEmpty(aobr.RandomID) &&
                        tempRandomIDValueDictionary.ContainsKey(aobr.RandomID))
                    {
                        var val = tempRandomIDValueDictionary[aobr.RandomID];
                        replaceBytes = ConvertRandomType(val.Item1, val.Item2, aobr.RandomType.Value);
                    }
                    else
                    {
                        for (var i = 0; i < replaceBytes.Length; i++)
                            do
                            {
                                var ba = new byte[1];
                                GHRNG.NextBytes(ba);
                                replaceBytes[i] = ba[0];
                            } while (aobr.RandomType == RandomType.String &&
                                     !char.IsLetter((char) replaceBytes[i]) ||
                                     aobr.RandomType == RandomType.HexString &&
                                     (!((char) replaceBytes[i]).ToString().IsHexFormat() ||
                                      ((char) replaceBytes[i]).ToString().IsHexFormat() &&
                                      char.IsLetter((char) replaceBytes[i]) &&
                                      !char.IsUpper((char) replaceBytes[i])));

                        if (!string.IsNullOrEmpty(aobr.RandomID) &&
                            !tempRandomIDValueDictionary.ContainsKey(aobr.RandomID))
                            tempRandomIDValueDictionary.Add(aobr.RandomID,
                                new Tuple<byte[], RandomType>(replaceBytes, aobr.RandomType.Value));
                    }
                }

                memory.WriteMemoryBytes(aobs.Address, replaceBytes, aobr.Offset);
            });

        if (aobs.AoBScripts != null && aobs.AoBScripts.Length != 0)
            Array.ForEach(aobs.AoBScripts, saobs => { ToggleAoB(memory, saobs, activated); });
    }

    public static void DisableHacks(HackMemory memory, bool uninitialize = false)
    {
        if (Hacks != null && Hacks.Length != 0)
            foreach (var h in Hacks)
                DisableHack(memory, h, uninitialize);
    }

    private static void DisableHack(HackMemory memory, Hack hack, bool uninitialize = false)
    {
        hack.Address = IntPtr.Zero;
        hack.Enabled = hack.Activated = false;
        if (uninitialize)
            hack.Initialized = false;
        if (hack is HackValue hackValue)
            if (hackValue.Value != null)
                hackValue.SetValue(memory, null);

        if (hack.ChildHacks != null && hack.ChildHacks.Length != 0)
            foreach (var h in hack.ChildHacks)
                DisableHack(memory, h, uninitialize);
    }

    private static byte[] ConvertRandomType(byte[] bytes, RandomType origType, RandomType newType)
    {
        if (origType == newType)
            return bytes;

        byte[] newBytes = null;

        if (origType == RandomType.Byte && newType == RandomType.HexString)
            newBytes = BitConverter.ToString(bytes).Replace("-", string.Empty).ToCharArray().Select(c => (byte) c)
                .ToArray();
        else if (origType == RandomType.HexString && newType == RandomType.Byte)
            newBytes = HexStringToByteArray(new string(bytes.Select(b => (char) b).ToArray()));

        return newBytes;
    }
}