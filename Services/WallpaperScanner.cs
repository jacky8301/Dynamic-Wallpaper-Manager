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
                        var wallpaper = await ProcessWallpaper(folder, _isIncrementalScan);
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
        public async Task<WallpaperItem> ProcessWallpaper(string folderPath, bool isIncrement)
        {
            try {
                if (!Path.Exists(folderPath)) {  //  目录不存在，是无效项，跳过，如果数据库中有记录，删除记录
                    _dbManager.DeleteWallpaperByPath(folderPath);
                    return null;
                }
                var existingWallpaper = _dbManager.GetWallpaperByFolderPath(folderPath);
                if (existingWallpaper == null || !isIncrement) {  // 数据库中没有记录，说明是新添加的目录
                    WallpaperItem wallpaperItem = new WallpaperItem {
                        FolderPath = folderPath,
                        AddedDate = DateTime.Now,
                        IsNewlyAdded = true
                    };
                    // 尝试解析 project.json，如果不存在，复制默认的 project.json 模板到该目录
                    var projectFile = Path.Combine(folderPath, "project.json");
                    if (!File.Exists(projectFile)) {
                        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                        string defaultProjectPath = Path.Combine(baseDirectory, "project.json");
                        File.Copy(defaultProjectPath, Path.Combine(folderPath, "project.json"));
                    }

                    var jsonContent = await File.ReadAllTextAsync(projectFile);
                    var project = JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);

                    if (project == null) {
                        return wallpaperItem;
                    }
                    wallpaperItem.Project = project;
                    string category = GetCategory(project);  // 自动分类
                    return wallpaperItem;
                } else {
                    return existingWallpaper;
                }
            } catch (Exception ex) {
                Log.Fatal($"Error processing folder {folderPath}: {ex.Message}");
                return null;
            }
        }

        private static string GetCategory(WallpaperProject project)
        {
            var category = "未分类";
            if (project.Tags != null && project.Tags.Count > 0) {
                var tagCategories = new Dictionary<string, string[]>
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

                foreach (var tag in project.Tags) {
                    foreach (var cat in tagCategories) {
                        if (cat.Value.Any(t => tag.ToLower().Contains(t.ToLower()))) {
                            category = cat.Key;
                            break;
                        }
                    }
                    if (category != "未分类") break;
                }
            }
            return category;
        }
    }

}
