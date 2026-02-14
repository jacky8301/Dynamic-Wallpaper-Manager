using Newtonsoft.Json;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;
using Serilog;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 壁纸扫描器，扫描指定文件夹中的壁纸目录，读取 project.json 并根据标签自动分类，支持增量扫描
    /// </summary>
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

        /// <summary>
        /// 初始化壁纸扫描器
        /// </summary>
        /// <param name="dbManager">数据库管理器实例，用于持久化壁纸数据</param>
        public WallpaperScanner(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 取消正在进行的扫描操作
        /// </summary>
        public void CancelScan()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// 异步扫描指定根目录下的所有壁纸文件夹，解析 project.json 并保存到数据库
        /// </summary>
        /// <param name="rootFolderPath">壁纸根目录路径</param>
        /// <param name="isIncremental">是否为增量扫描，增量模式下跳过已存在且未变化的壁纸</param>
        /// <param name="progress">扫描进度报告回调</param>
        /// <returns>扫描正常完成返回 true，被取消返回 false</returns>
        /// <exception cref="DirectoryNotFoundException">根目录不存在</exception>
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

                return await Task.Run(async () => {
                    var wallpaperFolders = Directory.GetDirectories(rootFolderPath)
                        .Where(f => long.TryParse(Path.GetFileName(f), out _))
                        .ToArray();
                    int total = wallpaperFolders.Length;
                    int processed = 0;
                    int newCount = 0;
                    int updatedCount = 0;
                    int skippedCount = 0;

                    progress?.Report(new ScanProgress { Status = $"找到 {total} 个壁纸文件夹，开始处理..." });

                    foreach (var folder in wallpaperFolders) {
                        if (cancellationToken.IsCancellationRequested) return false;

                        try {
                            var resultType = await ProcessWallpaper(folder, isIncremental);
                            switch (resultType) {
                                case ScanResultType.New:
                                    newCount++;
                                    break;
                                case ScanResultType.Updated:
                                    updatedCount++;
                                    break;
                                case ScanResultType.Skipped:
                                    skippedCount++;
                                    break;
                            }
                            processed++;

                            progress?.Report(new ScanProgress {
                                Percentage = processed * 100 / Math.Max(1, total),
                                ProcessedCount = processed,
                                TotalCount = total,
                                CurrentFolder = folder,
                                Status = $"正在处理: {Path.GetFileName(folder)}",
                                NewCount = newCount,
                                UpdatedCount = updatedCount,
                                SkippedCount = skippedCount
                            });
                        } catch (Exception ex) {
                            Log.Warning($"处理壁纸文件夹失败 {folder}: {ex.Message}");
                        }
                    }

                    progress?.Report(new ScanProgress {
                        Status = $"扫描完成，新增 {newCount}，更新 {updatedCount}，跳过 {skippedCount}",
                        NewCount = newCount,
                        UpdatedCount = updatedCount,
                        SkippedCount = skippedCount
                    });
                    return true;
                }, cancellationToken);
            } catch (OperationCanceledException) {
                progress?.Report(new ScanProgress { Status = "扫描已被取消" });
                return false;
            } catch (Exception ex) {
                progress?.Report(new ScanProgress { Status = $"扫描失败: {ex.Message}" });
                throw;
            }
        }

        /// <summary>
        /// 处理单个壁纸文件夹，读取 project.json 并推断分类后保存到数据库
        /// </summary>
        /// <param name="folderPath">壁纸文件夹路径</param>
        /// <param name="isIncremental">是否为增量模式</param>
        /// <returns>处理结果类型：新增、更新或跳过</returns>
        public async Task<ScanResultType> ProcessWallpaper(string folderPath, bool isIncremental)
        {
            try {
                if (!Path.Exists(folderPath)) {
                    _dbManager.DeleteWallpaperByPath(folderPath);
                    return ScanResultType.Skipped;
                }

                var existingWallpaper = _dbManager.GetWallpaperByFolderPath(folderPath);
                if (existingWallpaper != null && isIncremental) {
                    return ScanResultType.Skipped;
                }

                bool isNew = existingWallpaper == null;

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
                    return ScanResultType.Skipped;
                }

                wallpaperItem.Project = project;
                // 优先使用 project.json 中的分类，否则根据标签推断
                if (string.IsNullOrEmpty(project.Category) || project.Category == "未分类") {
                    var detectedCategory = GetCategory(project);
                    project.Category = detectedCategory;
                }
                wallpaperItem.Category = project.Category;
                _dbManager.SaveWallpaper(wallpaperItem);
                return isNew ? ScanResultType.New : ScanResultType.Updated;
            } catch (Exception ex) {
                Log.Fatal($"Error processing folder {folderPath}: {ex.Message}");
                return ScanResultType.Skipped;
            }
        }

        /// <summary>
        /// 根据壁纸项目的标签自动推断分类，匹配预定义的标签-分类映射表
        /// </summary>
        /// <param name="project">壁纸项目信息</param>
        /// <returns>推断出的分类名称，无法匹配时返回"未分类"</returns>
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
