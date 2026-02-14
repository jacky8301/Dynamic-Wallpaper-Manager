using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;    // 为什么要引入两个dependencyInjection命名空间？因为CommunityToolkit.Mvvm.DependencyInjection是对Microsoft.Extensions.DependencyInjection的封装，提供了更简化的API来注册和解析依赖项。
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using WallpaperEngine.Common;
using WallpaperEngine.Data;
using WallpaperEngine.Services;
using WallpaperEngine.ViewModels;
using Application = System.Windows.Application;

namespace WallpaperEngine {
    /// Interaction logic for App.xaml
    public partial class App : Application {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);
        private const int SW_RESTORE = 9;
        private static Mutex _mutex = null;
        private const string AppGuid = "{80DEC730-14F5-4798-A4A7-EEEB4ADE1672}"; // 请替换为唯一名称
        bool _createNew = true;
        private SingleInstanceManager _singleInstanceManager;
        public App()
        {
            string wallpaperDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DynamicWallpaperManager");
            if (!Directory.Exists(wallpaperDbPath)) {
                Directory.CreateDirectory(wallpaperDbPath);
            }
            wallpaperDbPath = Path.Combine(wallpaperDbPath, "wallpapers.db");
            Ioc.Default.ConfigureServices(
                new ServiceCollection()
                    // 在这里注册你的服务               
                    .AddSingleton<DatabaseManager>(new DatabaseManager(wallpaperDbPath))
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IDataContextService, DataContextService>()
                    .AddSingleton<IWallpaperFileService, WallpaperFileService>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<WallpaperDetailViewModel>()
                    .AddSingleton<PreviewViewModel>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<CollectionViewModel>()
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

            _singleInstanceManager = new SingleInstanceManager(AppGuid);

            if (!_singleInstanceManager.IsFirstInstance) {
                // 不是第一个实例，发送参数到第一个实例
                SingleInstanceManager.SendArgsToFirstInstance(AppGuid, e.Args);
                Application.Current.Shutdown();
                return;
            }

            // 是第一个实例，开始监听
            _singleInstanceManager.ArgumentsReceived += OnArgumentsReceived;
            _singleInstanceManager.StartListening();

            base.OnStartup(e);
        }

        private void OnArgumentsReceived(object? sender, string?[] args)
        {
            // 在UI线程上激活主窗口
            Dispatcher.Invoke(() =>
            {
                ActivateMainWindow();

                // 处理接收到的参数
                if (args != null && args.Length > 0) {
                    // 在这里处理启动参数
                    // 例如：打开特定文件等
                    ProcessCommandLineArgs(args);
                }
            });
        }

        private void ActivateMainWindow()
        {
            if (MainWindow != null) {
                if (MainWindow.WindowState == WindowState.Minimized) {
                    MainWindow.WindowState = WindowState.Normal;
                }

                MainWindow.Activate();
                MainWindow.Topmost = true;
                MainWindow.Topmost = false;
                MainWindow.Show();
            }
        }

        private void ProcessCommandLineArgs(string[] args)
        {
            // 处理命令行参数的逻辑
            // 例如：
            // if (args.Length > 0 && File.Exists(args[0]))
            // {
            //     (MainWindow as MainWindow)?.OpenFile(args[0]);
            // }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
            _singleInstanceManager?.Dispose();
            base.OnExit(e);
        }
    }
}
