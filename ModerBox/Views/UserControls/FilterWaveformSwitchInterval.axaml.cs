using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls;

public partial class FilterWaveformSwitchInterval : UserControl
{
    public FilterWaveformSwitchInterval()
    {
        InitializeComponent();
        DataContext = new FilterWaveformSwitchIntervalViewModel();
    }
}