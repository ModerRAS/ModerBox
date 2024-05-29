using Avalonia;
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
        public static IHost host { get; private set; }
        private static async Task UpdateMyApp() {
            var mgr = new UpdateManager(new GithubSource("https://github.com/ModerRAS/ModerBox", null, false));

            // check for new version
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
                return; // no update available


            // download new version
            await mgr.DownloadUpdatesAsync(newVersion);

            // install new version and restart app
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            VelopackApp.Build().Run();
            host = Host.CreateDefaultBuilder()
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
            host.Services.UseScheduler(scheduler => {
                // Easy peasy 👇
                scheduler
                    .ScheduleAsync(async () => {
                        try {
                            await UpdateMyApp();
                        } catch (Exception ex) {
                            Console.WriteLine(ex.ToString());
                        }
                    })
                    .EveryFiveMinutes().RunOnceAtStart();
            });
            host.RunAsync();
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
