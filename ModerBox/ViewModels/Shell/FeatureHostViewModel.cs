using ReactiveUI;
using System;
using System.Reactive;

namespace ModerBox.ViewModels {
    public class FeatureHostViewModel : ViewModelBase {
        public FeatureHostViewModel(string breadcrumb, ViewModelBase feature, Action back) {
            Breadcrumb = breadcrumb;
            Feature = feature;
            Back = ReactiveCommand.Create(back);
        }

        public string Breadcrumb { get; }
        public ViewModelBase Feature { get; }
        public ReactiveCommand<Unit, Unit> Back { get; }
    }
}
