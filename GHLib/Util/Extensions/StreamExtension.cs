using System.IO;

namespace GHLib.Util.Extensions;

public static class StreamExtention
{
    public static MemoryStream ToMemoryStream(this Stream s, bool leaveOpen = false)
    {
        var memstream = new MemoryStream();

        s.CopyTo(memstream);
        if (!leaveOpen)
        {
            s.Close();
            s.Dispose();
        }

        memstream.Position = 0;
        return memstream;
    }
}