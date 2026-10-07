using System.Collections.Generic;

namespace ModerBox.ViewModels {
    public static class FeatureCatalog {
        public static IReadOnlyList<FeatureEntry> Create() {
            return new FeatureEntry[] {
                Entry<FilterWaveformSwitchIntervalViewModel>(
                    "滤波器分合闸波形检测",
                    "扫描滤波器录波，计算分合闸时间并写入 SQLite",
                    "Filter",
                    FeatureGroup.FilterSwitch),
                Entry<FilterWaveformSwitchCopyViewModel>(
                    "分合闸波形筛选复制",
                    "按日期和通道筛选并复制分合闸相关录波",
                    "Copy",
                    FeatureGroup.FilterSwitch),
                Entry<SwitchOperationReportViewModel>(
                    "分合闸操作报表导出",
                    "读取分合闸检测得到的 SQLite，导出操作报表",
                    "Save",
                    FeatureGroup.FilterSwitch),
                Entry<HarmonicCalculateViewModel>(
                    "谐波计算",
                    "批量计算 COMTRADE 模拟量通道的谐波并导出",
                    "Audio",
                    FeatureGroup.Scattered),
                Entry<PeriodicWorkViewModel>(
                    "内置录波定期工作",
                    "按配置从内置录波提取直流场、换流变等模拟量并导出",
                    "Calendar",
                    FeatureGroup.Scattered),
                Entry<CurrentDifferenceAnalysisViewModel>(
                    "接地极电流差值分析",
                    "计算接地极电流差值并导出",
                    "Ruler",
                    FeatureGroup.Scattered),
                Entry<NewCurrentDifferenceAnalysisViewModel>(
                    "接地极电流差值分析 (新版)",
                    "接地极电流差值分析的新版入口，与旧版并存",
                    "RulerFilled",
                    FeatureGroup.Scattered),
                Entry<ThreePhaseIdeeAnalysisViewModel>(
                    "三相IDEE分析",
                    "按相汇总 PPR 录波中的三相 IDEE 并导出",
                    "ThreeBars",
                    FeatureGroup.Scattered),
                Entry<QuestionBankConversionViewModel>(
                    "题库转换",
                    "转换或合并题库文件",
                    "Document",
                    FeatureGroup.Scattered),
                Entry<ComtradeExportViewModel>(
                    "波形通道导出",
                    "从 COMTRADE 中选出通道并另存",
                    "Save",
                    FeatureGroup.Scattered),
                Entry<CableRoutingViewModel>(
                    "电缆走向绘制",
                    "按配置在底图上绘制电缆走向",
                    "Ruler",
                    FeatureGroup.Scattered),
                Entry<VideoAnalysisViewModel>(
                    "视频分析",
                    "分析视频并生成文案",
                    "Play",
                    FeatureGroup.Scattered),
                Entry<ContributionCalculationViewModel>(
                    "工作票贡献度计算",
                    "根据工作票 CSV 计算贡献度并导出 Excel",
                    "Calculator",
                    FeatureGroup.Scattered)
            };
        }

        private static FeatureEntry Entry<TViewModel>(
            string title,
            string description,
            string icon,
            FeatureGroup group) where TViewModel : ViewModelBase, new() {
            return new FeatureEntry(
                title,
                description,
                icon,
                group,
                typeof(TViewModel),
                () => new TViewModel { Title = title, Icon = icon });
        }
    }
}
