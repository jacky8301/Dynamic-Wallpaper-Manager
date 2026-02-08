using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;    // 为什么要引入两个dependencyInjection命名空间？因为CommunityToolkit.Mvvm.DependencyInjection是对Microsoft.Extensions.DependencyInjection的封装，提供了更简化的API来注册和解析依赖项。
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using WallpaperEngine.Data;
using WallpaperEngine.Services;
using WallpaperEngine.ViewModels;
using WallpaperEngine.Views;
using Application = System.Windows.Application;

namespace WallpaperEngine {
    /// Interaction logic for App.xaml
    public partial class App : Application {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        private static System.Threading.Mutex _mutex;
        bool _createNew = true;
        public App()
        {
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // 在这里注册你的服务
                    .AddSingleton<DatabaseManager>(new DatabaseManager(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wallpapers.db")))
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IDataContextService, DataContextService>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<WallpaperDetailViewModel>()
                    .AddSingleton<PreviewViewModel>()
                    .AddSingleton<MainViewModel>()
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
            const string mutexName = "DynamicWallpaperManager"; // 请替换为唯一名称
            bool createdNew;
            // 尝试创建互斥体
            _mutex = new System.Threading.Mutex(true, mutexName, out createdNew);

            if (!createdNew) {
                // 如果互斥体已存在，则关闭当前实例
                System.Windows.MessageBox.Show("应用程序已在运行中", "提示");
                _createNew = createdNew;
                Current.Shutdown();
                return;
            }
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
            // 程序退出时释放互斥体
            if (_createNew) {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            base.OnExit(e);
        }
    }
}
