using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class MainViewModel {
        [RelayCommand]
        private async Task FullScanWallpapers()
        {
           await ScanWallpapers(false);
        }

        [RelayCommand]
        private async Task IncrementalScanWallpapers()
        {
            await ScanWallpapers(true);
        }

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

        private async Task DoScanWallpapers(string folderPath, bool isIncrement)
        {
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
                LoadScanHistory();
                await LoadWallpapersAsync();
            }
        }

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

        [RelayCommand]
        private async Task CancelScan()
        {
            bool confirmed = await MaterialDialogService.ShowConfirmationAsync("确定要取消当前扫描吗？", "取消扫描");
            if (confirmed) {
                _scanner.CancelScan();
            }
        }

        private void CheckLastScanTime()
        {
            if (Properties.Settings.Default.LastScanTime != DateTime.MinValue) {
                LastScanTime = $"最后扫描: {Properties.Settings.Default.LastScanTime:yyyy-MM-dd HH:mm:ss}";
            }
        }

        private string GetLastUsedFolder()
        {
            return Properties.Settings.Default.LastScanFolder ??
                   Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        [RelayCommand]
        private void LoadScanHistory()
        {
            var history = _dbManager.GetScanHistory();
            ScanHistory.Clear();
            foreach (var item in history) {
                ScanHistory.Add(item);
            }
        }

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
