using System;
using GHLib.Common.Enums;
using static GHLib.Globals;
using static GHLib.Utils.MemoryTools;

namespace GHLib.Utils
{
    public static class PointerTools
    {
        #region GetPointer

        public static IntPtr GetPointer(IntPtr address, int offset = 0)
        {
            var addr = (IntPtr) ByteArrayToLong(ReadMemoryBytes(address, PtrSize));
            if (addr == IntPtr.Zero)
                return addr;
            return addr + offset;
        }

        public static IntPtr GetPointer(IntPtr address, int[] offsets)
        {
            var addr = address;
            for (var i = 0; i < offsets.Length; i++) addr = GetPointer(addr, offsets[i]);
            return addr;
        }

        #endregion

        #region GetAsmPointer

        public static IntPtr GetNearAsmPointer(IntPtr address, int offset = 0)
        {
            return address + BitConverter.ToInt32(ReadMemoryBytes(address, 0x4), 0) + 4 + offset;
        }

        public static IntPtr GetNearAsmPointer(IntPtr address, int[] offsets)
        {
            var addr = address;

            for (var i = 0; i < offsets.Length; i++)
                switch (i)
                {
                    case 0:
                        addr = GetNearAsmPointer(addr, offsets[i]);
                        break;
                    default:
                        addr = GetPointer(addr, offsets[i]);
                        break;
                }

            return addr;
        }

        public static IntPtr GetFarAsmPointer(IntPtr address, int offset = 0)
        {
            var ptr = IntPtr.Zero;
            var addr = BitConverter.ToInt32(ReadMemoryBytes(address, 0x4), 0);
            if (Is64Bit)
            {
                ptr = address + addr + 4;
                ptr = GetPointer(ptr) + offset;
            }
            else
            {
                ptr += addr;
            }

            return ptr;
        }

        public static IntPtr GetFarAsmPointer(IntPtr address, int[] offsets)
        {
            var addr = address;

            for (var i = 0; i < offsets.Length; i++)
                switch (i)
                {
                    case 0:
                        addr = GetFarAsmPointer(addr, offsets[i]);
                        break;
                    default:
                        addr = GetPointer(addr, offsets[i]);
                        break;
                }

            return addr;
        }

        public static IntPtr GetCloseJumpAsmPointer(IntPtr address, int offset = 0)
        {
            return address + BitConverter.ToInt32(ReadMemoryBytes(address, 0x1), 0) + 1 + offset;
        }

        public static IntPtr GetCloseJumpAsmPointer(IntPtr address, int[] offsets)
        {
            var addr = address;

            for (var i = 0; i < offsets.Length; i++)
                switch (i)
                {
                    case 0:
                        addr = GetCloseJumpAsmPointer(addr, offsets[i]);
                        break;
                    default:
                        addr = GetPointer(addr, offsets[i]);
                        break;
                }

            return addr;
        }

        public static IntPtr GetFarJumpAsmPointer(IntPtr address, int offset = 0)
        {
            return GetNearAsmPointer(address, offset);
        }

        public static IntPtr GetFarJumpAsmPointer(IntPtr address, int[] offsets)
        {
            return GetNearAsmPointer(address, offsets);
        }

        public static IntPtr GetOffsetAsm(IntPtr address, IntPtr paddress, int offset = 0)
        {
            return paddress + BitConverter.ToInt32(ReadMemoryBytes(address, 0x4), 0) + offset;
        }

        public static IntPtr GetOffsetAsm(IntPtr address, IntPtr paddress, int[] offsets)
        {
            var addr = address;

            for (var i = 0; i < offsets.Length; i++)
                switch (i)
                {
                    case 0:
                        addr = GetOffsetAsm(addr, paddress, offsets[i]);
                        break;
                    default:
                        addr = GetPointer(addr, offsets[i]);
                        break;
                }

            return addr;
        }

        #endregion

        #region GetAoBPointer

        public static IntPtr GetAoBPointer(IntPtr address, AoBPointerType? aobPointerType, int offset = 0)
        {
            return GetAoBPointer(address, IntPtr.Zero, aobPointerType, offset);
        }

        public static IntPtr GetAoBPointer(IntPtr address, AoBPointerType? aobPointerType, int[] offsets)
        {
            return GetAoBPointer(address, IntPtr.Zero, aobPointerType, offsets);
        }

        public static IntPtr GetAoBPointer(IntPtr address, IntPtr paddress, AoBPointerType? aobPointerType,
            int offset = 0)
        {
            switch (aobPointerType)
            {
                case AoBPointerType.NearAddress:
                    return GetNearAsmPointer(address, offset);
                case AoBPointerType.FarAddress:
                    return GetFarAsmPointer(address, offset);
                case AoBPointerType.CloseJump:
                    return GetCloseJumpAsmPointer(address, offset);
                case AoBPointerType.FarJump:
                    return GetFarJumpAsmPointer(address, offset);
                case AoBPointerType.Offset:
                    return GetOffsetAsm(address, paddress, offset);
                default:
                    return address + BitConverter.ToInt32(ReadMemoryBytes(address, PtrSize), 0) + offset;
            }
        }

        public static IntPtr GetAoBPointer(IntPtr address, IntPtr paddress, AoBPointerType? aobPointerType,
            int[] offsets)
        {
            var addr = address;

            for (var i = 0; i < offsets.Length; i++)
                switch (i)
                {
                    case 0:
                        addr = GetAoBPointer(addr, paddress, aobPointerType, offsets[i]);
                        break;
                    default:
                        addr = GetPointer(addr, offsets[i]);
                        break;
                }

            return addr;
        }

        #endregion
    }
}