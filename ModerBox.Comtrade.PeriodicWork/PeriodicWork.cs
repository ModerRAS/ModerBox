using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModerBox.Comtrade.PeriodicWork {
    public static class PeriodicWork {

        public static void AddPeriodicWork(this IServiceCollection services) {
            services.AddSingleton<ActorSystem>(ActorSystem.Create("PeriodicWorkActorSystem"));
            services.AddTransient<OrthogonalDataActor>();
            services.AddTransient<NonOrthogonalDataActor>();
        }
    }
}
