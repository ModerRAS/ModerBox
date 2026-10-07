using FluentAvalonia.UI.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Test {
    [TestClass]
    public class FeatureCatalogTest {
        [TestMethod]
        public void Create_ContainsExpectedFeaturesInDisplayOrder() {
            var features = FeatureCatalog.Create();

            CollectionAssert.AreEqual(
                new[] {
                    "滤波器分合闸波形检测",
                    "分合闸波形筛选复制",
                    "分合闸操作报表导出",
                    "谐波计算",
                    "内置录波定期工作",
                    "接地极电流差值分析",
                    "接地极电流差值分析 (新版)",
                    "三相IDEE分析",
                    "题库转换",
                    "波形通道导出",
                    "电缆走向绘制",
                    "视频分析",
                    "工作票贡献度计算"
                },
                features.Select(feature => feature.Title).ToArray());
            CollectionAssert.AreEqual(
                Enumerable.Repeat(FeatureGroup.FilterSwitch, 3)
                    .Concat(Enumerable.Repeat(FeatureGroup.Scattered, 10))
                    .ToArray(),
                features.Select(feature => feature.Group).ToArray());
            var invalidIcons = features
                .Where(feature => !Enum.TryParse<Symbol>(feature.Icon, out _))
                .Select(feature => feature.Title + ":" + feature.Icon);
            Assert.AreEqual("", string.Join(", ", invalidIcons));
        }

        [TestMethod]
        public void ViewLocator_HasMapping_ForCatalogAndShellViewModels() {
            foreach (var feature in FeatureCatalog.Create()) {
                Assert.IsTrue(
                    ViewLocator.HasMapping(feature.ViewModelType),
                    feature.ViewModelType.Name);
            }

            Assert.IsTrue(ViewLocator.HasMapping(typeof(LauncherViewModel)));
            Assert.IsTrue(ViewLocator.HasMapping(typeof(FeatureHostViewModel)));
            Assert.IsTrue(ViewLocator.HasMapping(typeof(HomePageViewModel)));
            Assert.IsTrue(ViewLocator.HasMapping(typeof(ContributionCalculationViewModel)));
        }

        [TestMethod]
        public void MainWindow_StartsOnHome_WithThreeNavigationItems() {
            var viewModel = new MainWindowViewModel();

            CollectionAssert.AreEqual(
                new[] { "首页", "滤波器分合闸波形分析", "零散功能" },
                viewModel.NavigationItems.Select(item => item.Title).ToArray());
            Assert.AreEqual("首页", viewModel.SelectedNavigation.Title);
            Assert.IsTrue(Enum.TryParse<Symbol>(viewModel.NavigationItems[2].Icon, out _));
            var launcher = (LauncherViewModel)viewModel.CurrentContent;
            Assert.IsTrue(launcher.IsHome);
            Assert.AreEqual(13, launcher.Sections.Sum(section => section.Cards.Count));
        }
    }
}
