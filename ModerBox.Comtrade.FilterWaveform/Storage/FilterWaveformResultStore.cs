using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    public sealed class FilterWaveformResultStore : IAsyncDisposable {
        private readonly string _dbPath;
        private readonly Channel<StoreItem> _channel;
        private readonly CancellationTokenSource _cts = new();
        private Task? _consumer;

        private const int DefaultBatchSize = 50;

        private readonly record struct StoreItem(FilterWaveformResultEntity? Result, ProcessedComtradeFileEntity? Processed);

        public FilterWaveformResultStore(string dbPath, int capacity = 256) {
            _dbPath = dbPath;
            _channel = Channel.CreateBounded<StoreItem>(new BoundedChannelOptions(capacity) {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public async Task InitializeAsync(bool overwriteExisting = true) {
            var dir = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrWhiteSpace(dir)) {
                Directory.CreateDirectory(dir);
            }

            if (overwriteExisting && File.Exists(_dbPath)) {
                File.Delete(_dbPath);
            }

            using (var db = FilterWaveformResultDbContext.Create(_dbPath)) {
                await db.Database.EnsureCreatedAsync(_cts.Token);
            }

            _consumer = Task.Run(ConsumeAsync, _cts.Token);
        }

        public ValueTask EnqueueResultAsync(ACFilterSheetSpec spec, string? imagePath = null, string? sourceCfgPath = null) {
            return _channel.Writer.WriteAsync(new StoreItem(ToResultEntity(spec, imagePath, sourceCfgPath), null), _cts.Token);
        }

        // Backward-compat for existing call sites
        public ValueTask EnqueueAsync(ACFilterSheetSpec spec, string? imagePath = null, string? sourceCfgPath = null) {
            return EnqueueResultAsync(spec, imagePath, sourceCfgPath);
        }

        public ValueTask EnqueueProcessedAsync(string cfgPath, ProcessedComtradeFileStatus status, string? note = null) {
            var now = DateTime.UtcNow;
            var entity = new ProcessedComtradeFileEntity {
                CfgPath = cfgPath,
                Status = status,
                LastUpdatedUtc = now,
                FirstSeenUtc = now,
                Note = note
            };
            return _channel.Writer.WriteAsync(new StoreItem(null, entity), _cts.Token);
        }

        public ValueTask EnqueueResultWithProcessedAsync(ACFilterSheetSpec spec, string cfgPath, ProcessedComtradeFileStatus status = ProcessedComtradeFileStatus.Processed, string? imagePath = null, string? note = null) {
            var now = DateTime.UtcNow;
            var processed = new ProcessedComtradeFileEntity {
                CfgPath = cfgPath,
                Status = status,
                LastUpdatedUtc = now,
                FirstSeenUtc = now,
                Note = note
            };
            var result = ToResultEntity(spec, imagePath, sourceCfgPath: cfgPath);
            return _channel.Writer.WriteAsync(new StoreItem(result, processed), _cts.Token);
        }

        public async Task CompleteAsync() {
            _channel.Writer.TryComplete();
            if (_consumer is not null) {
                await _consumer;
            }
        }

        public List<ACFilterSheetSpec> ReadAllForExport() {
            using var db = FilterWaveformResultDbContext.Create(_dbPath);
            var rows = db.Results
                .OrderBy(r => r.Time)
                .ThenBy(r => r.Name)
                .AsEnumerable()
                .Select(r => new ACFilterSheetSpec {
                    Name = r.Name,
                    Time = r.Time,
                    SwitchType = r.SwitchType,
                    WorkType = r.WorkType,
                    PhaseATimeInterval = r.PhaseATimeInterval,
                    PhaseBTimeInterval = r.PhaseBTimeInterval,
                    PhaseCTimeInterval = r.PhaseCTimeInterval,
                    PhaseAVoltageZeroCrossingDiff = r.PhaseAVoltageZeroCrossingDiff,
                    PhaseBVoltageZeroCrossingDiff = r.PhaseBVoltageZeroCrossingDiff,
                    PhaseCVoltageZeroCrossingDiff = r.PhaseCVoltageZeroCrossingDiff,
                    PhaseAClosingResistorDurationMs = r.PhaseAClosingResistorDurationMs,
                    PhaseBClosingResistorDurationMs = r.PhaseBClosingResistorDurationMs,
                    PhaseCClosingResistorDurationMs = r.PhaseCClosingResistorDurationMs,
                    SignalPicture = Array.Empty<byte>()
                })
                .ToList();

            return rows;
        }

        private async Task ConsumeAsync() {
            using var db = FilterWaveformResultDbContext.Create(_dbPath);
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var pending = 0;

            await foreach (var item in _channel.Reader.ReadAllAsync(_cts.Token)) {
                var itemPending = 0;
                if (item.Result is not null) {
                    db.Results.Add(item.Result);
                    itemPending++;
                }

                if (item.Processed is not null) {
                    // Upsert by CfgPath
                    var existing = await db.ProcessedFiles.FirstOrDefaultAsync(p => p.CfgPath == item.Processed.CfgPath, _cts.Token);
                    if (existing is null) {
                        db.ProcessedFiles.Add(item.Processed);
                    } else {
                        existing.Status = item.Processed.Status;
                        existing.LastUpdatedUtc = item.Processed.LastUpdatedUtc;
                        existing.Note = item.Processed.Note;
                    }
                    itemPending++;
                }

                pending += itemPending;
                if (pending >= DefaultBatchSize) {
                    await db.SaveChangesAsync(_cts.Token);
                    db.ChangeTracker.Clear();
                    pending = 0;
                }
            }

            if (pending > 0) {
                await db.SaveChangesAsync(_cts.Token);
                db.ChangeTracker.Clear();
            }
        }

        private static FilterWaveformResultEntity ToResultEntity(ACFilterSheetSpec spec, string? imagePath, string? sourceCfgPath) {
            return new FilterWaveformResultEntity {
                Name = spec.Name ?? string.Empty,
                Time = spec.Time,
                SwitchType = spec.SwitchType,
                WorkType = spec.WorkType,
                PhaseATimeInterval = spec.PhaseATimeInterval,
                PhaseBTimeInterval = spec.PhaseBTimeInterval,
                PhaseCTimeInterval = spec.PhaseCTimeInterval,
                PhaseAVoltageZeroCrossingDiff = spec.PhaseAVoltageZeroCrossingDiff,
                PhaseBVoltageZeroCrossingDiff = spec.PhaseBVoltageZeroCrossingDiff,
                PhaseCVoltageZeroCrossingDiff = spec.PhaseCVoltageZeroCrossingDiff,
                PhaseAClosingResistorDurationMs = spec.PhaseAClosingResistorDurationMs,
                PhaseBClosingResistorDurationMs = spec.PhaseBClosingResistorDurationMs,
                PhaseCClosingResistorDurationMs = spec.PhaseCClosingResistorDurationMs,
                ImagePath = imagePath,
                SourceCfgPath = sourceCfgPath
            };
        }

        public async ValueTask DisposeAsync() {
            try {
                _cts.Cancel();
            } catch {
            }

            try {
                _channel.Writer.TryComplete();
            } catch {
            }

            if (_consumer is not null) {
                try {
                    await _consumer;
                } catch {
                }
            }

            _cts.Dispose();
        }
    }
}
