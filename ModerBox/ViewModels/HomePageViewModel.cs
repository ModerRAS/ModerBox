using Avalonia.Controls;
using ModerBox.Common;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.ViewModels {
    public class HomePageViewModel : ViewModelBase {
        public ReactiveCommand<Unit, Unit> CheckUpdate { get; }
        public ReactiveCommand<Unit, Unit> CheckUpdateBackRoute { get; }
        private string _log;
        public string Log {
            get => _log;
            set => this.RaiseAndSetIfChanged(ref _log, value);
        }
        public async Task CheckUpdateTask() {
            try {
                await Util.UpdateMyApp((s) => {
                    Log = s;
                });
            } catch (Exception ex) {
                Log = "检查失败";
            }
        }

        public async Task CheckUpdateBackRouteTask() {
            try {
                await Util.UpdateMyAppBackRoute((s) => {
                    Log = s;
                });
            } catch (Exception ex) {
                Log = "检查失败";
            }
            
        }
        public HomePageViewModel() {
            CheckUpdate = ReactiveCommand.CreateFromTask(CheckUpdateTask);
            CheckUpdateBackRoute = ReactiveCommand.CreateFromTask(CheckUpdateBackRouteTask);
            _log = "点击下方按钮可以检查并更新";
        }
    }
}
