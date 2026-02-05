using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ModerBox.ViewModels;
using ModerBox.Views.UserControls;
using System;
using System.Collections.Generic;

namespace ModerBox {
    public class ViewLocator : IDataTemplate {
        // AOT 友好的静态视图映射
        private static readonly Dictionary<Type, Func<Control>> ViewMappings = new() {
            { typeof(HomePageViewModel), () => new HomePage() },
            { typeof(PeriodicWorkViewModel), () => new PeriodicWork() },
            { typeof(HarmonicCalculateViewModel), () => new HarmonicCalculate() },
            { typeof(FilterWaveformSwitchIntervalViewModel), () => new FilterWaveformSwitchInterval() },
            { typeof(FilterWaveformSwitchCopyViewModel), () => new FilterWaveformSwitchCopy() },
            { typeof(CurrentDifferenceAnalysisViewModel), () => new CurrentDifferenceAnalysis() },
            { typeof(ThreePhaseIdeeAnalysisViewModel), () => new ThreePhaseIdeeAnalysis() },
            { typeof(NewCurrentDifferenceAnalysisViewModel), () => new NewCurrentDifferenceAnalysis() },
            { typeof(QuestionBankConversionViewModel), () => new QuestionBankConversion() },
            { typeof(ComtradeExportViewModel), () => new ComtradeExport() },
            { typeof(CableRoutingViewModel), () => new Views.UserControls.CableRouting() }
        };

        public Control? Build(object? data) {
            if (data is null)
                return null;

            var dataType = data.GetType();
            
            // 使用静态映射查找对应的视图
            if (ViewMappings.TryGetValue(dataType, out var factory)) {
                var control = factory();
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + dataType.Name };
        }

        public bool Match(object? data) {
            return data is ViewModelBase;
        }
    }
}
