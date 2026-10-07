using ReactiveUI;
using System;
using System.Collections.Generic;

namespace ModerBox.ViewModels {
    public class LauncherViewModel : ViewModelBase {
        private readonly Action _searchChanged;
        private string _searchText = "";
        private bool _isHome;
        private IReadOnlyList<FeatureSection> _sections = Array.Empty<FeatureSection>();

        public LauncherViewModel(HomePageViewModel home, Action searchChanged) {
            Home = home;
            _searchChanged = searchChanged;
        }

        public HomePageViewModel Home { get; }

        public IReadOnlyList<FeatureSection> Sections {
            get => _sections;
            private set => this.RaiseAndSetIfChanged(ref _sections, value);
        }

        public bool IsHome {
            get => _isHome;
            private set => this.RaiseAndSetIfChanged(ref _isHome, value);
        }

        public string SearchText {
            get => _searchText;
            set {
                if (_searchText == value) {
                    return;
                }

                _searchText = value ?? "";
                this.RaisePropertyChanged(nameof(SearchText));
                _searchChanged();
            }
        }

        public void Apply(IReadOnlyList<FeatureSection> sections, bool isHome) {
            Sections = sections;
            IsHome = isHome;
        }

        public void ReplaceSearchText(string value) {
            value ??= "";
            if (_searchText == value) {
                return;
            }

            _searchText = value;
            this.RaisePropertyChanged(nameof(SearchText));
        }
    }
}
