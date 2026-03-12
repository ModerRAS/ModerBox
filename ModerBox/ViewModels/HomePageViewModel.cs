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
        /// <summary>
        /// Attempts to perform an application update and writes progress messages to the view model's Log.
        /// </summary>
        /// <remarks>
        /// Updates to Log are made as progress messages are received. If the update fails, Log is set to "检查失败".
        /// </remarks>
        public async Task CheckUpdateTask() {
            try {
                await Util.UpdateMyApp((s) => {
                    Log = s;
                });
            } catch {
                Log = "检查失败";
            }
        }

        /// <summary>
        /// Checks for available updates and performs the back-route update, writing progress messages to the Log property.
        /// </summary>
        /// <remarks>
        /// If the update check or back-route update fails, the Log property is set to "检查失败".
        /// </remarks>
        public async Task CheckUpdateBackRouteTask() {
            try {
                await Util.UpdateMyAppBackRoute((s) => {
                    Log = s;
                });
            } catch {
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
