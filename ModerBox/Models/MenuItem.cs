using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ModerBox.Models {
    public class MenuItem {
        public string Icon { get; set; }
        public string Title { get; set; }
        public ICommand Command { get; set; }
    }
}
