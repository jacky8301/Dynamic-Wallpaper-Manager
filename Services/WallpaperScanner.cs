using Newtonsoft.Json;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Services {
    public class WallpaperScanner {
        private readonly DatabaseManager _dbManager;
        private CancellationTokenSource _cancellationTokenSource;
        bool _isIncrementalScan = false;

        public WallpaperScanner(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void CancelScan()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task<bool> ScanWallpapersAsync(string rootFolderPath, bool inc, IProgress<ScanProgress> progress = null)
        {
            _isIncrementalScan = inc;
            if (!Directory.Exists(rootFolderPath)) {
                throw new DirectoryNotFoundException($"目录不存在: {rootFolderPath}");
            }

            if (_cancellationTokenSource.IsCancellationRequested) {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            var cancellationToken = _cancellationTokenSource.Token;

            try {
                progress?.Report(new ScanProgress { Status = "正在搜索壁纸文件夹..." });

                var validFolders = new List<string>();

                var wallpaperFolders = Directory.GetDirectories(rootFolderPath);
                foreach (var folder in wallpaperFolders) {
                    if (cancellationToken.IsCancellationRequested) {
                        return false;
                    }
                    validFolders.Add(folder);
                }


                int total = validFolders.Count;
                int processed = 0;
                int validCount = 0;

                progress?.Report(new ScanProgress { Status = $"找到 {total} 个壁纸文件夹，开始处理..." });

                foreach (var folder in validFolders) {
                    if (cancellationToken.IsCancellationRequested) return false;

                    try {
                        var wallpaper = await WallpaperFolderProcessor.Process(folder, _isIncrementalScan, _dbManager);
                        if (wallpaper != null) {
                            _dbManager.SaveWallpaper(wallpaper);
                            validCount++;
                        }

                        processed++;

                        progress?.Report(new ScanProgress {
                            Percentage = processed * 100 / Math.Max(1, total),
                            ProcessedCount = processed,
                            TotalCount = total,
                            CurrentFolder = folder,
                            Status = $"正在处理: {Path.GetFileName(folder)}"
                        });

                        await Task.Delay(50, cancellationToken);
                    } catch (Exception ex) {
                        System.Diagnostics.Debug.WriteLine($"处理壁纸文件夹失败 {folder}: {ex.Message}");
                    }
                }

                progress?.Report(new ScanProgress { Status = $"扫描完成，成功处理 {validCount} 个壁纸" });
                return true;
            } catch (OperationCanceledException) {
                progress?.Report(new ScanProgress { Status = "扫描已被取消" });
                return false;
            } catch (Exception ex) {
                progress?.Report(new ScanProgress { Status = $"扫描失败: {ex.Message}" });
                throw;
            }
        }

        
    }
}