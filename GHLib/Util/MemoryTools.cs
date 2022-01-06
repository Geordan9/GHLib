using System;
using System.Linq;
using System.Text;
using GHLib.Common.Enums;
using GHLib.Core.Hack;

namespace GHLib.Util;

public static class MemoryTools
{
    public static int GetByteSize(MemValueType memValueType, bool is64Bit = false)
    {
        return GetByteSize(memValueType, null, is64Bit);
    }

    public static int GetByteSize(MemValueType memValueType, int[] typeModifiers, bool is64Bit = false)
    {
        var ptrSize = is64Bit ? 0x8 : 0x4;
        var isByteArray = memValueType == MemValueType.ByteArray;
        var isString = memValueType == MemValueType.String;
        if (isByteArray || isString)
            throw new ArgumentException(
                $"{(isByteArray ? "Byte Array" : "String")} value type requires type modifiers.");

        var byteSize = 0;

        switch (memValueType)
        {
            case MemValueType.Binary:
                if (typeModifiers == null)
                    break;
                byteSize = (typeModifiers[0] + typeModifiers[1]) / 8 + 1;
                byteSize = byteSize > ptrSize ? ptrSize : byteSize;
                break;
            case MemValueType.Byte:
                byteSize = 1;
                break;
            case MemValueType.TwoBytes:
                byteSize = 2;
                break;
            case MemValueType.FourBytes:
            case MemValueType.Float:
                byteSize = 4;
                break;
            case MemValueType.EightBytes:
            case MemValueType.Double:
                byteSize = 8;
                break;
            case MemValueType.String:
            case MemValueType.ByteArray:
                if (typeModifiers == null)
                    break;
                byteSize = typeModifiers[0];
                break;
        }

        return byteSize;
    }

    internal static long ByteArrayToLong(byte[] bytes, bool is64Bit = false)
    {
        Array.Resize(ref bytes, 8);
        long value = BitConverter.ToInt32(bytes, 0);
        if (is64Bit)
        {
            long valueext = BitConverter.ToInt32(bytes, 4);
            value = (valueext << 32) | (value & 0xFFFFFFFFL);
        }

        return value;
    }

    internal static ulong ByteArrayToULong(byte[] bytes, bool is64Bit = false)
    {
        Array.Resize(ref bytes, 8);
        ulong value = BitConverter.ToUInt32(bytes, 0);
        if (is64Bit)
        {
            ulong valueext = BitConverter.ToUInt32(bytes, 4);
            value = (valueext << 32) | (value & 0xFFFFFFFFL);
        }

        return value;
    }

    internal static byte[] HexStringToByteArray(string hex)
    {
        var str = hex.Replace(" ", string.Empty);
        str = str[0] == '#' ? str.Substring(1) : str;
        var bytes = new byte[str.Length >> 1];
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
        return bytes;
    }

    internal static long HexLiteralToLong(string hex)
    {
        if (string.IsNullOrEmpty(hex)) return 0;

        var i = hex.Length > 1 && hex[0] == '0' && (hex[1] == 'x' || hex[1] == 'X') ? 2 : 0;
        long value = 0;

        while (i < hex.Length)
        {
            uint x = hex[i++];

            if (x >= '0' && x <= '9') x -= '0';
            else if (x >= 'A' && x <= 'F') x = x - 'A' + 10;
            else if (x >= 'a' && x <= 'f') x = x - 'a' + 10;
            else throw new ArgumentOutOfRangeException("hex");

            value = 16 * value + x;
        }

        return value;
    }

    #region MemoryValueToString

    public static string MemoryValueToString(HackMemory memory, IntPtr address, MemValueType memValueType)
    {
        return MemoryValueToString(memory, address, memValueType, null, null);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, MemValueType memValueType,
        MemValueModifier? valueModifier)
    {
        return MemoryValueToString(memory, address, memValueType, valueModifier, null);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, MemValueType memValueType,
        int[] typeModifiers)
    {
        return MemoryValueToString(memory, address, memValueType, null, typeModifiers);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, MemValueType memValueType,
        MemValueModifier? valueModifier, int[] typeModifiers)
    {
        return MemoryValueToString(memory, address, GetByteSize(memValueType, typeModifiers), memValueType,
            valueModifier,
            typeModifiers);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, int byteSize,
        MemValueType? memValueType)
    {
        return MemoryValueToString(memory, address, byteSize, memValueType, null, null);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, int byteSize,
        MemValueType? memValueType,
        MemValueModifier? valueModifier)
    {
        return MemoryValueToString(memory, address, byteSize, memValueType, valueModifier, null);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, int byteSize,
        MemValueType? memValueType,
        int[] typeModifiers)
    {
        return MemoryValueToString(memory, address, byteSize, memValueType, null, typeModifiers);
    }

    public static string MemoryValueToString(HackMemory memory, IntPtr address, int byteSize,
        MemValueType? memValueType,
        MemValueModifier? valueModifier,
        int[] typeModifiers)
    {
        var bytes = memory.ReadMemoryBytes(address, byteSize);

        string value = null;

        if (bytes == null)
            return value;

        var hex = valueModifier == MemValueModifier.Hexadecimal;

        var sign = valueModifier == MemValueModifier.Signed;

        var format = hex ? 16 : 10;

        switch (memValueType)
        {
            case MemValueType.Binary:
                long mask = ((1 << typeModifiers[1]) - 1) << typeModifiers[0];
                var val = (ByteArrayToLong(bytes) & mask) >> typeModifiers[0];
                value = Convert.ToString(val, format).ToUpper();
                break;
            case MemValueType.Byte:
                value = sign
                    ? Convert.ToString(Convert.ToSByte(bytes[0]), format)
                    : Convert.ToString(bytes[0], format);
                value = value.ToUpper().PadLeft(hex ? 2 : 0, '0');
                break;
            case MemValueType.TwoBytes:
                value = sign
                    ? Convert.ToString(BitConverter.ToInt16(bytes, 0), format)
                    : Convert.ToString(BitConverter.ToUInt16(bytes, 0), format);
                value = value.ToUpper().PadLeft(hex ? 4 : 0, '0');
                break;
            case MemValueType.FourBytes:
                value = sign
                    ? Convert.ToString(BitConverter.ToInt32(bytes, 0), format)
                    : Convert.ToString(BitConverter.ToUInt32(bytes, 0), format);
                value = value.ToUpper().PadLeft(hex ? 8 : 0, '0');
                break;
            case MemValueType.EightBytes:
                value = sign || hex
                    ? Convert.ToString(ByteArrayToLong(bytes), format)
                    : Convert.ToString(ByteArrayToULong(bytes));
                value = value.ToUpper().PadLeft(hex ? 16 : 0, '0');
                break;
            case MemValueType.Float:
                value = hex
                    ? Convert.ToString(BitConverter.ToInt32(bytes, 0), format).ToUpper().PadLeft(8, '0')
                    : BitConverter.ToSingle(bytes, 0).ToString("G10");
                break;
            case MemValueType.Double:
                value = hex
                    ? Convert.ToString(ByteArrayToLong(bytes), format).ToUpper().PadLeft(16, '0')
                    : BitConverter.ToDouble(bytes, 0).ToString("G10");
                break;
            case MemValueType.String:
                value = valueModifier == MemValueModifier.Unicode
                    ? Encoding.Unicode.GetString(bytes, 0, typeModifiers[0])
                    : Encoding.ASCII.GetString(bytes, 0, typeModifiers[0]);
                break;
            case MemValueType.ByteArray:
                value = hex
                    ? BitConverter.ToString(bytes).Replace("-", " ")
                    : string.Join(" ", bytes.Select(b => b.ToString()));
                break;
        }

        return value;
    }

    #endregion

    #region MemoryStringToBytes

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, MemValueType memValueType)
    {
        return MemoryStringToBytes(memory, value, memValueType, null, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, MemValueType memValueType,
        MemValueModifier? valueModifier)
    {
        return MemoryStringToBytes(memory, value, memValueType, valueModifier, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, MemValueType memValueType,
        int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, memValueType, null, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, MemValueType memValueType,
        MemValueModifier? valueModifier, int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, IntPtr.Zero, GetByteSize(memValueType, typeModifiers), memValueType,
            valueModifier, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address,
        MemValueType memValueType)
    {
        return MemoryStringToBytes(memory, value, address, memValueType, null, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address,
        MemValueType memValueType,
        MemValueModifier? valueModifier)
    {
        return MemoryStringToBytes(memory, value, address, memValueType, valueModifier, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address,
        MemValueType memValueType,
        int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, address, memValueType, null, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address,
        MemValueType memValueType,
        MemValueModifier? valueModifier, int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, address, GetByteSize(memValueType, typeModifiers), memValueType,
            valueModifier, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, int byteSize, MemValueType? memValueType)
    {
        return MemoryStringToBytes(memory, value, IntPtr.Zero, byteSize, memValueType, null, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, int byteSize, MemValueType? memValueType,
        MemValueModifier? valueModifier)
    {
        return MemoryStringToBytes(memory, value, IntPtr.Zero, byteSize, memValueType, valueModifier, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, int byteSize, MemValueType? memValueType,
        int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, IntPtr.Zero, byteSize, memValueType, null, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, int byteSize,
        MemValueType? memValueType, MemValueModifier? valueModifier,
        int[] typeModifiers)
    {
        if (memValueType == MemValueType.Binary)
            throw new ArgumentException("Binary memory type cannot be passed without original memory address.");
        return MemoryStringToBytes(memory, value, IntPtr.Zero, byteSize, memValueType, valueModifier, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address, int byteSize,
        MemValueType? memValueTypes)
    {
        return MemoryStringToBytes(memory, value, address, byteSize, memValueTypes, null, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address, int byteSize,
        MemValueType? memValueTypes, MemValueModifier? valueModifier)
    {
        return MemoryStringToBytes(memory, value, address, byteSize, memValueTypes, valueModifier, null);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address, int byteSize,
        MemValueType? memValueTypes, int[] typeModifiers)
    {
        return MemoryStringToBytes(memory, value, address, byteSize, memValueTypes, null, typeModifiers);
    }

    public static byte[] MemoryStringToBytes(HackMemory memory, string value, IntPtr address, int byteSize,
        MemValueType? memValueTypes, MemValueModifier? valueModifier,
        int[] typeModifiers)
    {
        var bytes = new byte[0];

        try
        {
            if (value.Length > 0 && value[0] == '.')
                value = '0' + value;

            var hex = valueModifier == MemValueModifier.Hexadecimal;

            var sign = valueModifier == MemValueModifier.Signed;

            var format = hex ? 16 : 10;

            switch (memValueTypes)
            {
                case MemValueType.Binary:
                    long mask = ~(((1 << typeModifiers[1]) - 1) << typeModifiers[0]);
                    var memValue =
                        ByteArrayToLong(memory.ReadMemoryBytes(address, byteSize));
                    var val = hex ? HexLiteralToLong(value) : long.Parse(value);
                    bytes = BitConverter.GetBytes((memValue & mask) | (val << typeModifiers[0]));
                    Array.Resize(ref bytes, byteSize);
                    break;
                case MemValueType.Byte:
                    bytes = new[] {Convert.ToByte(value.Split('.')[0], format)};
                    break;
                case MemValueType.TwoBytes:
                    bytes = BitConverter.GetBytes(Convert.ToInt16(value.Split('.')[0], format));
                    break;
                case MemValueType.FourBytes:
                    bytes = BitConverter.GetBytes(Convert.ToInt32(value.Split('.')[0], format));
                    break;
                case MemValueType.EightBytes:
                    bytes = BitConverter.GetBytes(hex
                        ? HexLiteralToLong(value)
                        : long.Parse(value.Split('.')[0]));
                    break;
                case MemValueType.Float:
                    bytes = hex
                        ? BitConverter.GetBytes(Convert.ToInt32(value, format))
                        : BitConverter.GetBytes(Convert.ToSingle(value));
                    break;
                case MemValueType.Double:
                    bytes = hex
                        ? BitConverter.GetBytes(HexLiteralToLong(value))
                        : BitConverter.GetBytes(Convert.ToDouble(value));
                    break;
                case MemValueType.String:
                    bytes = valueModifier == MemValueModifier.Unicode
                        ? Encoding.Unicode.GetBytes(value)
                        : Encoding.ASCII.GetBytes(value);
                    break;
                case MemValueType.ByteArray:
                    bytes = hex
                        ? HexStringToByteArray(value)
                        : value.Split(' ').Select(s => Convert.ToByte(s)).ToArray();
                    break;
            }
        }
        catch
        {
        }

        Array.Resize(ref bytes, byteSize);

        return bytes;
    }

    #endregion
}