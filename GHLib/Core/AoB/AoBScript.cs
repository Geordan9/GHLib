using System;

namespace GHLib.Core.AoB;

public class AoBScript
{
    public IntPtr Address { get; set; }

    public int Offset { get; set; }

    public string Module { get; set; } = string.Empty;

    public int ModuleIndex { get; set; }

    public string AoBString { get; set; }

    public byte[] AoB { get; set; }

    public AoBReplacement[] AoBReplacements { get; set; }

    public AoBScript[] AoBScripts { get; set; }

    public AoBPointer AoBPointer { get; set; }

    public bool IsRelative { get; set; } = true;
}