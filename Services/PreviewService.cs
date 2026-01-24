using System.Diagnostics;
using System.IO;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    public class PreviewService {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;
        private Process _currentPreviewProcess;

        public PreviewService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
        }

        public class PreviewOptions {
            public string WindowTitle { get; set; } = "Wallpaper Preview";
            public int Width { get; set; } = 1920;
            public int Height { get; set; } = 1080;
            public string WindowId { get; set; } = "Wallpaper #1";
            public bool AutoClose { get; set; } = true;
        }

        public bool PreviewWallpaper(WallpaperItem wallpaper, PreviewOptions options = null)
        {
            if (wallpaper == null) return false;

            options ??= new PreviewOptions();

            // 检查Wallpaper Engine路径
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
                throw new Exception($"启动预览失败: {ex.Message}", ex);
            }
        }

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

        public void StopPreview()
        {
            try {
                if (_currentPreviewProcess != null && !_currentPreviewProcess.HasExited) {
                    // 尝试优雅关闭
                    _currentPreviewProcess.CloseMainWindow();

                    if (!_currentPreviewProcess.WaitForExit(2000)) {
                        _currentPreviewProcess.Kill();
                    }
                }
            } catch (Exception ex) {
                Debug.WriteLine($"停止预览进程时出错: {ex.Message}");
            } finally {
                _currentPreviewProcess?.Dispose();
                _currentPreviewProcess = null;
            }
        }

        public bool IsPreviewRunning()
        {
            return _currentPreviewProcess != null && !_currentPreviewProcess.HasExited;
        }
    }
}