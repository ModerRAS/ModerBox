﻿using Avalonia;
using Avalonia.ReactiveUI;
using System;
using Velopack.Sources;
using Velopack;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Coravel;

namespace ModerBox {
    internal sealed class Program {
        
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            VelopackApp.Build().Run();
            Env.host = Host.CreateDefaultBuilder()
                .ConfigureServices(service => {
                    service.AddSingleton(service);
                    service.AddScheduler();
                })
                .ConfigureLogging(logging => {
                    logging.ClearProviders();
                    logging.AddSimpleConsole(options => {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "[yyyy/MM/dd HH:mm:ss] ";
                    });
#if DEBUG
                    logging.AddDebug();
#endif
                }).Build();
            Env.host.Services.UseScheduler(scheduler => {
                // Easy peasy 👇
                scheduler
                    .ScheduleAsync(async () => {
                        try {
                            //await Util.UpdateMyApp();
                        } catch (Exception ex) {
                            Console.WriteLine(ex.ToString());
                        }
                    })
                    .EveryFiveMinutes().RunOnceAtStart();
            });
            Env.host.RunAsync();
            BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace()
                .UseReactiveUI();
    }
}
