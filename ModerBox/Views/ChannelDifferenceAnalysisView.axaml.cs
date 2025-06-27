using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModerBox.Views {
    public partial class ChannelDifferenceAnalysisView : UserControl {
        public ChannelDifferenceAnalysisView() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 