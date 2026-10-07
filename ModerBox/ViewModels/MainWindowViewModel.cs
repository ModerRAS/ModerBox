using ReactiveUI;
using System.Collections.Generic;

namespace ModerBox.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        private readonly ShellNavigation _navigation;

        public MainWindowViewModel() : this(FeatureCatalog.Create(), new HomePageViewModel()) {
        }

        public MainWindowViewModel(IReadOnlyList<FeatureEntry> features, HomePageViewModel home) {
            _navigation = new ShellNavigation(features, home);
            _navigation.Changed += () => {
                this.RaisePropertyChanged(nameof(SelectedNavigation));
                this.RaisePropertyChanged(nameof(CurrentContent));
            };
        }

        public IReadOnlyList<ShellNavigationItem> NavigationItems => _navigation.Items;

        public ShellNavigationItem SelectedNavigation {
            get => _navigation.SelectedItem;
            set {
                if (value is null || ReferenceEquals(value, _navigation.SelectedItem)) {
                    return;
                }

                _navigation.Select(value);
            }
        }

        public object CurrentContent => _navigation.CurrentContent;

        public void ActivateNavigation(ShellNavigationItem item) {
            _navigation.Select(item);
        }
    }
}
