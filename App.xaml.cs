using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;    // 为什么要引入两个dependencyInjection命名空间？因为CommunityToolkit.Mvvm.DependencyInjection是对Microsoft.Extensions.DependencyInjection的封装，提供了更简化的API来注册和解析依赖项。
using Serilog;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using WallpaperEngine.Common;
using WallpaperEngine.Data;
using WallpaperEngine.Services;
using WallpaperEngine.ViewModels;
using Application = System.Windows.Application;

namespace WallpaperEngine {
    /// <summary>
    /// 应用程序入口点，负责依赖注入配置、Serilog 日志初始化、单实例管理及生命周期控制
    /// </summary>
    public partial class App : Application {
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        private const string AppGuid = "{80DEC730-14F5-4798-A4A7-EEEB4ADE1672}";
        private SingleInstanceManager _singleInstanceManager;
        private ServiceProvider _serviceProvider;
        public App()
        {
            // 配置 Serilog（在所有其他初始化之前，以捕获数据库初始化日志）
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("log/dynamic_wallpaper_manager.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .CreateLogger();

            string wallpaperDbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "DynamicWallpaperManager");
            if (!Directory.Exists(wallpaperDbPath)) {
                Directory.CreateDirectory(wallpaperDbPath);
            }
            wallpaperDbPath = Path.Combine(wallpaperDbPath, "wallpapers.db");
            _serviceProvider = new ServiceCollection()
                    // 在这里注册你的服务
                    .AddSingleton<DatabaseManager>(new DatabaseManager(wallpaperDbPath))
                    .AddSingleton<ICategoryService, CategoryService>()
                    .AddSingleton<ISettingsService, SettingsService>()
                    .AddSingleton<IDataContextService, DataContextService>()
                    .AddSingleton<IWallpaperFileService, WallpaperFileService>()
                    .AddSingleton<SettingsViewModel>()
                    .AddSingleton<WallpaperDetailViewModel>()
                    .AddSingleton<MainViewModel>()
                    .AddSingleton<FavoriteViewModel>()
                    .AddSingleton<CollectionViewModel>()
                    .AddSingleton<CategoryManagementViewModel>()
                    .BuildServiceProvider();
            Ioc.Default.ConfigureServices(_serviceProvider);
        }
        /// <summary>
        /// 应用程序启动时执行，配置日志、初始化单实例管理器并开始监听
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Information("Application starting up");

            // 迁移壁纸ID：将数据库中的Id回写到project.json文件
            //try
            //{
            //    var dbManager = Ioc.Default.GetService<DatabaseManager>();
            //    dbManager?.MigrateWallpaperIds();
            //}
            //catch (Exception ex)
            //{
            //    Log.Warning("壁纸ID迁移失败: {Message}", ex.Message);
            //}

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

        /// <summary>
        /// 当从其他实例接收到启动参数时，在 UI 线程上激活主窗口并处理参数
        /// </summary>
        private void OnArgumentsReceived(object? sender, string?[] args)
        {
            // 在UI线程上激活主窗口
            Dispatcher.Invoke(() =>
            {
                Log.Information("收到启动参数: {ArgCount} 个", args?.Length ?? 0);
                ActivateMainWindow();

                // 处理接收到的参数
                if (args != null && args.Length > 0) {
                    // 在这里处理启动参数
                    // 例如：打开特定文件等
                    ProcessCommandLineArgs(args);
                }
            });
        }

        /// <summary>
        /// 激活并显示主窗口，将其从最小化状态恢复并置于前台
        /// </summary>
        private void ActivateMainWindow()
        {
            Log.Debug("激活主窗口");
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

        /// <summary>
        /// 处理从其他实例传递过来的命令行参数
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        private void ProcessCommandLineArgs(string?[] args)
        {
            // 处理命令行参数的逻辑
        }

        /// <summary>
        /// 应用程序退出时执行，关闭日志并释放单实例管理器资源
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
            _serviceProvider?.Dispose();
            _singleInstanceManager?.Dispose();
            base.OnExit(e);
        }
    }
}
