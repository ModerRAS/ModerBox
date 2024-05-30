using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls;

public partial class HomePage : UserControl
{
    public HomePage()
    {
        InitializeComponent();
        DataContext = new HomePageViewModel();
    }
}