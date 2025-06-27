using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModerBox.Views.UserControls {
    public partial class ChannelDifferenceAnalysis : UserControl {
        public ChannelDifferenceAnalysis() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 