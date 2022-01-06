using System;
using GHLib.Core.AoB;

namespace GHLib.Core.Hack;

public class Hack
{
    private Hack[] childHacks;
    private bool enabled;

    public bool Enabled
    {
        get => enabled;
        set
        {
            enabled = value;
            if (!enabled && ChildHacks != null)
                foreach (var h in ChildHacks)
                    h.Enabled = enabled;
        }
    }

    public bool Activated { get; set; }

    public bool Initialized { get; set; }

    public string ID { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public IntPtr Address { get; set; } = IntPtr.Zero;

    public bool RelativeAddress { get; set; } = true;

    public AoBScript[] AoBScripts { get; set; }

    public HackOffset[] Offsets { get; set; }

    public HackOptions Options { get; set; }

    public Hack Parent { get; internal set; }

    public Hack[] ChildHacks
    {
        get => childHacks;
        set
        {
            childHacks = value;
            foreach (var ch in childHacks)
                if (ch != null)
                    ch.Parent = this;
        }
    }
}