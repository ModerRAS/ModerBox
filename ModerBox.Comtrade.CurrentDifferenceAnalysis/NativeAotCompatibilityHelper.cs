using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ScottPlot;

namespace ModerBox.Comtrade.CurrentDifferenceAnalysis
{
    /// <summary>
    /// Native AOT兼容性辅助类
    /// </summary>
    public static class NativeAotCompatibilityHelper
    {
        /// <summary>
        /// 检测是否运行在Native AOT环境下
        /// </summary>
        public static bool IsNativeAot => RuntimeFeature.IsDynamicCodeSupported == false;

        /// <summary>
        /// 安全创建ScottPlot图表
        /// </summary>
        /// <returns>配置好的Plot对象</returns>
        public static Plot CreateSafePlot()
        {
            try
            {
                var plot = new Plot();
                ConfigurePlotForNativeAot(plot);
                return plot;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create plot: {ex.Message}");
                // 创建最基本的plot
                return new Plot();
            }
        }

        /// <summary>
        /// 为Native AOT配置Plot
        /// </summary>
        /// <param name="plot">要配置的Plot对象</param>
        public static void ConfigurePlotForNativeAot(Plot plot)
        {
            try
            {
                // 设置基本背景色
                plot.FigureBackground.Color = ScottPlot.Colors.White;
                plot.DataBackground.Color = ScottPlot.Colors.White;

                if (IsNativeAot)
                {
                    // 在Native AOT下使用最安全的设置
                    
                    // 禁用反锯齿以避免可能的兼容性问题
                    plot.Axes.AntiAlias(false);
                    
                    // 设置简单的边距
                    plot.Axes.Margins(0.05, 0.05);
                    
                    // 避免使用复杂的文本设置
                    // 使用空字符串而不是null
                    SafeSetTitle(plot, "");
                    SafeSetXLabel(plot, "");
                    SafeSetYLabel(plot, "");
                }
                else
                {
                    // 在常规.NET运行时下可以使用更多功能
                    plot.Axes.AntiAlias(true);
                    plot.Axes.Margins(0.1, 0.1);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Plot configuration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全设置标题
        /// </summary>
        /// <param name="plot">Plot对象</param>
        /// <param name="title">标题文本</param>
        public static void SafeSetTitle(Plot plot, string title)
        {
            try
            {
                if (IsNativeAot)
                {
                    // 在Native AOT下避免复杂的字体操作
                    if (!string.IsNullOrEmpty(title))
                    {
                        // 只设置简单的ASCII字符标题
                        var asciiTitle = ConvertToAscii(title);
                        plot.Title(asciiTitle);
                    }
                }
                else
                {
                    plot.Title(title ?? "");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set title: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全设置X轴标签
        /// </summary>
        /// <param name="plot">Plot对象</param>
        /// <param name="label">标签文本</param>
        public static void SafeSetXLabel(Plot plot, string label)
        {
            try
            {
                if (IsNativeAot)
                {
                    var asciiLabel = ConvertToAscii(label);
                    plot.XLabel(asciiLabel);
                }
                else
                {
                    plot.XLabel(label ?? "");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set X label: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全设置Y轴标签
        /// </summary>
        /// <param name="plot">Plot对象</param>
        /// <param name="label">标签文本</param>
        public static void SafeSetYLabel(Plot plot, string label)
        {
            try
            {
                if (IsNativeAot)
                {
                    var asciiLabel = ConvertToAscii(label);
                    plot.YLabel(asciiLabel);
                }
                else
                {
                    plot.YLabel(label ?? "");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set Y label: {ex.Message}");
            }
        }

        /// <summary>
        /// 安全保存图片
        /// </summary>
        /// <param name="plot">Plot对象</param>
        /// <param name="filePath">文件路径</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        /// <returns>是否成功保存</returns>
        public static bool SafeSavePng(Plot plot, string filePath, int width = 800, int height = 400)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                plot.SavePng(filePath, width, height);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save PNG: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 将文本转换为ASCII字符（移除或替换非ASCII字符）
        /// </summary>
        /// <param name="text">输入文本</param>
        /// <returns>ASCII文本</returns>
        private static string ConvertToAscii(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            var result = text;
            
            // 简单的中文到英文映射
            result = result.Replace("电流", "Current");
            result = result.Replace("差值", "Difference");
            result = result.Replace("分析", "Analysis");
            result = result.Replace("时间", "Time");
            result = result.Replace("数值", "Value");
            result = result.Replace("百分比", "Percentage");
            result = result.Replace("波形", "Waveform");
            
            // 移除所有非ASCII字符
            var asciiChars = result.Where(c => c <= 127).ToArray();
            return new string(asciiChars);
        }

        /// <summary>
        /// 检查ScottPlot是否能正常工作
        /// </summary>
        /// <returns>错误信息，如果没有错误则返回null</returns>
        public static string? TestScottPlotCompatibility()
        {
            try
            {
                var plot = CreateSafePlot();
                SafeSetTitle(plot, "Test");
                
                // 添加一个简单的数据点测试
                var x = new double[] { 1, 2, 3 };
                var y = new double[] { 1, 2, 1 };
                var scatter = plot.Add.Scatter(x, y);
                scatter.Color = ScottPlot.Colors.Blue;
                
                // 测试保存到内存
                var tempPath = Path.GetTempFileName() + ".png";
                var success = SafeSavePng(plot, tempPath, 100, 100);
                
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                
                return success ? null : "Failed to save test chart";
            }
            catch (Exception ex)
            {
                return $"ScottPlot compatibility test failed: {ex.Message}";
            }
        }
    }
} 