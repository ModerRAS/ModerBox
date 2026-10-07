using System.Reactive.Concurrency;
using FluentAvalonia.UI.Controls;
using ModerBox.ViewModels;
using ReactiveUI;

namespace ModerBox.Test {
    [TestClass]
    public class ShellNavigationTest {
        [TestInitialize]
        public void Initialize() {
            RxApp.MainThreadScheduler = ImmediateScheduler.Instance;
            RxApp.TaskpoolScheduler = ImmediateScheduler.Instance;
        }

        [TestMethod]
        public void Start_Home_ShowsFilterThenScattered() {
            var navigation = CreateNavigation();

            Assert.AreEqual(FeatureGroup.Home, navigation.SelectedItem.Group);
            var launcher = LauncherOf(navigation);
            Assert.IsTrue(launcher.IsHome);
            Assert.AreEqual(2, launcher.Sections.Count);
            CollectionAssert.AreEqual(
                new[] { "滤波器分合闸波形检测", "分合闸波形筛选复制" },
                launcher.Sections[0].Cards.Select(card => card.Title).ToArray());
            CollectionAssert.AreEqual(
                new[] { "谐波计算", "题库转换" },
                launcher.Sections[1].Cards.Select(card => card.Title).ToArray());
        }

        [TestMethod]
        public void SelectGroup_ShowsOnlyThatGroup() {
            var navigation = CreateNavigation();

            navigation.Select(Item(navigation, FeatureGroup.Scattered));

            var launcher = LauncherOf(navigation);
            Assert.IsFalse(launcher.IsHome);
            Assert.AreEqual(1, launcher.Sections.Count);
            Assert.AreEqual("零散功能", launcher.Sections[0].Title);
            CollectionAssert.AreEqual(
                new[] { "谐波计算", "题库转换" },
                launcher.Sections[0].Cards.Select(card => card.Title).ToArray());
        }

        [TestMethod]
        public void Search_Harmonic_KeepsHarmonicThenRestore() {
            var navigation = CreateNavigation();
            var launcher = LauncherOf(navigation);

            launcher.SearchText = "谐波";

            Assert.AreEqual(1, launcher.Sections.Count);
            Assert.AreEqual("零散功能", launcher.Sections[0].Title);
            CollectionAssert.AreEqual(
                new[] { "谐波计算" },
                launcher.Sections[0].Cards.Select(card => card.Title).ToArray());

            launcher.SearchText = "";

            Assert.AreEqual(2, launcher.Sections.Count);
            Assert.AreEqual(4, launcher.Sections.Sum(section => section.Cards.Count));
        }

        [TestMethod]
        public void SelectHome_ClearsSearch() {
            var navigation = CreateNavigation();
            var launcher = LauncherOf(navigation);
            launcher.SearchText = "谐波";

            navigation.Select(Item(navigation, FeatureGroup.Scattered));

            Assert.AreEqual("谐波", launcher.SearchText);
            Assert.AreEqual(2, LauncherOf(navigation).Sections[0].Cards.Count);

            navigation.Select(Item(navigation, FeatureGroup.Home));

            Assert.AreEqual("", launcher.SearchText);
            Assert.AreEqual(4, LauncherOf(navigation).Sections.Sum(section => section.Cards.Count));
        }

        [TestMethod]
        public void OpenFeature_SelectsGroup_BackReturnsToGroup() {
            var navigation = CreateNavigation();
            var entry = FeatureOf(navigation, "分合闸波形筛选复制");

            navigation.Open(entry);

            Assert.AreEqual(FeatureGroup.FilterSwitch, navigation.SelectedItem.Group);
            var host = (FeatureHostViewModel)navigation.CurrentContent;
            Assert.AreEqual("滤波器分合闸波形分析 / 分合闸波形筛选复制", host.Breadcrumb);

            navigation.Back();

            var launcher = LauncherOf(navigation);
            Assert.IsFalse(launcher.IsHome);
            Assert.AreEqual(FeatureGroup.FilterSwitch, navigation.SelectedItem.Group);
            CollectionAssert.AreEqual(
                new[] { "滤波器分合闸波形检测", "分合闸波形筛选复制" },
                launcher.Sections[0].Cards.Select(card => card.Title).ToArray());
        }

        [TestMethod]
        public void OpenFeature_Twice_ReturnsSameViewModel() {
            var calls = 0;
            var entry = new FeatureEntry(
                "谐波计算",
                "批量计算谐波",
                "Audio",
                FeatureGroup.Scattered,
                typeof(FakeViewModel),
                () => {
                    calls++;
                    return new FakeViewModel();
                });
            var navigation = new ShellNavigation(new[] { entry }, new HomePageViewModel());

            navigation.Open(entry);
            var first = ((FeatureHostViewModel)navigation.CurrentContent).Feature;
            navigation.Back();
            navigation.Open(entry);
            var second = ((FeatureHostViewModel)navigation.CurrentContent).Feature;

            Assert.AreSame(first, second);
            Assert.AreEqual(1, calls);
        }

        private static ShellNavigation CreateNavigation() {
            var features = new[] {
                Entry("滤波器分合闸波形检测", "扫描滤波器录波", FeatureGroup.FilterSwitch),
                Entry("分合闸波形筛选复制", "按日期和通道筛选", FeatureGroup.FilterSwitch),
                Entry("谐波计算", "批量计算谐波", FeatureGroup.Scattered),
                Entry("题库转换", "转换或合并题库文件", FeatureGroup.Scattered)
            };
            return new ShellNavigation(features, new HomePageViewModel());
        }

        private static FeatureEntry Entry(string title, string description, FeatureGroup group) {
            return new FeatureEntry(title, description, "Home", group, typeof(FakeViewModel), () => new FakeViewModel());
        }

        private static ShellNavigationItem Item(ShellNavigation navigation, FeatureGroup group) {
            return navigation.Items.First(item => item.Group == group);
        }

        private static LauncherViewModel LauncherOf(ShellNavigation navigation) {
            return (LauncherViewModel)navigation.CurrentContent;
        }

        private static FeatureEntry FeatureOf(ShellNavigation navigation, string title) {
            return LauncherOf(navigation).Sections.SelectMany(section => section.Cards).First(card => card.Title == title);
        }

        private sealed class FakeViewModel : ViewModelBase {
        }
    }
}
