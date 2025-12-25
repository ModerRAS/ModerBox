using System;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Services.Scheduling {
    public enum ScheduleRecurrence {
        Daily = 0,
        Weekly = 1
    }

    public sealed record ScheduleOptions(ScheduleRecurrence Recurrence, TimeOnly TimeOfDay, DayOfWeek? DayOfWeek);

    public sealed class LocalRecurringScheduler : IAsyncDisposable {
        private readonly Func<CancellationToken, Task> _job;
        private readonly SemaphoreSlim _runLock = new(1, 1);

        private CancellationTokenSource? _cts;
        private Task? _loop;

        public LocalRecurringScheduler(Func<CancellationToken, Task> job) {
            _job = job;
        }

        public bool IsRunning => _loop is { IsCompleted: false };

        public void Start(ScheduleOptions options) {
            if (IsRunning) {
                return;
            }

            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => LoopAsync(options, _cts.Token));
        }

        public async Task StopAsync() {
            if (_cts is null) {
                return;
            }

            try {
                _cts.Cancel();
            } catch {
            }

            var loop = _loop;
            _loop = null;

            try {
                if (loop is not null) {
                    await loop;
                }
            } catch {
            }

            try {
                _cts.Dispose();
            } catch {
            }
            _cts = null;
        }

        private async Task LoopAsync(ScheduleOptions options, CancellationToken ct) {
            while (!ct.IsCancellationRequested) {
                var now = DateTimeOffset.Now;
                var next = ComputeNextRun(now, options);

                var delay = next - DateTimeOffset.Now;
                if (delay > TimeSpan.Zero) {
                    try {
                        await Task.Delay(delay, ct);
                    } catch (OperationCanceledException) {
                        break;
                    }
                }

                if (ct.IsCancellationRequested) {
                    break;
                }

                // 防止并发重入：如果上一次还在跑，这一次直接跳过，继续算下一个触发点
                if (!await _runLock.WaitAsync(0, ct).ConfigureAwait(false)) {
                    continue;
                }

                try {
                    await _job(ct).ConfigureAwait(false);
                } catch {
                } finally {
                    _runLock.Release();
                }
            }
        }

        public static DateTimeOffset ComputeNextRun(DateTimeOffset now, ScheduleOptions options) {
            var localNow = now.LocalDateTime;

            if (options.Recurrence == ScheduleRecurrence.Daily) {
                var candidateLocal = new DateTime(localNow.Year, localNow.Month, localNow.Day, options.TimeOfDay.Hour, options.TimeOfDay.Minute, options.TimeOfDay.Second);
                if (candidateLocal <= localNow) {
                    candidateLocal = candidateLocal.AddDays(1);
                }
                return new DateTimeOffset(candidateLocal, TimeZoneInfo.Local.GetUtcOffset(candidateLocal));
            }

            // Weekly
            var targetDay = options.DayOfWeek ?? DayOfWeek.Monday;
            var daysUntil = ((int)targetDay - (int)localNow.DayOfWeek + 7) % 7;

            var candidateDate = localNow.Date.AddDays(daysUntil);
            var candidateLocalWeekly = new DateTime(candidateDate.Year, candidateDate.Month, candidateDate.Day, options.TimeOfDay.Hour, options.TimeOfDay.Minute, options.TimeOfDay.Second);
            if (candidateLocalWeekly <= localNow) {
                candidateLocalWeekly = candidateLocalWeekly.AddDays(7);
            }

            return new DateTimeOffset(candidateLocalWeekly, TimeZoneInfo.Local.GetUtcOffset(candidateLocalWeekly));
        }

        public async ValueTask DisposeAsync() {
            await StopAsync();
            _runLock.Dispose();
        }
    }
}
