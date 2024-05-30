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
        private string _log;
        public string Log {
            get => _log;
            set => this.RaiseAndSetIfChanged(ref _log, value);
        }
        public async Task CheckUpdateTask() {
            await Util.UpdateMyApp((s) => {
                Log = s;
            });
        }

        public HomePageViewModel() {
            CheckUpdate = ReactiveCommand.CreateFromTask(CheckUpdateTask);
            _log = "点击下方按钮可以检查并更新";
        }
    }
}
