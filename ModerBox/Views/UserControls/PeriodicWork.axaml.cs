using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls;

public partial class PeriodicWork : UserControl
{
    public PeriodicWork()
    {
        InitializeComponent();
        DataContext = new PeriodicWorkViewModel();
    }
}