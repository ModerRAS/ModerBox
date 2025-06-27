using Avalonia.Controls;
using ModerBox.ViewModels;

namespace ModerBox.Views.UserControls {
    /// <summary>
    /// 接地极电流平衡分析用户控件
    /// </summary>
    public partial class GroundCurrentBalance : UserControl {
        public GroundCurrentBalance() {
            InitializeComponent();
            DataContext = new GroundCurrentBalanceViewModel();
        }
    }
} 