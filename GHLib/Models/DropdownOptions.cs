using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GHLib.Models
{
    public class DropdownOptions
    {
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();

        public bool DisallowManualInput { get; set; }
    }
}
