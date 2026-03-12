using Avalonia.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls {
    public partial class VideoAnalysisSettings : UserControl {
        public VideoAnalysisSettings() {
            InitializeComponent();
            DataContext = new VideoAnalysisSettingsViewModel();
        }
    }
}
