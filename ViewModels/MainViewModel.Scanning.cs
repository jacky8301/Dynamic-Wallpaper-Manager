using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的扫描部分，包含全量扫描、增量扫描、进度更新、错误处理和扫描历史管理
    /// </summary>
    public partial class MainViewModel {
        /// <summary>
        /// 全量扫描壁纸命令，扫描指定文件夹中的所有壁纸
        /// </summary>
        [RelayCommand]
        private async Task FullScanWallpapers()
        {
           await ScanWallpapers(false);
        }

        /// <summary>
        /// 增量扫描壁纸命令，仅扫描新增或变更的壁纸
        /// </summary>
        [RelayCommand]
        private async Task IncrementalScanWallpapers()
        {
            await ScanWallpapers(true);
        }

        /// <summary>
        /// 执行壁纸扫描，增量扫描时优先使用上次扫描路径，否则弹出文件夹选择对话框
        /// </summary>
        /// <param name="isIncrement">是否为增量扫描</param>
        private async Task ScanWallpapers(bool isIncrement)
        {
            if (IsScanning) {
                return;
            }

            try {
                string scanPath = string.Empty;
                bool hasLastScanPath = false;

                var lastScan = ScanHistory.FirstOrDefault();
                if (lastScan != null) {
                    if (Directory.Exists(lastScan.ScanPath)) {
                        hasLastScanPath = true;
                    }
                }

                if (hasLastScanPath && isIncrement) {
                    scanPath = lastScan.ScanPath;
                    await DoScanWallpapers(scanPath, true);
                } else {
                    var dialog = new FolderBrowserDialog {
                        Description = "选择壁纸文件夹根目录",
                        ShowNewFolderButton = false,
                        RootFolder = Environment.SpecialFolder.MyComputer,
                        SelectedPath = GetLastUsedFolder()
                    };
                    var result = dialog.ShowDialog();
                    if (result == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath)) {
                        await DoScanWallpapers(dialog.SelectedPath, isIncrement);
                    }

                }
            } catch (Exception ex) {
                await HandleScanError(ex);
            }
        }

        /// <summary>
        /// 执行实际的壁纸扫描操作，更新进度并在完成后保存扫描记录
        /// </summary>
        /// <param name="folderPath">要扫描的文件夹路径</param>
        /// <param name="isIncrement">是否为增量扫描</param>
        private async Task DoScanWallpapers(string folderPath, bool isIncrement)
        {
            Log.Information("开始{ScanType}扫描: {FolderPath}", isIncrement ? "增量" : "全量", folderPath);
            IsScanning = true;
            ScanStatus = "正在准备扫描...";
            CurrentScanFolder = folderPath;
            ScannedCount = 0;
            TotalCount = 0;
            NewFoundCount = 0;
            UpdatedCount = 0;
            SkippedCount = 0;
            try {
                ScanStatus = isIncrement ? "正在执行增量扫描..." : "正在执行全量扫描...";
                var progress = new Progress<ScanProgress>(UpdateProgress);
                var result = await _scanner.ScanWallpapersAsync(folderPath, isIncrement, progress);
                ScanStatus = result ? (isIncrement ? "增量扫描完成！" : "全量扫描完成！") : "扫描被取消";
            } catch (Exception ex) {
                await HandleScanError(ex);
            } finally {
                IsScanning = false;
                SelectedCategory = "所有分类";
                ShowFavoritesOnly = false;
                SearchText = string.Empty;
                _dbManager.SaveScanRecord(CurrentScanFolder, NewFoundCount, UpdatedCount, SkippedCount);
                Log.Information("扫描完成, 新增: {NewCount}, 更新: {UpdatedCount}, 跳过: {SkippedCount}", NewFoundCount, UpdatedCount, SkippedCount);
                LoadScanHistory();
                await LoadWallpapersAsync();
            }
        }

        /// <summary>
        /// 更新扫描进度信息到UI属性
        /// </summary>
        /// <param name="progress">扫描进度数据</param>
        private void UpdateProgress(ScanProgress progress)
        {
            ScanProgress = progress.Percentage;
            ScannedCount = progress.ProcessedCount;
            TotalCount = progress.TotalCount;
            NewFoundCount = progress.NewCount;
            UpdatedCount = progress.UpdatedCount;
            SkippedCount = progress.SkippedCount;

            if (progress.CurrentFolder != null) {
                var folderName = Path.GetFileName(progress.CurrentFolder);
                ScanStatus = $"正在扫描: {folderName} ({ScannedCount}/{TotalCount})";
            }

            if (progress.Status != null) {
                ScanStatus = progress.Status;
            }
        }

        /// <summary>
        /// 处理扫描过程中的异常，根据异常类型显示对应的错误消息
        /// </summary>
        /// <param name="ex">捕获的异常</param>
        private async Task HandleScanError(Exception ex)
        {
            ScanStatus = "扫描过程中发生错误";

            Log.Error($"扫描错误: {ex.Message}");

            var errorMessage = ex switch {
                UnauthorizedAccessException => "没有权限访问指定的文件夹。请以管理员身份运行或选择其他文件夹。",
                DirectoryNotFoundException => "指定的文件夹不存在或已被移动。",
                IOException => "文件访问错误，可能是文件正在被其他程序使用。",
                _ => $"扫描壁纸时发生错误:\n{ex.Message}"
            };

            await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                Message = errorMessage,
                Title = "扫描错误",
                ShowCancelButton = false,
                DialogType = DialogType.Error
            });
        }

        /// <summary>
        /// 取消扫描命令，弹出确认对话框后取消当前扫描任务
        /// </summary>
        [RelayCommand]
        private async Task CancelScan()
        {
            bool confirmed = await MaterialDialogService.ShowConfirmationAsync("确定要取消当前扫描吗？", "取消扫描");
            if (confirmed) {
                Log.Information("用户取消扫描");
                _scanner.CancelScan();
            }
        }

        /// <summary>
        /// 检查并更新上次扫描时间的显示文本
        /// </summary>
        private void CheckLastScanTime()
        {
            if (Properties.Settings.Default.LastScanTime != DateTime.MinValue) {
                LastScanTime = $"最后扫描: {Properties.Settings.Default.LastScanTime:yyyy-MM-dd HH:mm:ss}";
            }
        }

        /// <summary>
        /// 获取上次使用的扫描文件夹路径，默认返回"我的图片"目录
        /// </summary>
        /// <returns>上次使用的文件夹路径</returns>
        private string GetLastUsedFolder()
        {
            return Properties.Settings.Default.LastScanFolder ??
                   Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        /// <summary>
        /// 加载扫描历史记录命令，从数据库读取并更新历史列表
        /// </summary>
        [RelayCommand]
        private void LoadScanHistory()
        {
            var history = _dbManager.GetScanHistory();
            ScanHistory.Clear();
            foreach (var item in history) {
                ScanHistory.Add(item);
            }
        }

        /// <summary>
        /// 清除扫描历史记录命令，弹出确认对话框后清除所有记录
        /// </summary>
        [RelayCommand]
        private async Task ClearScanHistory()
        {
            bool result = await MaterialDialogService.ShowConfirmationAsync("确定要清除所有扫描历史记录吗？","确认清除");
            if (result) {
                _dbManager.ClearScanHistory();
                LoadScanHistory();
            }
        }
    }
}
