using System;

namespace ModerBox.Comtrade.FilterWaveform {
    /// <summary>
    /// 合闸电阻退出时刻检测器
    /// 使用基于斜率变化的检测算法，通过陷波滤波器去除 50Hz 基波，然后检测电流信号的斜率突变点
    /// </summary>
    public class ClosingResistorExitDetector {
        private readonly double _samplingRate;
        private readonly double _sampleIntervalMs;

        // IIR 陷波滤波器系数（50Hz, Q=16.7）
        private readonly double[] _notchB;
        private readonly double[] _notchA;

        // IIR 高通滤波器系数（10Hz 截止）
        private readonly double[] _hpB;
        private readonly double[] _hpA;

        /// <summary>
        /// 创建合闸电阻退出时刻检测器
        /// </summary>
        /// <param name="samplingRate">采样率（Hz），默认 10000Hz</param>
        public ClosingResistorExitDetector(double samplingRate = 10000) {
            _samplingRate = samplingRate;
            _sampleIntervalMs = 1000.0 / samplingRate; // 每个采样点的时间间隔（毫秒）

            // 设计 50Hz 陷波滤波器
            (_notchB, _notchA) = DesignNotchFilter(50, 3);

            // 设计 10Hz 高通滤波器
            (_hpB, _hpA) = DesignHighpassFilter(10);
        }

        /// <summary>
        /// 设计 IIR 陷波滤波器
        /// </summary>
        /// <param name="f0">中心频率（Hz）</param>
        /// <param name="bw">带宽（Hz）</param>
        private (double[] b, double[] a) DesignNotchFilter(double f0, double bw) {
            double Q = f0 / bw;
            double omega0 = 2 * Math.PI * f0 / _samplingRate;
            double alpha = Math.Sin(omega0) / (2 * Q);
            double cosOmega0 = Math.Cos(omega0);

            double b0 = 1;
            double b1 = -2 * cosOmega0;
            double b2 = 1;
            double a0 = 1 + alpha;
            double a1 = -2 * cosOmega0;
            double a2 = 1 - alpha;

            // 归一化
            return (
                new double[] { b0 / a0, b1 / a0, b2 / a0 },
                new double[] { 1, a1 / a0, a2 / a0 }
            );
        }

        /// <summary>
        /// 设计一阶 IIR 高通滤波器
        /// </summary>
        /// <param name="fc">截止频率（Hz）</param>
        private (double[] b, double[] a) DesignHighpassFilter(double fc) {
            double omega = 2 * Math.PI * fc / _samplingRate;
            double cosOmega = Math.Cos(omega);
            double sinOmega = Math.Sin(omega);
            double alpha = sinOmega / (1 + cosOmega);

            double b0 = (1 + cosOmega) / 2;
            double b1 = -(1 + cosOmega);
            double a0 = 1 + alpha;
            double a1 = -(1 - alpha);

            return (
                new double[] { b0 / a0, b1 / a0 },
                new double[] { 1, a1 / a0 }
            );
        }

        /// <summary>
        /// 检测合闸电阻退出时刻
        /// </summary>
        /// <param name="current">电流波形数据</param>
        /// <returns>检测结果，如果未检测到则返回 null</returns>
        public ClosingResistorExitResult? DetectExitTime(double[]? current) {
            if (current == null || current.Length < 100) {
                return null;
            }

            // 第一步：信号预处理
            var filtered = Preprocess(current);

            // 第二步：计算斜率
            var slope = CalculateSlope(filtered, 15);

            // 平滑处理
            var smoothedSlope = SmoothSlope(slope, 5);

            // 第三步：检测斜率变化
            var detectionIndex = DetectSlopeChange(smoothedSlope);
            if (detectionIndex < 0) {
                return null;
            }

            // 第四步：亚像素精定位
            var (refinedIndex, confidence) = RefinePosition(smoothedSlope, detectionIndex);

            // 计算时间
            double timeMs = refinedIndex * _sampleIntervalMs;

            return new ClosingResistorExitResult {
                TimeMs = timeMs,
                RawDetectionIndex = detectionIndex,
                RefinedIndex = refinedIndex,
                Confidence = confidence
            };
        }

        /// <summary>
        /// 信号预处理：去直流、陷波滤波、高通滤波
        /// </summary>
        private double[] Preprocess(double[] current) {
            // 1. 去直流分量
            double mean = 0;
            int meanCount = Math.Min(100, current.Length);
            for (int i = 0; i < meanCount; i++) {
                mean += current[i];
            }
            mean /= meanCount;

            var dcRemoved = new double[current.Length];
            for (int i = 0; i < current.Length; i++) {
                dcRemoved[i] = current[i] - mean;
            }

            // 2. 陷波滤波（去除 50Hz 基波）
            var notchFiltered = ApplyIIRFilter(dcRemoved, _notchB, _notchA);

            // 3. 高通滤波（去除低频漂移）
            var hpFiltered = ApplyIIRFilter(notchFiltered, _hpB, _hpA);

            return hpFiltered;
        }

        /// <summary>
        /// 应用 IIR 滤波器（零相位滤波：正反向滤波）
        /// </summary>
        private double[] ApplyIIRFilter(double[] data, double[] b, double[] a) {
            int order = Math.Max(b.Length, a.Length);
            var result = new double[data.Length];

            // 正向滤波
            var forward = new double[data.Length];
            var x = new double[order];
            var y = new double[order];

            for (int i = 0; i < data.Length; i++) {
                // 移位输入
                for (int j = order - 1; j > 0; j--) {
                    x[j] = x[j - 1];
                }
                x[0] = data[i];

                // 计算输出
                double output = 0;
                for (int j = 0; j < b.Length && j < order; j++) {
                    output += b[j] * x[j];
                }
                for (int j = 1; j < a.Length && j < order; j++) {
                    output -= a[j] * y[j - 1];
                }

                // 移位输出
                for (int j = order - 1; j > 0; j--) {
                    y[j] = y[j - 1];
                }
                y[0] = output;

                forward[i] = output;
            }

            // 反向滤波
            x = new double[order];
            y = new double[order];

            for (int i = data.Length - 1; i >= 0; i--) {
                for (int j = order - 1; j > 0; j--) {
                    x[j] = x[j - 1];
                }
                x[0] = forward[i];

                double output = 0;
                for (int j = 0; j < b.Length && j < order; j++) {
                    output += b[j] * x[j];
                }
                for (int j = 1; j < a.Length && j < order; j++) {
                    output -= a[j] * y[j - 1];
                }

                for (int j = order - 1; j > 0; j--) {
                    y[j] = y[j - 1];
                }
                y[0] = output;

                result[i] = output;
            }

            return result;
        }

        /// <summary>
        /// 使用多项式拟合计算斜率
        /// </summary>
        private double[] CalculateSlope(double[] data, int windowSize) {
            var slopes = new double[data.Length];
            int halfWin = windowSize / 2;

            for (int i = 0; i < data.Length; i++) {
                int start = Math.Max(0, i - halfWin);
                int end = Math.Min(data.Length, i + halfWin + 1);
                int len = end - start;

                if (len < 5) {
                    slopes[i] = 0;
                    continue;
                }

                // 使用二阶多项式拟合 y = a*t^2 + b*t + c
                // 斜率 = 2*a*t + b，在中心点的斜率
                var (a, b, _) = FitPolynomial2(data, start, len);
                int centerT = len / 2;
                slopes[i] = 2 * a * centerT + b;
            }

            return slopes;
        }

        /// <summary>
        /// 二阶多项式拟合
        /// </summary>
        private (double a, double b, double c) FitPolynomial2(double[] data, int start, int len) {
            // 使用最小二乘法拟合 y = a*t^2 + b*t + c
            // 对于小窗口，使用简化的正规方程求解

            double sumT = 0, sumT2 = 0, sumT3 = 0, sumT4 = 0;
            double sumY = 0, sumTY = 0, sumT2Y = 0;

            for (int i = 0; i < len; i++) {
                double t = i;
                double y = data[start + i];
                double t2 = t * t;
                double t3 = t2 * t;
                double t4 = t3 * t;

                sumT += t;
                sumT2 += t2;
                sumT3 += t3;
                sumT4 += t4;
                sumY += y;
                sumTY += t * y;
                sumT2Y += t2 * y;
            }

            double n = len;

            // 使用克莱姆法则求解 3x3 线性方程组
            // | n     sumT   sumT2 | | c |   | sumY   |
            // | sumT  sumT2  sumT3 | | b | = | sumTY  |
            // | sumT2 sumT3  sumT4 | | a |   | sumT2Y |

            double[,] A = {
                { n, sumT, sumT2 },
                { sumT, sumT2, sumT3 },
                { sumT2, sumT3, sumT4 }
            };

            double[] B = { sumY, sumTY, sumT2Y };

            // 计算行列式
            double det = Determinant3x3(A);
            if (Math.Abs(det) < 1e-10) {
                return (0, 0, 0);
            }

            // 使用克莱姆法则
            double[,] Ac = { { B[0], sumT, sumT2 }, { B[1], sumT2, sumT3 }, { B[2], sumT3, sumT4 } };
            double[,] Ab = { { n, B[0], sumT2 }, { sumT, B[1], sumT3 }, { sumT2, B[2], sumT4 } };
            double[,] Aa = { { n, sumT, B[0] }, { sumT, sumT2, B[1] }, { sumT2, sumT3, B[2] } };

            double c = Determinant3x3(Ac) / det;
            double b = Determinant3x3(Ab) / det;
            double a = Determinant3x3(Aa) / det;

            return (a, b, c);
        }

        private double Determinant3x3(double[,] m) {
            return m[0, 0] * (m[1, 1] * m[2, 2] - m[1, 2] * m[2, 1])
                 - m[0, 1] * (m[1, 0] * m[2, 2] - m[1, 2] * m[2, 0])
                 + m[0, 2] * (m[1, 0] * m[2, 1] - m[1, 1] * m[2, 0]);
        }

        /// <summary>
        /// 滑动平均平滑
        /// </summary>
        private double[] SmoothSlope(double[] slope, int windowSize) {
            var smoothed = new double[slope.Length];

            for (int i = 0; i < slope.Length; i++) {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(slope.Length, i + windowSize / 2 + 1);
                double sum = 0;
                for (int j = start; j < end; j++) {
                    sum += slope[j];
                }
                smoothed[i] = sum / (end - start);
            }

            return smoothed;
        }

        /// <summary>
        /// 检测斜率变化点
        /// </summary>
        private int DetectSlopeChange(double[] slope) {
            if (slope.Length < 10) {
                return -1;
            }

            // 计算斜率一阶差分
            var slopeDiff = new double[slope.Length - 1];
            for (int i = 0; i < slope.Length - 1; i++) {
                slopeDiff[i] = slope[i + 1] - slope[i];
            }

            // 动态阈值计算（基于最大值的 20%，降低阈值提高灵敏度）
            double maxDiff = 0;
            for (int i = 0; i < slopeDiff.Length; i++) {
                maxDiff = Math.Max(maxDiff, Math.Abs(slopeDiff[i]));
            }
            double threshold = 0.2 * maxDiff;

            if (threshold < 1e-10) {
                return -1;
            }

            // 检测超过阈值的点
            var candidates = new System.Collections.Generic.List<int>();
            for (int i = 0; i < slopeDiff.Length; i++) {
                if (Math.Abs(slopeDiff[i]) > threshold) {
                    candidates.Add(i);
                }
            }

            // 去抖动：连续 2 个点都超过阈值（放宽条件）
            var validCandidates = new System.Collections.Generic.List<int>();
            for (int i = 0; i < candidates.Count - 1; i++) {
                if (candidates[i + 1] == candidates[i] + 1) {
                    validCandidates.Add(candidates[i] + 1);
                }
            }

            // 如果没有连续点，尝试使用单独的候选点
            if (validCandidates.Count == 0 && candidates.Count > 0) {
                validCandidates.AddRange(candidates);
            }

            // 多重条件过滤（放宽范围：5-100ms）
            var filtered = new System.Collections.Generic.List<int>();
            for (int i = 0; i < validCandidates.Count; i++) {
                int idx = validCandidates[i];
                double t = idx * _sampleIntervalMs;

                // 条件1：时刻在合理范围内
                if (t < 5 || t > 100) {
                    continue;
                }

                filtered.Add(idx);
            }

            // 选择斜率变化绝对值最大的点
            if (filtered.Count > 0) {
                int bestIdx = filtered[0];
                double bestDiff = Math.Abs(slopeDiff[bestIdx]);
                for (int i = 1; i < filtered.Count; i++) {
                    if (Math.Abs(slopeDiff[filtered[i]]) > bestDiff) {
                        bestDiff = Math.Abs(slopeDiff[filtered[i]]);
                        bestIdx = filtered[i];
                    }
                }
                return bestIdx;
            }

            // 如果还没有找到，返回最大变化点
            if (candidates.Count > 0) {
                int bestIdx = candidates[0];
                double bestDiff = Math.Abs(slopeDiff[bestIdx]);
                for (int i = 1; i < candidates.Count; i++) {
                    if (Math.Abs(slopeDiff[candidates[i]]) > bestDiff) {
                        bestDiff = Math.Abs(slopeDiff[candidates[i]]);
                        bestIdx = candidates[i];
                    }
                }
                return bestIdx;
            }

            return -1;
        }

        /// <summary>
        /// 亚像素精定位
        /// </summary>
        private (double refinedIndex, double confidence) RefinePosition(double[] slope, int idx) {
            int windowStart = Math.Max(0, idx - 5);
            int windowEnd = Math.Min(slope.Length, idx + 6);

            if (windowEnd - windowStart < 7) {
                return (idx, 0.5);
            }

            // 取局部斜率
            int len = windowEnd - windowStart;
            var localSlope = new double[len];
            for (int i = 0; i < len; i++) {
                localSlope[i] = slope[windowStart + i];
            }

            // 抛物线拟合
            var (a, b, _) = FitPolynomial2(localSlope, 0, len);

            if (Math.Abs(a) < 1e-10) {
                return (idx, 0.5);
            }

            // 求抛物线极值点位置
            double peakPosition = -b / (2 * a);
            double refinedIndex = windowStart + peakPosition;

            // 计算置信度（基于抛物线拟合的 R^2）
            double sumY = 0, sumY2 = 0, sumResidual = 0;
            for (int i = 0; i < len; i++) {
                double t = i;
                double predicted = a * t * t + b * t + localSlope[0];
                sumY += localSlope[i];
                sumY2 += localSlope[i] * localSlope[i];
                sumResidual += (localSlope[i] - predicted) * (localSlope[i] - predicted);
            }
            double meanY = sumY / len;
            double ssTotal = sumY2 - len * meanY * meanY;
            double confidence = ssTotal > 0 ? 1 - sumResidual / ssTotal : 0.5;
            confidence = Math.Max(0, Math.Min(1, confidence));

            return (refinedIndex, confidence);
        }

        /// <summary>
        /// 检测合闸电阻投入时间（电流开始到合闸电阻退出的时间间隔）
        /// </summary>
        /// <param name="current">电流波形数据</param>
        /// <returns>检测结果，如果未检测到则返回 null</returns>
        public ClosingResistorDurationResult? DetectClosingResistorDuration(double[]? current) {
            if (current == null || current.Length < 100) {
                return null;
            }

            // 第一步：检测电流开始时刻（合闸电阻投入时刻）
            int currentStartIndex = DetectCurrentStartIndex(current);
            if (currentStartIndex < 0) {
                return null;
            }

            // 第二步：检测合闸电阻退出时刻
            int resistorExitIndex = DetectResistorExitIndex(current, currentStartIndex);
            if (resistorExitIndex < 0 || resistorExitIndex <= currentStartIndex) {
                return null;
            }

            // 第三步：计算投入时间
            double currentStartTimeMs = currentStartIndex * _sampleIntervalMs;
            double resistorExitTimeMs = resistorExitIndex * _sampleIntervalMs;
            double durationMs = resistorExitTimeMs - currentStartTimeMs;

            return new ClosingResistorDurationResult {
                CurrentStartTimeMs = currentStartTimeMs,
                ResistorExitTimeMs = resistorExitTimeMs,
                DurationMs = durationMs,
                CurrentStartIndex = currentStartIndex,
                ResistorExitIndex = resistorExitIndex,
                Confidence = 0.9
            };
        }

        /// <summary>
        /// 检测电流开始时刻（合闸时刻）
        /// 使用滑动窗口检测电流从静默到有显著电流的时刻
        /// </summary>
        private int DetectCurrentStartIndex(double[] current) {
            int windowSize = (int)(_samplingRate / 500); // 2ms 窗口
            if (windowSize < 10) windowSize = 10;

            // 计算初始噪声水平（前 5ms 的标准差）
            int noiseWindow = (int)(_samplingRate * 0.005);
            if (noiseWindow < 20) noiseWindow = 20;
            if (noiseWindow >= current.Length) noiseWindow = current.Length / 4;

            double noiseMean = 0;
            for (int i = 0; i < noiseWindow; i++) {
                noiseMean += current[i];
            }
            noiseMean /= noiseWindow;

            double noiseStd = 0;
            for (int i = 0; i < noiseWindow; i++) {
                double diff = current[i] - noiseMean;
                noiseStd += diff * diff;
            }
            noiseStd = Math.Sqrt(noiseStd / noiseWindow);

            // 找到稳态时的最大电流幅值（用于判断显著电流）
            // 扫描后面的数据找到典型幅值
            double typicalAmplitude = 0;
            for (int i = noiseWindow; i < Math.Min(current.Length, noiseWindow + (int)(_samplingRate * 0.1)); i++) {
                typicalAmplitude = Math.Max(typicalAmplitude, Math.Abs(current[i]));
            }

            // 显著电流阈值：取最大幅值的 10% 或噪声的 10 倍，取较大者
            double significantThreshold = Math.Max(typicalAmplitude * 0.1, noiseStd * 10);
            significantThreshold = Math.Max(significantThreshold, 0.05); // 最小 0.05A

            // 滑动窗口检测：找到第一个显著超过阈值的点
            for (int i = noiseWindow; i < current.Length - windowSize; i++) {
                // 检查当前点的绝对值是否显著
                if (Math.Abs(current[i] - noiseMean) > significantThreshold) {
                    // 验证这不是单独的噪声尖峰：检查后续几个点
                    int confirmCount = 0;
                    for (int j = i; j < Math.Min(i + windowSize, current.Length); j++) {
                        if (Math.Abs(current[j] - noiseMean) > significantThreshold * 0.5) {
                            confirmCount++;
                        }
                    }
                    
                    // 如果超过一半的点都超过阈值的一半，认为是有效起点
                    if (confirmCount > windowSize / 2) {
                        // 回退找到电流刚开始变化的点
                        for (int k = i - 1; k >= 0; k--) {
                            if (Math.Abs(current[k] - noiseMean) < noiseStd * 3) {
                                return k + 1;
                            }
                        }
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// 检测合闸电阻退出时刻
        /// 在电流开始后，检测电流幅值包络发生明显增大的时刻
        /// 合闸电阻退出时电流幅值会跳变增大
        /// </summary>
        private int DetectResistorExitIndex(double[] current, int startIndex) {
            // 合闸电阻投入时间通常在 8-20ms，所以退出时刻应该在电流开始后 8ms 以后
            int minDuration = (int)(_samplingRate * 0.008); // 最小 8ms
            int maxDuration = (int)(_samplingRate * 0.022); // 最大 22ms
            
            int searchStart = startIndex + minDuration;
            int searchEnd = Math.Min(current.Length, startIndex + maxDuration);
            
            if (searchStart >= searchEnd || searchStart >= current.Length - 100) {
                return -1;
            }

            // 计算每个采样点的瞬时幅值（使用绝对值的包络）
            // 使用短窗口滑动最大值来估计包络
            int envWindowSize = (int)(_samplingRate * 0.005); // 5ms 窗口
            if (envWindowSize < 10) envWindowSize = 10;

            // 计算幅值包络
            var envelope = new double[searchEnd - searchStart];
            for (int i = 0; i < envelope.Length; i++) {
                double maxVal = 0;
                int windowStart = searchStart + i;
                int windowEnd = Math.Min(windowStart + envWindowSize, current.Length);
                for (int j = windowStart; j < windowEnd; j++) {
                    maxVal = Math.Max(maxVal, Math.Abs(current[j]));
                }
                envelope[i] = maxVal;
            }

            // 平滑包络
            var smoothedEnv = new double[envelope.Length];
            int smoothWindow = envWindowSize / 2;
            for (int i = 0; i < envelope.Length; i++) {
                double sum = 0;
                int count = 0;
                for (int j = Math.Max(0, i - smoothWindow); j < Math.Min(envelope.Length, i + smoothWindow + 1); j++) {
                    sum += envelope[j];
                    count++;
                }
                smoothedEnv[i] = sum / count;
            }

            // 计算包络的变化率（只关注增大的变化）
            double maxIncrease = 0;
            int maxIncreaseIdx = -1;
            
            for (int i = smoothWindow; i < smoothedEnv.Length - smoothWindow; i++) {
                // 计算前后窗口的平均值
                double prevAvg = 0, nextAvg = 0;
                for (int j = 0; j < smoothWindow; j++) {
                    prevAvg += smoothedEnv[i - smoothWindow + j];
                    nextAvg += smoothedEnv[i + j];
                }
                prevAvg /= smoothWindow;
                nextAvg /= smoothWindow;
                
                // 只关注增大的变化（合闸电阻退出时幅值增大）
                double increase = nextAvg - prevAvg;
                if (increase > maxIncrease && prevAvg > 0.01) {
                    // 计算相对增长率
                    double relIncrease = increase / prevAvg;
                    if (relIncrease > 0.2) { // 至少 20% 的增长
                        maxIncrease = increase;
                        maxIncreaseIdx = searchStart + i;
                    }
                }
            }

            if (maxIncreaseIdx > 0) {
                return maxIncreaseIdx;
            }

            // 使用斜率检测作为备用
            return DetectResistorExitBySlope(current, startIndex, searchStart, searchEnd);
        }

        /// <summary>
        /// 使用斜率变化检测合闸电阻退出时刻（备用方法）
        /// </summary>
        private int DetectResistorExitBySlope(double[] current, int startIndex, int searchStart, int searchEnd) {
            int searchLength = searchEnd - searchStart;
            if (searchLength < 50) {
                return searchStart + searchLength / 2;
            }

            var searchData = new double[searchLength];
            Array.Copy(current, searchStart, searchData, 0, searchLength);

            // 对搜索区域进行预处理（滤波去除 50Hz 基波）
            var filtered = Preprocess(searchData);

            // 计算斜率（使用更大的窗口以平滑噪声）
            var slope = CalculateSlope(filtered, 21);

            // 计算斜率一阶差分（加速度）
            var slopeDiff = new double[slope.Length - 1];
            for (int i = 0; i < slope.Length - 1; i++) {
                slopeDiff[i] = slope[i + 1] - slope[i];
            }

            // 找到最大变化点
            int maxIdx = 0;
            double maxVal = 0;
            for (int i = 0; i < slopeDiff.Length; i++) {
                if (Math.Abs(slopeDiff[i]) > maxVal) {
                    maxVal = Math.Abs(slopeDiff[i]);
                    maxIdx = i;
                }
            }

            return searchStart + maxIdx;
        }
    }
}
