using Serilog;
using System.Windows;
using Application = System.Windows.Application;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;    // 为什么要引入两个dependencyInjection命名空间？因为CommunityToolkit.Mvvm.DependencyInjection是对Microsoft.Extensions.DependencyInjection的封装，提供了更简化的API来注册和解析依赖项。
using WallpaperEngine.ViewModels;
using WallpaperEngine.Services;

namespace WallpaperEngine {
    /// Interaction logic for App.xaml
    public partial class App : Application {
        public App()
        {
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // 在这里注册你的服务
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<WallpaperDetailViewModel>()
                    .BuildServiceProvider()
            );
        }
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
