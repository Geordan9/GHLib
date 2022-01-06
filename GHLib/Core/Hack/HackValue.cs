using GHLib.Common.Enums;
using static GHLib.Util.HackTools;

namespace GHLib.Core.Hack;

public class HackValue : Hack
{
    public string Value { get; private set; }

    public bool IsReadOnly { get; set; } = false;

    public MemValueType? MemType { get; set; }

    public int[] MemTypeModifiers { get; set; }

    public MemValueModifier? MemValMod { get; set; }

    public int ByteSize { get; set; }

    public bool IsValid(HackMemory memory)
    {
        return ValidateValue(memory, this);
    }

    public void Update(HackMemory memory, bool changed = false)
    {
        UpdateValue(memory, this, changed);
    }

    public void SetValue(HackMemory memory, string value)
    {
        try
        {
            if (MemType == MemValueType.String ||
                MemType != MemValueType.String && !string.IsNullOrEmpty(value))
            {
                Value = MemType != MemValueType.String ? value.Trim() : value;
                if (Enabled)
                    UpdateValue(memory, this, true);
            }
            else if (value == null)
            {
                Value = string.Empty;
            }
        }
        catch
        {
            Value = string.Empty;
        }
    }
}