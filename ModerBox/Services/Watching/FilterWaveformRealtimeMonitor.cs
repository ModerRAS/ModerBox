using ModerBox.Comtrade.FilterWaveform;
using ModerBox.Comtrade.FilterWaveform.Storage;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ModerBox.Services.Watching {
    public sealed class FilterWaveformRealtimeMonitor : IAsyncDisposable {
        private readonly string _sourceFolder;
        private readonly string _targetFile;
        private readonly bool _useSlidingWindowAlgorithm;
        private readonly int _ioWorkerCount;
        private readonly int _processWorkerCount;
        private readonly TimeSpan _quietPeriod;
        private readonly Action<string>? _status;
        private readonly Action<int, int>? _progress;

        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, PendingCfg> _pending = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, byte> _queuedOrRunning = new(StringComparer.OrdinalIgnoreCase);
        private readonly Channel<string> _queue = Channel.CreateUnbounded<string>(new UnboundedChannelOptions {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        private FileSystemWatcher? _watcher;
        private Task? _worker;

        private static readonly TimeSpan StableProbeInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan MissingPairRetryInterval = TimeSpan.FromSeconds(10);

        public FilterWaveformRealtimeMonitor(
            string sourceFolder,
            string targetFile,
            bool useSlidingWindowAlgorithm,
            int ioWorkerCount,
            int processWorkerCount,
            TimeSpan quietPeriod,
            Action<string>? status = null,
            Action<int, int>? progress = null) {
            _sourceFolder = sourceFolder;
            _targetFile = targetFile;
            _useSlidingWindowAlgorithm = useSlidingWindowAlgorithm;
            _ioWorkerCount = ioWorkerCount;
            _processWorkerCount = processWorkerCount;
            _quietPeriod = quietPeriod;
            _status = status;
            _progress = progress;
        }

        public void Start() {
            if (_watcher is not null) {
                return;
            }

            if (!Directory.Exists(_sourceFolder)) {
                throw new DirectoryNotFoundException($"源目录不存在: {_sourceFolder}");
            }

            _worker = Task.Run(() => ProcessQueueAsync(_cts.Token));
            _watcher = new FileSystemWatcher(_sourceFolder) {
                IncludeSubdirectories = true,
                Filter = "*.*",
                NotifyFilter = NotifyFilters.FileName
                    | NotifyFilters.LastWrite
                    | NotifyFilters.Size
                    | NotifyFilters.CreationTime,
                InternalBufferSize = 64 * 1024
            };

            _watcher.Created += OnFileChanged;
            _watcher.Changed += OnFileChanged;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;
            _watcher.EnableRaisingEvents = true;
        }

        public async Task StopAsync() {
            try {
                _cts.Cancel();
            } catch {
            }

            if (_watcher is not null) {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileChanged;
                _watcher.Changed -= OnFileChanged;
                _watcher.Renamed -= OnFileRenamed;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
            }

            _queue.Writer.TryComplete();

            if (_worker is not null) {
                try {
                    await _worker;
                } catch {
                }
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e) {
            SchedulePath(e.FullPath);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e) {
            SchedulePath(e.FullPath);
        }

        private void OnWatcherError(object sender, ErrorEventArgs e) {
            _status?.Invoke($"实时监视出现异常: {e.GetException().Message}");
        }

        private void SchedulePath(string path) {
            var cfgPath = ToCfgPath(path);
            if (cfgPath is null) {
                return;
            }

            ScheduleCfg(cfgPath);
        }

        private void ScheduleCfg(string cfgPath) {
            cfgPath = Path.GetFullPath(cfgPath);
            var dueAtUtc = DateTimeOffset.UtcNow.Add(_quietPeriod);
            var shouldStartWaiter = false;

            _pending.AddOrUpdate(
                cfgPath,
                _ => {
                    shouldStartWaiter = true;
                    return new PendingCfg(cfgPath, dueAtUtc);
                },
                (_, current) => current with { DueAtUtc = dueAtUtc });

            _status?.Invoke($"检测到文件变化，等待稳定: {Path.GetFileName(cfgPath)}");

            if (shouldStartWaiter) {
                _ = Task.Run(() => WaitAndQueueAsync(cfgPath, _cts.Token));
            }
        }

        private async Task WaitAndQueueAsync(string cfgPath, CancellationToken ct) {
            try {
                while (!ct.IsCancellationRequested) {
                    if (!_pending.TryGetValue(cfgPath, out var pending)) {
                        return;
                    }

                    var delay = pending.DueAtUtc - DateTimeOffset.UtcNow;
                    if (delay > TimeSpan.Zero) {
                        await Task.Delay(delay, ct);
                        continue;
                    }

                    if (!_pending.TryRemove(cfgPath, out _)) {
                        return;
                    }

                    if (!await WaitForStablePairAsync(cfgPath, ct)) {
                        _status?.Invoke($"文件未就绪，已跳过: {Path.GetFileName(cfgPath)}");
                        return;
                    }

                    if (!_queuedOrRunning.TryAdd(cfgPath, 0)) {
                        ScheduleCfg(cfgPath);
                        return;
                    }

                    await _queue.Writer.WriteAsync(cfgPath, ct);
                    return;
                }
            } catch (OperationCanceledException) {
            } catch (ChannelClosedException) {
            } catch (Exception ex) {
                _status?.Invoke($"加入实时队列失败: {ex.Message}");
            }
        }

        private async Task<bool> WaitForStablePairAsync(string cfgPath, CancellationToken ct) {
            var datPath = Path.ChangeExtension(cfgPath, ".dat");
            var deadline = DateTimeOffset.UtcNow.Add(_quietPeriod);

            while (!ct.IsCancellationRequested && DateTimeOffset.UtcNow <= deadline) {
                var first = TryCaptureSnapshot(cfgPath, datPath);
                if (first is not null) {
                    await Task.Delay(StableProbeInterval, ct);
                    var second = TryCaptureSnapshot(cfgPath, datPath);

                    if (second is not null && first.Value == second.Value && CanOpenForRead(cfgPath) && CanOpenForRead(datPath)) {
                        return true;
                    }
                }

                await Task.Delay(MissingPairRetryInterval, ct);
            }

            return false;
        }

        private async Task ProcessQueueAsync(CancellationToken ct) {
            await foreach (var cfgPath in _queue.Reader.ReadAllAsync(ct)) {
                try {
                    _progress?.Invoke(0, 100);
                    _status?.Invoke($"正在实时处理: {Path.GetFileName(cfgPath)}");

                    var result = await FilterWaveformStreamingFacade.ProcessSingleCfgWithSqliteAsync(
                        _sourceFolder,
                        cfgPath,
                        _targetFile,
                        _useSlidingWindowAlgorithm,
                        _ioWorkerCount,
                        _processWorkerCount);

                    _progress?.Invoke(100, 100);
                    _status?.Invoke(BuildResultMessage(result));
                } catch (OperationCanceledException) {
                    return;
                } catch (Exception ex) {
                    _status?.Invoke($"实时处理失败: {Path.GetFileName(cfgPath)}，{ex.Message}");
                } finally {
                    _queuedOrRunning.TryRemove(cfgPath, out _);
                }
            }
        }

        private static string BuildResultMessage(FilterWaveformSingleProcessResult result) {
            return result.Status switch {
                ProcessedComtradeFileStatus.Processed => $"实时处理完成: {Path.GetFileName(result.CfgPath)}",
                ProcessedComtradeFileStatus.SkippedNoMatch => $"已跳过无匹配通道文件: {Path.GetFileName(result.CfgPath)}",
                ProcessedComtradeFileStatus.ProcessedNoResult => $"已处理但无结果: {Path.GetFileName(result.CfgPath)}",
                _ => $"实时处理未完成: {Path.GetFileName(result.CfgPath)} ({result.Status})"
            };
        }

        private static FileSnapshot? TryCaptureSnapshot(string cfgPath, string datPath) {
            try {
                var cfg = new FileInfo(cfgPath);
                var dat = new FileInfo(datPath);
                if (!cfg.Exists || !dat.Exists) {
                    return null;
                }

                return new FileSnapshot(cfg.Length, cfg.LastWriteTimeUtc, dat.Length, dat.LastWriteTimeUtc);
            } catch {
                return null;
            }
        }

        private static bool CanOpenForRead(string path) {
            try {
                using var _ = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                return true;
            } catch {
                return false;
            }
        }

        private static string? ToCfgPath(string path) {
            var extension = Path.GetExtension(path);
            if (extension.Equals(".cfg", StringComparison.OrdinalIgnoreCase)) {
                return path;
            }

            if (extension.Equals(".dat", StringComparison.OrdinalIgnoreCase)) {
                return Path.ChangeExtension(path, ".cfg");
            }

            return null;
        }

        public async ValueTask DisposeAsync() {
            await StopAsync();
            _cts.Dispose();
        }

        private sealed record PendingCfg(string CfgPath, DateTimeOffset DueAtUtc);

        private readonly record struct FileSnapshot(
            long CfgLength,
            DateTime CfgLastWriteTimeUtc,
            long DatLength,
            DateTime DatLastWriteTimeUtc);
    }
}
