namespace ModerBox.ViewModels {
    public class ShellNavigationItem : ViewModelBase {
        public ShellNavigationItem(FeatureGroup group, string title, string icon) {
            Group = group;
            Title = title;
            Icon = icon;
        }

        public FeatureGroup Group { get; }
    }
}
