using System.Diagnostics;
using System.IO;
using Serilog;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 壁纸预览服务，通过启动 Wallpaper Engine 进程实现壁纸的窗口化预览
    /// </summary>
    public class PreviewService {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;
        private Process? _currentPreviewProcess;

        /// <summary>
        /// 初始化预览服务并加载应用程序设置
        /// </summary>
        /// <param name="settingsService">设置服务实例</param>
        public PreviewService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
        }

        /// <summary>
        /// 预览选项配置类，定义预览窗口的标题、尺寸等参数
        /// </summary>
        public class PreviewOptions {
            public string WindowTitle { get; set; } = "Wallpaper Preview";
            public int Width { get; set; } = 1920;
            public int Height { get; set; } = 1080;
            public string WindowId { get; set; } = "Wallpaper #1";
            public bool AutoClose { get; set; } = true;
        }

        /// <summary>
        /// 启动壁纸预览，会先停止已有的预览进程
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        /// <param name="options">预览选项，为 null 时使用默认配置</param>
        /// <returns>预览进程成功启动返回 true，否则返回 false</returns>
        /// <exception cref="InvalidOperationException">Wallpaper Engine 路径未设置或不存在</exception>
        /// <exception cref="FileNotFoundException">壁纸的 project.json 文件不存在</exception>
        public bool PreviewWallpaper(WallpaperItem wallpaper, PreviewOptions? options = null)
        {
            if (wallpaper == null) return false;

            options ??= new PreviewOptions();

            Log.Information("启动壁纸预览: {FolderPath}", wallpaper.FolderPath);
            if (string.IsNullOrEmpty(_settings.WallpaperEnginePath) ||
                !File.Exists(_settings.WallpaperEnginePath)) {
                throw new InvalidOperationException("Wallpaper Engine路径未设置或不存在");
            }

            // 检查壁纸文件
            var projectJsonPath = Path.Combine(wallpaper.FolderPath, "project.json");
            if (!File.Exists(projectJsonPath)) {
                throw new FileNotFoundException($"找不到project.json文件: {projectJsonPath}");
            }

            try {
                // 停止现有的预览
                StopPreview();

                // 构建命令参数
                var arguments = BuildPreviewArguments(projectJsonPath, options);

                // 启动预览进程
                var processStartInfo = new ProcessStartInfo {
                    FileName = _settings.WallpaperEnginePath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                _currentPreviewProcess = Process.Start(processStartInfo);


                return _currentPreviewProcess != null && !_currentPreviewProcess.HasExited;
            } catch (Exception ex) {
                Log.Error("启动预览失败: {Error}", ex.Message);
                throw new Exception($"启动预览失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 构建 Wallpaper Engine 预览命令行参数
        /// </summary>
        /// <param name="projectJsonPath">壁纸 project.json 文件路径</param>
        /// <param name="options">预览选项</param>
        /// <returns>拼接后的命令行参数字符串</returns>
        private string BuildPreviewArguments(string projectJsonPath, PreviewOptions options)
        {
            var args = new List<string>
        {
            "-control openWallpaper",
            $"-file \"{projectJsonPath}\"",
            $"-playInWindow \"{options.WindowId}\"",
            $"-width {options.Width}",
            $"-height {options.Height}",
            "-paused false"  // 自动开始播放
        };

            return string.Join(" ", args);
        }

        /// <summary>
        /// 停止当前正在运行的预览进程，先尝试优雅关闭，超时后强制终止
        /// </summary>
        public void StopPreview()
        {
            try {
                Log.Debug("停止壁纸预览");
                if (_currentPreviewProcess != null && !_currentPreviewProcess.HasExited) {
                    // 尝试优雅关闭
                    _currentPreviewProcess.CloseMainWindow();

                    if (!_currentPreviewProcess.WaitForExit(2000)) {
                        _currentPreviewProcess.Kill();
                    }
                }
            } catch (Exception ex) {
                Log.Warning("停止预览进程时出错: {Error}", ex.Message);
            } finally {
                _currentPreviewProcess?.Dispose();
                _currentPreviewProcess = null;
            }
        }

        /// <summary>
        /// 检查预览进程是否正在运行
        /// </summary>
        /// <returns>预览进程正在运行返回 true，否则返回 false</returns>
        public bool IsPreviewRunning()
        {
            return _currentPreviewProcess != null && !_currentPreviewProcess.HasExited;
        }
    }
}