using System;
using GHLib.Common.Enums;
using GHLib.Core.Hack;
using static GHLib.Util.MemoryTools;

namespace GHLib.Util;

public static class PointerTools
{
    #region GetPointer

    public static IntPtr GetPointer(HackMemory memory, IntPtr address, int offset = 0, bool is64Bit = false)
    {
        var addr = (IntPtr) ByteArrayToLong(memory.ReadMemoryBytes(address, memory.Is64Bit(is64Bit) ? 0x8 : 0x4));
        if (addr == IntPtr.Zero)
            return addr;

        return (IntPtr) ((long) addr + offset - memory.ImageBase - memory.CodeOffset);
    }

    public static IntPtr GetPointer(HackMemory memory, IntPtr address, int[] offsets, bool is64Bit = false)
    {
        var addr = address;
        for (var i = 0; i < offsets.Length; i++) addr = GetPointer(memory, addr, offsets[i], is64Bit);
        return addr;
    }

    #endregion

    #region GetAsmPointer

    public static IntPtr GetNearAsmPointer(HackMemory memory, IntPtr address, int offset = 0)
    {
        return address + BitConverter.ToInt32(memory.ReadMemoryBytes(address, 0x4), 0) + 4 + offset;
    }

    public static IntPtr GetNearAsmPointer(HackMemory memory, IntPtr address, int[] offsets, bool is64Bit = false)
    {
        var addr = address;

        for (var i = 0; i < offsets.Length; i++)
            addr = i == 0 ? GetNearAsmPointer(memory, addr, offsets[i]) : GetPointer(memory, addr, offsets[i], is64Bit);

        return addr;
    }

    public static IntPtr GetFarAsmPointer(HackMemory memory, IntPtr address, int offset = 0, bool is64Bit = false)
    {
        var ptr = IntPtr.Zero;
        var addr = BitConverter.ToInt32(memory.ReadMemoryBytes(address, 0x4), 0);
        is64Bit = memory.Is64Bit(is64Bit);
        if (is64Bit)
        {
            ptr = address + addr + 4;
            ptr = GetPointer(memory, ptr, 0, is64Bit) + offset;
        }
        else
        {
            ptr += (int) (addr - memory.ImageBase - memory.CodeOffset);
        }

        return ptr;
    }

    public static IntPtr GetFarAsmPointer(HackMemory memory, IntPtr address, int[] offsets, bool is64Bit = false)
    {
        var addr = address;

        for (var i = 0; i < offsets.Length; i++)
            addr = i == 0
                ? GetFarAsmPointer(memory, addr, offsets[i], is64Bit)
                : GetPointer(memory, addr, offsets[i], is64Bit);

        return addr;
    }

    public static IntPtr GetCloseJumpAsmPointer(HackMemory memory, IntPtr address, int offset = 0)
    {
        return address + (sbyte) memory.ReadMemoryBytes(address, 0x1)[0] + 1 + offset;
    }

    public static IntPtr GetCloseJumpAsmPointer(HackMemory memory, IntPtr address, int[] offsets, bool is64Bit = false)
    {
        var addr = address;

        for (var i = 0; i < offsets.Length; i++)
            addr = i == 0
                ? GetCloseJumpAsmPointer(memory, addr, offsets[i])
                : GetPointer(memory, addr, offsets[i], is64Bit);

        return addr;
    }

    public static IntPtr GetFarJumpAsmPointer(HackMemory memory, IntPtr address, int offset = 0)
    {
        return GetNearAsmPointer(memory, address, offset);
    }

    public static IntPtr GetFarJumpAsmPointer(HackMemory memory, IntPtr address, int[] offsets, bool is64Bit = false)
    {
        return GetNearAsmPointer(memory, address, offsets, is64Bit);
    }

    public static IntPtr GetOffsetAsm(HackMemory memory, IntPtr address, IntPtr paddress, int offset = 0)
    {
        return paddress + BitConverter.ToInt32(memory.ReadMemoryBytes(address, 0x4), 0) + offset;
    }

    public static IntPtr GetOffsetAsm(HackMemory memory, IntPtr address, IntPtr paddress, int[] offsets,
        bool is64Bit = false)
    {
        var addr = address;

        for (var i = 0; i < offsets.Length; i++)
            addr = i == 0
                ? GetOffsetAsm(memory, addr, paddress, offsets[i])
                : GetPointer(memory, addr, offsets[i], is64Bit);

        return addr;
    }

    #endregion

    #region GetAoBPointer

    public static IntPtr GetAoBPointer(HackMemory memory, IntPtr address, AoBPointerType? aobPointerType,
        int offset = 0, bool is64Bit = false)
    {
        return GetAoBPointer(memory, address, IntPtr.Zero, aobPointerType, offset, is64Bit);
    }

    public static IntPtr GetAoBPointer(HackMemory memory, IntPtr address, AoBPointerType? aobPointerType, int[] offsets,
        bool is64Bit = false)
    {
        return GetAoBPointer(memory, address, IntPtr.Zero, aobPointerType, offsets, is64Bit);
    }

    public static IntPtr GetAoBPointer(HackMemory memory, IntPtr address, IntPtr paddress,
        AoBPointerType? aobPointerType,
        int offset = 0, bool is64Bit = false)
    {
        return aobPointerType switch
        {
            AoBPointerType.NearAddress => GetNearAsmPointer(memory, address, offset),
            AoBPointerType.FarAddress => GetFarAsmPointer(memory, address, offset),
            AoBPointerType.CloseJump => GetCloseJumpAsmPointer(memory, address, offset),
            AoBPointerType.FarJump => GetFarJumpAsmPointer(memory, address, offset),
            AoBPointerType.Offset => GetOffsetAsm(memory, address, paddress, offset),
            _ => GetPointer(memory, address, offset, is64Bit)
        };
    }

    public static IntPtr GetAoBPointer(HackMemory memory, IntPtr address, IntPtr paddress,
        AoBPointerType? aobPointerType,
        int[] offsets, bool is64Bit = false)
    {
        var addr = address;

        for (var i = 0; i < offsets.Length; i++)
            addr = i == 0
                ? GetAoBPointer(memory, addr, paddress, aobPointerType, offsets[i], is64Bit)
                : GetPointer(memory, addr, offsets[i], is64Bit);

        return addr;
    }

    #endregion
}