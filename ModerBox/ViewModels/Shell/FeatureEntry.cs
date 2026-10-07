using ReactiveUI;
using System;
using System.Reactive;

namespace ModerBox.ViewModels {
    public class FeatureEntry {
        private readonly Func<ViewModelBase> _factory;
        private ViewModelBase? _instance;

        public FeatureEntry(
            string title,
            string description,
            string icon,
            FeatureGroup group,
            Type viewModelType,
            Func<ViewModelBase> factory) {
            Title = title;
            Description = description;
            Icon = icon;
            Group = group;
            ViewModelType = viewModelType;
            _factory = factory;
            OpenCommand = ReactiveCommand.Create(() => { });
        }

        public string Title { get; }
        public string Description { get; }
        public string Icon { get; }
        public FeatureGroup Group { get; }
        public Type ViewModelType { get; }
        public ReactiveCommand<Unit, Unit> OpenCommand { get; private set; }

        public void Attach(Action<FeatureEntry> open) {
            OpenCommand = ReactiveCommand.Create(() => open(this));
        }

        public ViewModelBase GetViewModel() {
            return _instance ??= _factory();
        }
    }
}
