using System;
using System.Collections.Generic;
using System.Linq;

namespace ModerBox.ViewModels {
    public class ShellNavigation {
        private readonly IReadOnlyList<FeatureEntry> _features;
        private readonly LauncherViewModel _launcher;
        private ShellNavigationItem _selected;
        private object _current;
        private FeatureEntry? _active;
        private bool _suppressSidebar;

        public ShellNavigation(IReadOnlyList<FeatureEntry> features, HomePageViewModel home) {
            _features = features;
            foreach (var feature in features) {
                feature.Attach(Open);
            }

            Items = new[] {
                new ShellNavigationItem(FeatureGroup.Home, "首页", "Home"),
                new ShellNavigationItem(FeatureGroup.FilterSwitch, "滤波器分合闸波形分析", "Filter"),
                new ShellNavigationItem(FeatureGroup.Scattered, "零散功能", "Library")
            };
            _selected = Items[0];
            _launcher = new LauncherViewModel(home, RefreshHomeSearch);
            _current = _launcher;
            ShowLauncher();
        }

        public event Action? Changed;

        public IReadOnlyList<ShellNavigationItem> Items { get; }

        public ShellNavigationItem SelectedItem => _selected;

        public object CurrentContent => _current;

        public void Select(ShellNavigationItem item) {
            if (_suppressSidebar) {
                return;
            }

            ApplySection(item);
        }

        public void Open(FeatureEntry entry) {
            var section = ItemFor(entry.Group);
            _suppressSidebar = true;
            try {
                _active = entry;
                if (!ReferenceEquals(_selected, section)) {
                    _selected = section;
                }

                ShowFeature(entry);
            } finally {
                _suppressSidebar = false;
            }
        }

        public void Back() {
            if (_active is null) {
                return;
            }

            _active = null;
            ShowLauncher();
        }

        private void RefreshHomeSearch() {
            if (_active is not null || _selected.Group != FeatureGroup.Home) {
                return;
            }

            ShowLauncher();
        }

        private void ApplySection(ShellNavigationItem item) {
            _active = null;
            if (item.Group == FeatureGroup.Home) {
                _launcher.ReplaceSearchText("");
            }

            var selectionChanged = !ReferenceEquals(_selected, item);
            if (selectionChanged) {
                _selected = item;
            }

            ShowLauncher(selectionChanged);
        }

        private void ShowLauncher(bool selectionChanged = false) {
            _launcher.Apply(BuildSections(), _selected.Group == FeatureGroup.Home);
            var contentChanged = !ReferenceEquals(_current, _launcher);
            if (contentChanged) {
                _current = _launcher;
            }

            if (selectionChanged || contentChanged) {
                Changed?.Invoke();
            }
        }

        private void ShowFeature(FeatureEntry entry) {
            var section = ItemFor(entry.Group);
            _current = new FeatureHostViewModel(
                $"{section.Title} / {entry.Title}",
                entry.GetViewModel(),
                Back);
            Changed?.Invoke();
        }

        private IReadOnlyList<FeatureSection> BuildSections() {
            if (_selected.Group == FeatureGroup.Home) {
                return new[] {
                    CreateSection(FeatureGroup.FilterSwitch, applySearch: true),
                    CreateSection(FeatureGroup.Scattered, applySearch: true)
                }.Where(section => section.Cards.Count > 0).ToArray();
            }

            return new[] { CreateSection(_selected.Group, applySearch: false) };
        }

        private FeatureSection CreateSection(FeatureGroup group, bool applySearch) {
            var cards = _features
                .Where(feature => feature.Group == group && (!applySearch || Matches(feature)))
                .ToArray();
            return new FeatureSection(ItemFor(group).Title ?? string.Empty, cards);
        }

        private bool Matches(FeatureEntry feature) {
            var query = _launcher.SearchText.Trim();
            if (query.Length == 0) {
                return true;
            }

            return feature.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
                || feature.Description.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private ShellNavigationItem ItemFor(FeatureGroup group) {
            return Items.First(item => item.Group == group);
        }
    }
}
