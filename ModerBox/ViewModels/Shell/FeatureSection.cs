using System.Collections.Generic;

namespace ModerBox.ViewModels {
    public class FeatureSection {
        public FeatureSection(string title, IReadOnlyList<FeatureEntry> cards) {
            Title = title;
            Cards = cards;
        }

        public string Title { get; }
        public IReadOnlyList<FeatureEntry> Cards { get; }
    }
}
