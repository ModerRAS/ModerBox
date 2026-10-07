using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void OnNavigationItemInvoked(object? sender, NavigationViewItemInvokedEventArgs e) {
            if (DataContext is not MainWindowViewModel viewModel) {
                return;
            }

            var item = e.InvokedItem as ShellNavigationItem
                ?? e.InvokedItemContainer?.DataContext as ShellNavigationItem;
            if (item is not null) {
                viewModel.ActivateNavigation(item);
            }
        }
    }
}