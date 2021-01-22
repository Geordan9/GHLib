using GHLib.Common.Enums;
using static GHLib.Utils.HackTools;

namespace GHLib.Models
{
    public class HackInput : Hack
    {
        private bool editing;
        private string value;

        public string Value
        {
            get => value;
            set
            {
                if (MemType == MemValueType.String || (MemType != MemValueType.String && value != string.Empty))
                {
                    this.value = value;
                    OnPropertyChanged();
                    if (Enabled)
                        UpdateValue(this, true);
                }
                Editing = false;
            }
        }

        public bool Editing
        {
            get => editing;
            set
            {
                editing = value;
                OnPropertyChanged();
            }
        }

        public bool IsReadOnly { get; set; } = false;

        public MemValueType? MemType { get; set; }

        public int[] MemTypeModifiers { get; set; }

        public MemValueModifier? MemValMod { get; set; }

        public int ByteSize { get; set; }

        public DropdownOptions Dropdown { get; set; }

        public new HackInput Clone()
        {
            return (HackInput)MemberwiseClone();
        }
    }
}