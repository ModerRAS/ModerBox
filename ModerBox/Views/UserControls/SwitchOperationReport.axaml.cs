using Avalonia.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls {
    public partial class SwitchOperationReport : UserControl {
        public SwitchOperationReport() {
            InitializeComponent();
            DataContext = new SwitchOperationReportViewModel();
        }
    }
}
