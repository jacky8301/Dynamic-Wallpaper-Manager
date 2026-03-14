using Newtonsoft.Json;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;
using Serilog;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 壁纸扫描器，扫描指定文件夹中的壁纸目录，读取 project.json，支持增量扫描
    /// </summary>
    public class WallpaperScanner {
        private readonly DatabaseManager _dbManager;
        private CancellationTokenSource _cancellationTokenSource;


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
            Log.Information("取消壁纸扫描");
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
        public async Task<bool> ScanWallpapersAsync(string rootFolderPath, bool isIncremental, IProgress<ScanProgress>? progress = null)
        {
            Log.Information("开始扫描壁纸, 路径: {RootPath}, 增量: {IsIncremental}", rootFolderPath, isIncremental);

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
        /// 处理单个壁纸文件夹，读取 project.json 并保存到数据库
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

                // 处理壁纸ID：优先使用project.json中的wallpaperId，否则使用现有ID或生成新ID
                string wallpaperId = string.Empty;
                if (!string.IsNullOrEmpty(project.WallpaperId))
                {
                    wallpaperId = project.WallpaperId;
                }
                else if (existingWallpaper != null)
                {
                    wallpaperId = existingWallpaper.Id;
                    project.WallpaperId = wallpaperId;
                    // 将ID写回project.json文件
                    await WriteWallpaperIdToProjectFile(projectFile, project);
                }
                else
                {
                    wallpaperId = Guid.NewGuid().ToString();
                    project.WallpaperId = wallpaperId;
                    await WriteWallpaperIdToProjectFile(projectFile, project);
                }

                wallpaperItem.Id = wallpaperId;

                // 如果project.json中的ID与现有ID不同，需要删除旧记录
                if (existingWallpaper != null && existingWallpaper.Id != wallpaperId)
                {
                    _dbManager.DeleteWallpaperByPath(folderPath);
                }

                // 使用 project.json 中的分类，如果为空或"未分类"，则设置为"未分类"
                if (string.IsNullOrEmpty(project.Category) || project.Category == "未分类") {
                    project.Category = "未分类";
                    wallpaperItem.CategoryId = CategoryConstants.UNCATEGORIZED_ID;
                    wallpaperItem.Category = "未分类";
                }
                else
                {
                    // 将分类名称转换为ID
                    var categoryId = _dbManager.GetCategoryIdByName(project.Category);
                    wallpaperItem.CategoryId = categoryId != null ? categoryId : CategoryConstants.UNCATEGORIZED_ID;
                    wallpaperItem.Category = project.Category;
                }
                _dbManager.SaveWallpaper(wallpaperItem);
                return isNew ? ScanResultType.New : ScanResultType.Updated;
            } catch (Exception ex) {
                Log.Fatal($"Error processing folder {folderPath}: {ex.Message}");
                return ScanResultType.Skipped;
            }
        }

        /// <summary>
        /// 将壁纸ID写回project.json文件
        /// </summary>
        /// <param name="projectFile">project.json文件路径</param>
        /// <param name="project">壁纸项目对象</param>
        private async Task WriteWallpaperIdToProjectFile(string projectFile, WallpaperProject project)
        {
            try
            {
                var json = JsonConvert.SerializeObject(project, Formatting.Indented);
                await File.WriteAllTextAsync(projectFile, json);
                Log.Debug("已更新壁纸ID到文件: {ProjectFile}", projectFile);
            }
            catch (Exception ex)
            {
                Log.Warning("更新project.json文件失败 {ProjectFile}: {Message}", projectFile, ex.Message);
            }
        }

    }

}
