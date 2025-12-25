using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.FilterWaveform.Storage {
    public sealed class FilterWaveformResultStore : IAsyncDisposable {
        private readonly string _dbPath;
        private readonly Channel<FilterWaveformResultEntity> _channel;
        private readonly CancellationTokenSource _cts = new();
        private Task? _consumer;

        public FilterWaveformResultStore(string dbPath, int capacity = 256) {
            _dbPath = dbPath;
            _channel = Channel.CreateBounded<FilterWaveformResultEntity>(new BoundedChannelOptions(capacity) {
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

        public void Enqueue(ACFilterSheetSpec spec, string? imagePath = null, string? sourceCfgPath = null) {
            if (!_channel.Writer.TryWrite(ToEntity(spec, imagePath, sourceCfgPath))) {
                _channel.Writer.WriteAsync(ToEntity(spec, imagePath, sourceCfgPath), _cts.Token).AsTask().GetAwaiter().GetResult();
            }
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

            await foreach (var entity in _channel.Reader.ReadAllAsync(_cts.Token)) {
                db.Results.Add(entity);
                await db.SaveChangesAsync(_cts.Token);
            }
        }

        private static FilterWaveformResultEntity ToEntity(ACFilterSheetSpec spec, string? imagePath, string? sourceCfgPath) {
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
