using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GHLib.Common.Enums;
using GHLib.Core.AoB;
using GHLib.Core.Hack;

namespace GHLib.Util;

public static class GHBinaryTools
{
    private static readonly byte[] MagicBytes = {0x47, 0x48, 0x4D, 0x00};

    private static bool CompiledAoBStrings;

    private static bool ObfuscatedStrings;

    private static Hack[] HackDuplicates;

    private static Hack[][] ChildHackDuplicates;

    private static AoBScript[] AoBScriptDuplicates;

    private static AoBScript[][] AoBScriptsDuplicates;

    private static AoBReplacement[] AoBReplacementDuplicates;

    private static AoBReplacement[][] AoBReplacementsDuplicates;

    private static AoBPointer[] AoBPointerDuplicates;

    private static HackOffset[] HackOffsetDuplicates;

    private static HackOffset[][] HackOffsetsDuplicates;

    private static HackOptions[] HackOptionsDuplicates;

    #region Read Methods

    public static int VersionNumber = -1;

    public static HackCatagory[] ReadBinaryHackModule(string path)
    {
        return ReadBinaryHackModule(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
    }

    public static HackCatagory[] ReadBinaryHackModule(Stream stream)
    {
        using var reader = new BinaryReader(stream);
        if (!reader.ReadBytes(4).SequenceEqual(MagicBytes))
        {
            reader.Close();
            return null;
        }

        VersionNumber = reader.ReadInt32();
        CompiledAoBStrings = reader.ReadBoolean();
        ObfuscatedStrings = reader.ReadBoolean();
        ReadDuplicates(reader);
        var hackCatagoryList = new List<HackCatagory>();
        while (reader.ReadByte() == 0 && reader.BaseStream.Position < reader.BaseStream.Length)
            hackCatagoryList.Add(ReadHackCatagoryBytes(reader));

        reader.Close();

        var hackCatagoryArray = hackCatagoryList.ToArray();
        foreach (var hc in hackCatagoryArray)
        foreach (var hg in hc.HackGroups)
        foreach (var h in hg.Hacks)
            HackTools.ReadjustHackParents(h);

        HackDuplicates = null;
        ChildHackDuplicates = null;
        AoBScriptDuplicates = null;
        AoBScriptsDuplicates = null;
        AoBReplacementDuplicates = null;
        AoBReplacementsDuplicates = null;
        AoBPointerDuplicates = null;
        HackOffsetDuplicates = null;
        HackOffsetsDuplicates = null;
        HackOptionsDuplicates = null;

        return hackCatagoryArray;
    }

    #region Read Duplicates

    private static void ReadDuplicates(BinaryReader reader)
    {
        HackDuplicates = null;
        ChildHackDuplicates = null;
        AoBScriptDuplicates = null;
        AoBScriptsDuplicates = null;
        AoBReplacementDuplicates = null;
        AoBReplacementsDuplicates = null;
        AoBPointerDuplicates = null;
        HackOffsetDuplicates = null;
        HackOffsetsDuplicates = null;
        HackOptionsDuplicates = null;

        HackOptionsDuplicates = ReadHackOptionsDuplicates(reader);

        HackOffsetDuplicates = ReadHackOffsetDuplicates(reader);

        var count = reader.ReadInt32();

        if (count != -1)
        {
            var hackOffsetsDuplicateArray = new HackOffset[count][];
            for (var i = 0; i < hackOffsetsDuplicateArray.Length; i++)
                hackOffsetsDuplicateArray[i] = ReadHackOffsetDuplicates(reader);

            HackOffsetsDuplicates = hackOffsetsDuplicateArray;
        }

        AoBPointerDuplicates = ReadAoBPointerDuplicates(reader);

        AoBReplacementDuplicates = ReadAoBReplacementDuplicates(reader);

        count = reader.ReadInt32();

        if (count != -1)
        {
            var aobReplacementsDuplicateArray = new AoBReplacement[count][];
            for (var i = 0; i < aobReplacementsDuplicateArray.Length; i++)
                aobReplacementsDuplicateArray[i] = ReadAoBReplacementDuplicates(reader);

            AoBReplacementsDuplicates = aobReplacementsDuplicateArray;
        }

        AoBScriptDuplicates = ReadAoBScriptDuplicates(reader);

        count = reader.ReadInt32();

        if (count != -1)
        {
            AoBScriptsDuplicates = new AoBScript[count][];
            for (var i = 0; i < AoBScriptsDuplicates.Length; i++)
                AoBScriptsDuplicates[i] = ReadAoBScriptDuplicates(reader);
        }


        count = reader.ReadInt32();

        if (count != -1)
        {
            ChildHackDuplicates = new Hack[count][];
            for (var i = 0; i < ChildHackDuplicates.Length; i++)
                ChildHackDuplicates[i] = ReadHackDuplicates(reader);
        }

        HackDuplicates = ReadHackDuplicates(reader);
    }

    private static Hack[] ReadHackDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var hackDuplicateArray = new Hack[count];
        for (var i = 0; i < hackDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            if (val == 0xFE)
            {
                var hack = HackDuplicates[reader.ReadByte()];
                hackDuplicateArray[i] = hack is HackValue hackValue
                    ? new HackValue
                    {
                        Name = hack.Name,
                        Address = hack.Address,
                        RelativeAddress = hack.RelativeAddress,
                        AoBScripts = hack.AoBScripts,
                        Offsets = hack.Offsets,
                        Options = hackValue.Options,
                        Parent = hack.Parent,
                        ChildHacks = hack.ChildHacks,
                        IsReadOnly = hackValue.IsReadOnly,
                        MemType = hackValue.MemType,
                        MemTypeModifiers = hackValue.MemTypeModifiers,
                        MemValMod = hackValue.MemValMod,
                        ByteSize = hackValue.ByteSize
                    }
                    : new Hack
                    {
                        Name = hack.Name,
                        Address = hack.Address,
                        RelativeAddress = hack.RelativeAddress,
                        AoBScripts = hack.AoBScripts,
                        Offsets = hack.Offsets,
                        Options = hack.Options,
                        Parent = hack.Parent,
                        ChildHacks = hack.ChildHacks
                    };
            }
            else
            {
                hackDuplicateArray[i] = ReadHackBytes(reader, val == 3);
            }
        }

        return hackDuplicateArray;
    }

    private static AoBScript[] ReadAoBScriptDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var aobScriptDuplicateArray = new AoBScript[count];
        for (var i = 0; i < aobScriptDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            var aobs = val == 0xFC
                ? AoBScriptDuplicates[reader.ReadByte()]
                : ReadAoBScriptBytes(reader);
            aobScriptDuplicateArray[i] = new AoBScript
            {
                Address = aobs.Address,
                Offset = aobs.Offset,
                Module = aobs.Module,
                ModuleIndex = aobs.ModuleIndex,
                AoBString = aobs.AoBString,
                AoB = aobs.AoB,
                AoBReplacements = aobs.AoBReplacements,
                AoBScripts = aobs.AoBScripts,
                AoBPointer = aobs.AoBPointer,
                IsRelative = aobs.IsRelative
            };
        }

        return aobScriptDuplicateArray;
    }

    private static AoBReplacement[] ReadAoBReplacementDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var aobReplacementDuplicateArray = new AoBReplacement[count];
        for (var i = 0; i < aobReplacementDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            if (val == 0xFA)
                aobReplacementDuplicateArray[i] = AoBReplacementDuplicates[reader.ReadByte()];
            else
                aobReplacementDuplicateArray[i] = ReadAoBReplacementBytes(reader);
        }

        return aobReplacementDuplicateArray;
    }

    private static AoBPointer[] ReadAoBPointerDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var aobPointerDuplicateArray = new AoBPointer[count];
        for (var i = 0; i < aobPointerDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            if (val == 0xF8)
                aobPointerDuplicateArray[i] = AoBPointerDuplicates[reader.ReadByte()];
            else
                aobPointerDuplicateArray[i] = ReadAoBPointerBytes(reader);
        }

        return aobPointerDuplicateArray;
    }

    private static HackOffset[] ReadHackOffsetDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var HackOffsetDuplicateArray = new HackOffset[count];
        for (var i = 0; i < HackOffsetDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            if (val == 0xF7)
                HackOffsetDuplicateArray[i] = HackOffsetDuplicates[reader.ReadByte()];
            else
                HackOffsetDuplicateArray[i] = ReadHackOffsetBytes(reader);
        }

        return HackOffsetDuplicateArray;
    }

    private static HackOptions[] ReadHackOptionsDuplicates(BinaryReader reader)
    {
        var count = reader.ReadInt32();
        if (count == -1)
            return null;
        var hackOptionsDuplicateArray = new HackOptions[count];
        for (var i = 0; i < hackOptionsDuplicateArray.Length; i++)
        {
            var val = reader.ReadByte();
            if (val == 0xF5)
                hackOptionsDuplicateArray[i] = HackOptionsDuplicates[reader.ReadByte()];
            else
                hackOptionsDuplicateArray[i] = ReadHackOptionsBytes(reader);
        }

        return hackOptionsDuplicateArray;
    }

    #endregion

    private static HackCatagory ReadHackCatagoryBytes(BinaryReader reader)
    {
        var hackCatagory = new HackCatagory
        {
            Name = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString()
        };
        var hackGroupList = new List<HackGroup>();
        var nothing = true;
        while (reader.ReadByte() == 1 && reader.BaseStream.Position < reader.BaseStream.Length)
        {
            nothing = false;
            hackGroupList.Add(ReadHackGroupBytes(reader));
        }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        hackCatagory.HackGroups = hackGroupList.ToArray();

        return hackCatagory;
    }

    private static HackGroup ReadHackGroupBytes(BinaryReader reader)
    {
        var hackGroup = new HackGroup
        {
            Name = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString()
        };
        var hackList = new List<Hack>();
        var nothing = true;
        var b = reader.ReadByte();
        while ((b == 2 || b == 3 || b == 0xFE) && reader.BaseStream.Position < reader.BaseStream.Length)
        {
            nothing = false;
            if (b == 0xFE)
            {
                var hack = HackDuplicates[reader.ReadByte()];
                hackList.Add(hack is HackValue hackValue
                    ? new HackValue
                    {
                        Name = hack.Name,
                        Address = hack.Address,
                        RelativeAddress = hack.RelativeAddress,
                        AoBScripts = hack.AoBScripts,
                        Offsets = hack.Offsets,
                        Options = hackValue.Options,
                        Parent = hack.Parent,
                        ChildHacks = hack.ChildHacks,
                        IsReadOnly = hackValue.IsReadOnly,
                        MemType = hackValue.MemType,
                        MemTypeModifiers = hackValue.MemTypeModifiers,
                        MemValMod = hackValue.MemValMod,
                        ByteSize = hackValue.ByteSize
                    }
                    : new Hack
                    {
                        Name = hack.Name,
                        Address = hack.Address,
                        RelativeAddress = hack.RelativeAddress,
                        AoBScripts = hack.AoBScripts,
                        Offsets = hack.Offsets,
                        Options = hack.Options,
                        Parent = hack.Parent,
                        ChildHacks = hack.ChildHacks
                    });
            }
            else
            {
                hackList.Add(ReadHackBytes(reader, b == 3));
            }

            b = reader.ReadByte();
        }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        hackGroup.Hacks = hackList.ToArray();

        return hackGroup;
    }

    private static Hack ReadHackBytes(BinaryReader reader, bool isInput = false)
    {
        var hack = isInput ? new HackValue() : new Hack();
        hack.ID = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
        hack.Name = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
        hack.Description = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
        hack.Address = (IntPtr) reader.ReadInt64();
        hack.RelativeAddress = reader.ReadBoolean();
        if (isInput)
        {
            ((HackValue) hack).IsReadOnly = reader.ReadBoolean();
            var memType = reader.ReadByte();
            ((HackValue) hack).MemType = memType < 0xFF ? (MemValueType?) memType : null;
            var b = reader.ReadByte();
            if (b < 0xFF)
            {
                var intArray = new int[b];
                for (var i = 0; i < b; i++) intArray[i] = reader.ReadInt32();
            }

            var memValMod = reader.ReadByte();
            ((HackValue) hack).MemValMod = memValMod < 0xFF ? (MemValueModifier?) memValMod : null;

            var ddb = reader.ReadByte();
            if (ddb != 0xFF)
            {
                if (ddb == 0xF5)
                    ((HackValue) hack).Options = HackOptionsDuplicates[reader.ReadByte()];
                else if (ddb == 8)
                    ((HackValue) hack).Options = ReadHackOptionsBytes(reader);
            }
        }

        var nothing = true;

        var aobScriptList = new List<AoBScript>();
        var aobsb = reader.ReadByte();
        if (aobsb == 0xFB)
        {
            var aobScriptsDuplicate = AoBScriptsDuplicates[reader.ReadByte()];
            for (var i = 0; i < aobScriptsDuplicate.Length; i++)
                aobScriptList.Add(new AoBScript
                {
                    Address = aobScriptsDuplicate[i].Address,
                    Offset = aobScriptsDuplicate[i].Offset,
                    Module = aobScriptsDuplicate[i].Module,
                    ModuleIndex = aobScriptsDuplicate[i].ModuleIndex,
                    AoBString = aobScriptsDuplicate[i].AoBString,
                    AoB = aobScriptsDuplicate[i].AoB,
                    AoBReplacements = aobScriptsDuplicate[i].AoBReplacements,
                    AoBScripts = aobScriptsDuplicate[i].AoBScripts,
                    AoBPointer = aobScriptsDuplicate[i].AoBPointer,
                    IsRelative = aobScriptsDuplicate[i].IsRelative
                });
        }
        else if (aobsb != 0xFF)
        {
            while ((aobsb == 4 || aobsb == 0xFC) && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                nothing = false;
                var aobs = aobsb == 0xFC
                    ? AoBScriptDuplicates[reader.ReadByte()]
                    : ReadAoBScriptBytes(reader);

                aobScriptList.Add(new AoBScript
                {
                    Address = aobs.Address,
                    Offset = aobs.Offset,
                    Module = aobs.Module,
                    ModuleIndex = aobs.ModuleIndex,
                    AoBString = aobs.AoBString,
                    AoB = aobs.AoB,
                    AoBReplacements = aobs.AoBReplacements,
                    AoBScripts = aobs.AoBScripts,
                    AoBPointer = aobs.AoBPointer,
                    IsRelative = aobs.IsRelative
                });
                aobsb = reader.ReadByte();
            }
        }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        nothing = true;

        var hackOffsettList = new List<HackOffset>();
        var hob = reader.ReadByte();
        if (hob == 0xF6)
            hackOffsettList.AddRange(HackOffsetsDuplicates[reader.ReadByte()]);
        else if (hob != 0xFF)
            while ((hob == 6 || hob == 0xF7) && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                nothing = false;
                if (hob == 0xF7)
                    hackOffsettList.Add(HackOffsetDuplicates[reader.ReadByte()]);
                else
                    hackOffsettList.Add(ReadHackOffsetBytes(reader));
                hob = reader.ReadByte();
            }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        nothing = true;

        var childHackList = new List<Hack>();
        var chcount = reader.ReadByte();
        if (chcount == 0xFD)
        {
            var childHacksDuplicate = ChildHackDuplicates[reader.ReadByte()];
            for (var i = 0; i < childHacksDuplicate.Length; i++)
            {
                var chack = childHacksDuplicate[i];
                childHackList.Add(chack is HackValue chackValue
                    ? new HackValue
                    {
                        Name = chack.Name,
                        Address = chack.Address,
                        RelativeAddress = chack.RelativeAddress,
                        AoBScripts = chack.AoBScripts,
                        Offsets = chack.Offsets,
                        Options = chackValue.Options,
                        Parent = chack.Parent,
                        ChildHacks = chack.ChildHacks,
                        IsReadOnly = chackValue.IsReadOnly,
                        MemType = chackValue.MemType,
                        MemTypeModifiers = chackValue.MemTypeModifiers,
                        MemValMod = chackValue.MemValMod,
                        ByteSize = chackValue.ByteSize
                    }
                    : new Hack
                    {
                        Name = chack.Name,
                        Address = chack.Address,
                        RelativeAddress = chack.RelativeAddress,
                        AoBScripts = chack.AoBScripts,
                        Offsets = chack.Offsets,
                        Options = chack.Options,
                        Parent = chack.Parent,
                        ChildHacks = chack.ChildHacks
                    });
            }
        }
        else if (chcount != 0xFF)
        {
            var hb = reader.ReadByte();
            for (var i = 0;
                 i < chcount && (hb == 2 || hb == 3 || hb == 0xFE) &&
                 reader.BaseStream.Position < reader.BaseStream.Length;
                 i++)
            {
                nothing = false;
                if (hb == 0xFE)
                {
                    var chack = HackDuplicates[reader.ReadByte()];
                    childHackList.Add(chack is HackValue chackValue
                        ? new HackValue
                        {
                            Name = chack.Name,
                            Address = chack.Address,
                            RelativeAddress = chack.RelativeAddress,
                            AoBScripts = chack.AoBScripts,
                            Offsets = chack.Offsets,
                            Options = chackValue.Options,
                            Parent = chack.Parent,
                            ChildHacks = chack.ChildHacks,
                            IsReadOnly = chackValue.IsReadOnly,
                            MemType = chackValue.MemType,
                            MemTypeModifiers = chackValue.MemTypeModifiers,
                            MemValMod = chackValue.MemValMod,
                            ByteSize = chackValue.ByteSize
                        }
                        : new Hack
                        {
                            Name = chack.Name,
                            Address = chack.Address,
                            RelativeAddress = chack.RelativeAddress,
                            AoBScripts = chack.AoBScripts,
                            Offsets = chack.Offsets,
                            Options = chack.Options,
                            Parent = chack.Parent,
                            ChildHacks = chack.ChildHacks
                        });
                }
                else
                {
                    childHackList.Add(ReadHackBytes(reader, hb == 3));
                }

                hb = reader.ReadByte();
            }
        }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        hack.AoBScripts = aobScriptList.ToArray();
        hack.Offsets = hackOffsettList.ToArray();
        hack.ChildHacks = childHackList.ToArray();

        return hack;
    }

    private static AoBScript ReadAoBScriptBytes(BinaryReader reader)
    {
        var aobScript = new AoBScript
        {
            Address = (IntPtr) reader.ReadInt64(),
            Offset = reader.ReadInt32()
        };
        if (CompiledAoBStrings)
        {
            var wildcard = reader.ReadByte();
            var aob = reader.ReadBytes(reader.ReadInt32());
            aobScript.AoBString =
                BitConverter.ToString(aob).Replace("-", " ").Replace(wildcard.ToString("X2"), "*");
        }
        else
        {
            aobScript.AoBString =
                ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
        }

        if (aobScript.AoBString.Length > 0)
        {
            aobScript.Module =
                ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
            aobScript.ModuleIndex = reader.ReadInt32();
        }
        else
        {
            aobScript.Module = reader.ReadString();
        }

        aobScript.IsRelative = reader.ReadBoolean();

        var nothing = true;

        var aobReplacementList = new List<AoBReplacement>();

        var aobrb = reader.ReadByte();
        if (aobrb == 0xF9)
            aobReplacementList.AddRange(AoBReplacementsDuplicates[reader.ReadByte()]);
        else if (aobrb != 0xFF)
            while ((aobrb == 5 || aobrb == 0xFA) && reader.BaseStream.Position < reader.BaseStream.Length)
            {
                nothing = false;
                if (aobrb == 0xFA)
                    aobReplacementList.Add(AoBReplacementDuplicates[reader.ReadByte()]);
                else
                    aobReplacementList.Add(ReadAoBReplacementBytes(reader));
                aobrb = reader.ReadByte();
            }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);

        nothing = true;

        var aobScriptList = new List<AoBScript>();
        var ascount = reader.ReadByte();
        if (ascount == 0xFB)
        {
            var aobScriptsDuplicate = AoBScriptsDuplicates[reader.ReadByte()];
            for (var i = 0; i < aobScriptsDuplicate.Length; i++)
                aobScriptList.Add(new AoBScript
                {
                    Address = aobScriptsDuplicate[i].Address,
                    Offset = aobScriptsDuplicate[i].Offset,
                    Module = aobScriptsDuplicate[i].Module,
                    ModuleIndex = aobScriptsDuplicate[i].ModuleIndex,
                    AoBString = aobScriptsDuplicate[i].AoBString,
                    AoB = aobScriptsDuplicate[i].AoB,
                    AoBReplacements = aobScriptsDuplicate[i].AoBReplacements,
                    AoBScripts = aobScriptsDuplicate[i].AoBScripts,
                    AoBPointer = aobScriptsDuplicate[i].AoBPointer,
                    IsRelative = aobScriptsDuplicate[i].IsRelative
                });
        }
        else if (ascount != 0xFF)
        {
            var aobsb = reader.ReadByte();
            for (var i = 0;
                 i < ascount && (aobsb == 4 || aobsb == 0xFC) &&
                 reader.BaseStream.Position < reader.BaseStream.Length;
                 i++)
            {
                nothing = false;

                var aobs = aobsb == 0xFC ? AoBScriptDuplicates[reader.ReadByte()] : ReadAoBScriptBytes(reader);

                aobScriptList.Add(new AoBScript
                {
                    Address = aobs.Address,
                    Offset = aobs.Offset,
                    Module = aobs.Module,
                    ModuleIndex = aobs.ModuleIndex,
                    AoBString = aobs.AoBString,
                    AoB = aobs.AoB,
                    AoBReplacements = aobs.AoBReplacements,
                    AoBScripts = aobs.AoBScripts,
                    AoBPointer = aobs.AoBPointer,
                    IsRelative = aobs.IsRelative
                });

                aobsb = reader.ReadByte();
            }
        }

        if (!nothing) reader.BaseStream.Seek(-1, SeekOrigin.Current);


        var aobpb = reader.ReadByte();
        if (aobpb != 0xFF)
        {
            if (aobpb == 0xF8)
                aobScript.AoBPointer = AoBPointerDuplicates[reader.ReadByte()];
            else if (aobpb == 7)
                aobScript.AoBPointer = ReadAoBPointerBytes(reader);
        }

        aobScript.AoBReplacements = aobReplacementList.ToArray();
        aobScript.AoBScripts = aobScriptList.ToArray();

        return aobScript;
    }

    private static AoBReplacement ReadAoBReplacementBytes(BinaryReader reader)
    {
        var aobReplacement = new AoBReplacement
        {
            ReplaceAoB = reader.ReadBytes(reader.ReadInt32()),
            RandomLength = reader.ReadInt32(),
            RandomID = reader.ReadString(),
            Offset = reader.ReadInt32()
        };

        var b = reader.ReadByte();
        aobReplacement.RandomType = b < 0xFF ? (RandomType?) b : null;

        return aobReplacement;
    }

    private static AoBPointer ReadAoBPointerBytes(BinaryReader reader)
    {
        var aobPointer = new AoBPointer
        {
            Offset = reader.ReadInt32()
        };
        var b = reader.ReadByte();
        aobPointer.PointerType = b < 0xFF ? (AoBPointerType?) b : null;

        return aobPointer;
    }

    private static HackOffset ReadHackOffsetBytes(BinaryReader reader)
    {
        var hackOffset = new HackOffset
        {
            Offset = reader.ReadInt32(),
            IsPointer = reader.ReadBoolean()
        };

        return hackOffset;
    }

    private static HackOptions ReadHackOptionsBytes(BinaryReader reader)
    {
        var hackOptions = new HackOptions();
        var optionCount = reader.ReadInt32();
        for (var i = 0; i < optionCount; i++)
        {
            var key = ObfuscatedStrings ? GHDecrypt(reader.ReadBytes(reader.ReadInt32())) : reader.ReadString();
            var value = reader.ReadString();
            hackOptions.Options.Add(key, value);
        }

        hackOptions.DisallowManualInput = reader.ReadBoolean();

        return hackOptions;
    }

    #endregion

    #region GHCrypt

    private static string GHDecrypt(byte[] bytes)
    {
        if (bytes.Length == 0)
            return string.Empty;
        var changedBytes = new byte[bytes.Length];
        for (var i = 0; i < bytes.Length; i++) changedBytes[i] = ((byte) (bytes[i] ^ 7)).RotateRight(7);

        return Base64Decode(
            Encoding.ASCII.GetString(MemoryTools.HexStringToByteArray(Encoding.ASCII.GetString(changedBytes))));
    }

    private static byte RotateRight(this byte b, int bits)
    {
        return (byte) ((byte) (b >> bits) | (byte) (b << (8 - bits)));
    }

    public static string Base64Decode(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }

    #endregion
}