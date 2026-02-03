using Avalonia.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls {
    public partial class FilterWaveformSwitchCopy : UserControl {
        public FilterWaveformSwitchCopy() {
            InitializeComponent();
            DataContext = new FilterWaveformSwitchCopyViewModel();
        }
    }
}
