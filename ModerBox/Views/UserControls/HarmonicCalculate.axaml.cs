using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls;

public partial class HarmonicCalculate : UserControl
{
    public HarmonicCalculate()
    {
        InitializeComponent();
        DataContext = new HarmonicCalculateViewModel();
    }
}