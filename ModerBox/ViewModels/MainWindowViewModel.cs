using System.Collections.Generic;
using System.Linq;
using ReactiveUI;

namespace ModerBox.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        public IEnumerable<ViewModelBase> Pages { get; }

        private ViewModelBase _currentPage;
        public ViewModelBase CurrentPage {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public MainWindowViewModel() {
            Pages = new List<ViewModelBase> {
                new HomePageViewModel { Title = "首页", Icon = "Home" },
                new HarmonicCalculateViewModel { Title = "谐波计算", Icon = "ShowResults" },
                new FilterWaveformSwitchIntervalViewModel { Title = "滤波器分合闸波形检测", Icon = "Filter" },
                new PeriodicWorkViewModel { Title = "内置录波定期工作", Icon = "Calendar" },
                new CurrentDifferenceAnalysisViewModel { Title = "接地极电流差值分析", Icon = "Calculator" },
                new ThreePhaseIdeeAnalysisViewModel { Title = "三相IDEE分析", Icon = "ThreeColumns" }
            };
            _currentPage = Pages.First();
        }
    }
}
