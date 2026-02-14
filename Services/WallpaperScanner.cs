using Newtonsoft.Json;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;
using Serilog;

namespace WallpaperEngine.Services {
    public class WallpaperScanner {
        private readonly DatabaseManager _dbManager;
        private CancellationTokenSource _cancellationTokenSource;

        private static readonly Dictionary<string, string[]> TagCategories = new()
        {
            { "自然", new[] { "nature", "自然", "风景", "landscape" } },
            { "抽象", new[] { "abstract", "抽象", "艺术", "art" } },
            { "游戏", new[] { "game", "游戏", "gaming" } },
            { "动漫", new[] { "anime", "动漫", "动画" } },
            { "科幻", new[] { "sci-fi", "科幻", "space" } },
            { "风景", new[] { "scenery", "风景", "view" } },
            { "建筑", new[] { "architecture", "建筑", "building" } },
            { "动物", new[] { "animal", "动物", "pet" } }
        };

        public WallpaperScanner(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void CancelScan()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task<bool> ScanWallpapersAsync(string rootFolderPath, bool isIncremental, IProgress<ScanProgress> progress = null)
        {
            if (!Directory.Exists(rootFolderPath)) {
                throw new DirectoryNotFoundException($"目录不存在: {rootFolderPath}");
            }

            if (_cancellationTokenSource.IsCancellationRequested) {
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
            }

            var cancellationToken = _cancellationTokenSource.Token;

            try {
                progress?.Report(new ScanProgress { Status = "正在搜索壁纸文件夹..." });

                var wallpaperFolders = Directory.GetDirectories(rootFolderPath);
                int total = wallpaperFolders.Length;
                int processed = 0;
                int validCount = 0;

                progress?.Report(new ScanProgress { Status = $"找到 {total} 个壁纸文件夹，开始处理..." });

                foreach (var folder in wallpaperFolders) {
                    if (cancellationToken.IsCancellationRequested) return false;

                    try {
                        var wallpaper = await ProcessWallpaper(folder, isIncremental);
                        if (wallpaper != null) {
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

                        await Task.Yield();
                    } catch (Exception ex) {
                        Log.Warning($"处理壁纸文件夹失败 {folder}: {ex.Message}");
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

        public async Task<WallpaperItem> ProcessWallpaper(string folderPath, bool isIncremental)
        {
            try {
                if (!Path.Exists(folderPath)) {
                    _dbManager.DeleteWallpaperByPath(folderPath);
                    return null;
                }

                var existingWallpaper = _dbManager.GetWallpaperByFolderPath(folderPath);
                if (existingWallpaper != null && isIncremental) {
                    return existingWallpaper;
                }

                var wallpaperItem = new WallpaperItem {
                    FolderPath = folderPath,
                    AddedDate = DateTime.Now,
                    IsNewlyAdded = true
                };

                var projectFile = Path.Combine(folderPath, "project.json");
                if (!File.Exists(projectFile)) {
                    string defaultProjectPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "project.json");
                    File.Copy(defaultProjectPath, projectFile);
                }

                var jsonContent = await File.ReadAllTextAsync(projectFile);
                var project = JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);

                if (project == null) {
                    return wallpaperItem;
                }

                wallpaperItem.Project = project;
                wallpaperItem.Category = GetCategory(project);
                _dbManager.SaveWallpaper(wallpaperItem);
                return wallpaperItem;
            } catch (Exception ex) {
                Log.Fatal($"Error processing folder {folderPath}: {ex.Message}");
                return null;
            }
        }

        private static string GetCategory(WallpaperProject project)
        {
            if (project.Tags == null || project.Tags.Count == 0) {
                return "未分类";
            }

            foreach (var tag in project.Tags) {
                foreach (var cat in TagCategories) {
                    if (cat.Value.Any(t => tag.Contains(t, StringComparison.OrdinalIgnoreCase))) {
                        return cat.Key;
                    }
                }
            }

            return "未分类";
        }
    }

}
