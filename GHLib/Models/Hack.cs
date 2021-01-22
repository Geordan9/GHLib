using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GHLib.Models
{
    public class Hack : INotifyPropertyChanged
    {
        private bool enabled;
        private bool activated;
        private bool initialized;

        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                OnPropertyChanged();
                if (!enabled && ChildHacks != null)
                    foreach (var h in ChildHacks)
                        h.Enabled = enabled;
            }
        }

        public bool Activated
        {
            get => activated;
            set
            {
                activated = value;
                OnPropertyChanged();
            }
        }

        public bool Initialized
        {
            get => initialized;
            set
            {
                initialized = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; set; }

        public IntPtr Address { get; set; } = IntPtr.Zero;

        public bool RelativeAddress { get; set; } = true;

        public AoBScript[] AoBScripts { get; set; }

        public HackOffset[] Offsets { get; set; }

        public Hack Parent { get; internal set; }

        private Hack[] childHacks;

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

        public Hack Clone()
        {
            return (Hack)MemberwiseClone();
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}