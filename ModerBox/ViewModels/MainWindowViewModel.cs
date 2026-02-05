using ReactiveUI;
using System.Collections.ObjectModel;
using System.Linq;

namespace ModerBox.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        public ObservableCollection<ViewModelBase> Pages { get; }

        private ViewModelBase _currentPage;
        public ViewModelBase CurrentPage {
            get => _currentPage;
            set => this.RaiseAndSetIfChanged(ref _currentPage, value);
        }

        public MainWindowViewModel() {
            Pages = new ObservableCollection<ViewModelBase> {
                new HomePageViewModel { Title = "首页", Icon = "Home" },
                new HarmonicCalculateViewModel { Title = "谐波计算", Icon = "Audio" },
                new FilterWaveformSwitchIntervalViewModel { Title = "滤波器分合闸波形检测", Icon = "Filter" },
                new FilterWaveformSwitchCopyViewModel { Title = "分合闸波形筛选复制", Icon = "Copy" },
                new PeriodicWorkViewModel { Title = "内置录波定期工作", Icon = "Calendar" },
                new CurrentDifferenceAnalysisViewModel { Title = "接地极电流差值分析", Icon = "Ruler" },
                new NewCurrentDifferenceAnalysisViewModel { Title = "接地极电流差值分析 (新版)", Icon = "RulerFilled" },
                new ThreePhaseIdeeAnalysisViewModel { Title = "三相IDEE分析", Icon = "ThreeBars" },
                new QuestionBankConversionViewModel { Title = "题库转换", Icon = "Document" },
                new ComtradeExportViewModel { Title = "波形通道导出", Icon = "Save" },
                new CableRoutingViewModel { Title = "电缆走向绘制", Icon = "RulerFilled" }
            };
            _currentPage = Pages.First();
        }
    }
}
