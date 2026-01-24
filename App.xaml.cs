using Serilog;
using System.Windows;
using Application = System.Windows.Application;

namespace WallpaperEngine {
    /// Interaction logic for App.xaml
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e)
        {
            // 配置 Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("log/dynamic_wallpaper_manager.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            Log.Information("Application starting up");

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }



}
