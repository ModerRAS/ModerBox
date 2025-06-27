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

        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(Views.UserControls.CurrentDifferenceAnalysis))]

        public Control? Build(object? data) {
            if (data is null)
                return null;

            var fullName = data.GetType().FullName!;
            
            // 先尝试标准的ViewModel -> View映射
            var viewName = fullName.Replace("ViewModel", "View", StringComparison.Ordinal);
            var type = Type.GetType(viewName);
            
            // 如果找不到，尝试UserControls命名空间中的控件（去掉ViewModel后缀）
            if (type == null) {
                var userControlName = fullName.Replace("ModerBox.ViewModels.", "ModerBox.Views.UserControls.")
                                            .Replace("ViewModel", "", StringComparison.Ordinal);
                type = Type.GetType(userControlName);
            }

            if (type != null) {
                var control = (Control)Activator.CreateInstance(type)!;
                control.DataContext = data;
                return control;
            }

            return new TextBlock { Text = "Not Found: " + viewName };
        }

        public bool Match(object? data) {
            return data is ViewModelBase;
        }
    }
}
