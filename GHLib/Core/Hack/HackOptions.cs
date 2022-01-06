using System.Collections.Generic;

namespace GHLib.Core.Hack;

public class HackOptions
{
    public Dictionary<string, string> Options { get; set; } = new();

    public bool DisallowManualInput { get; set; }
}