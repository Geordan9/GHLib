using System;
using System.IO;
using GHLib.Util.Extensions;
using MemoryLib.Core;
using ProcessLib.Util.Extensions;

namespace GHLib.Core.Hack;

public class HackMemory
{
    public HackMemory(Memory memory)
    {
        Memory = memory;
    }

    public HackMemory(Stream stream, long imageBase, int codeOff, bool createCopy = true)
    {
        Memory = createCopy ? stream.ToMemoryStream(true) : stream;
        CopyStream = createCopy;
        IsStream = true;
        ImageBase = imageBase;
        CodeOffset = codeOff;
    }

    private bool CopyStream { get; }
    private bool IsStream { get; }
    private object Memory { get; }

    public Stream Stream => Memory as Stream;

    public long ImageBase { get; }

    public int CodeOffset { get; }

    ~HackMemory()
    {
        if (IsStream && CopyStream)
        {
            var stream = Memory as Stream;
            stream.Close();
            stream.Dispose();
        }
    }

    public byte[] ReadMemoryBytes(IntPtr addr, int length)
    {
        try
        {
            byte[] bytesRead;
            if (IsStream)
            {
                var stream = Memory as Stream;
                var origPos = stream.Position;
                stream.Position += (long) addr;
                bytesRead = new byte[length];
                stream.Read(bytesRead, 0, length);
                stream.Position = origPos;
            }
            else
            {
                var memory = Memory as Memory;
                memory.Open();
                bytesRead = memory.Read(addr, (uint) length, out var b);
                memory.CloseHandle();
            }

            return bytesRead;
        }
        catch
        {
            return null;
        }
    }

    public void WriteMemoryBytes(IntPtr addr, byte[] bytes, int offset = 0)
    {
        try
        {
            if (IsStream)
            {
                var stream = Memory as Stream;
                var origPos = stream.Position;
                stream.Position += (long) (addr + offset);
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = origPos;
            }
            else
            {
                var memory = Memory as Memory;
                memory.Open();
                memory.Write(addr + offset, bytes, out _);
                memory.CloseHandle();
            }
        }
        catch
        {
        }
    }

    public bool Is64Bit(bool defaultVal = false)
    {
        return IsStream ? defaultVal : !(Memory as Memory).ReadProcess.IsWin64Emulator();
    }
}