using ReactiveUI;

namespace ModerBox.ViewModels {
    public class ViewModelBase : ReactiveObject {
        private string? _title;

        public string? Title {
            get => _title;
            set => this.RaiseAndSetIfChanged(ref _title, value);
        }

        private string? _icon;
        public string? Icon {
            get => _icon;
            set => this.RaiseAndSetIfChanged(ref _icon, value);
        }
    }
}
