using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ModerBox.ViewModels;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ModerBox {
    public class ViewLocator : IDataTemplate {
        // 告诉修剪器保留视图的公有构造函数，防止 NativeAOT 剪裁导致 Type.GetType 失效。
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Views.UserControls.HomePage))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Views.UserControls.PeriodicWork))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Views.UserControls.HarmonicCalculate))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Views.UserControls.FilterWaveformSwitchInterval))]

        public Control? Build(object? data) {
            if (data is null)
                return null;

            var name = data.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(name);

            if (type != null) {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + name };
        }

        public bool Match(object? data) {
            return data is ViewModelBase;
        }
    }
}
