using Avalonia.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls {
    public partial class VideoAnalysis : UserControl {
        public VideoAnalysis() {
            InitializeComponent();
            DataContext = new VideoAnalysisViewModel();
        }
    }
}
