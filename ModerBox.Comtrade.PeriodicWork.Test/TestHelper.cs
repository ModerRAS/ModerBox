using Akka.Actor;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork.Test {
    public static class TestHelper {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan ShortTimeout = TimeSpan.FromSeconds(30);
        
        /// <summary>
        /// 安全地执行带超时的 Ask 操作
        /// </summary>
        public static async Task<T> SafeAsk<T>(this IActorRef actor, object message, TimeSpan? timeout = null, CancellationToken cancellationToken = default) {
            var timeoutToUse = timeout ?? DefaultTimeout;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeoutToUse);
            
            try {
                return await actor.Ask<T>(message, timeoutToUse, cts.Token);
            } catch (TaskCanceledException) {
                throw new TimeoutException($"Actor Ask 操作超时 ({timeoutToUse.TotalSeconds} 秒)");
            }
        }
        
        /// <summary>
        /// 安全地终止 ActorSystem
        /// </summary>
        public static async Task SafeTerminate(this ActorSystem actorSystem, TimeSpan? timeout = null) {
            if (actorSystem == null) return;
            
            var timeoutToUse = timeout ?? TimeSpan.FromSeconds(10);
            
            try {
                await actorSystem.Terminate();
                await actorSystem.WhenTerminated.WaitAsync(timeoutToUse);
            } catch (TimeoutException) {
                Console.WriteLine($"ActorSystem 终止超时 ({timeoutToUse.TotalSeconds} 秒)");
            }
        }
    }
} 