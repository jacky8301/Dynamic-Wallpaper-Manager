using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Services
{
    public class IncrementalScanner
    {
        private readonly DatabaseManager _dbManager;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isCancelled = false;

        public class ScanStatistics
        {
            public int TotalFolders { get; set; }
            public int ScannedFolders { get; set; }
            public int NewWallpapers { get; set; }
            public int UpdatedWallpapers { get; set; }
            public int SkippedFolders { get; set; }
            public long DurationMs { get; set; }
        }

        public IncrementalScanner(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Cancel()
        {
            _isCancelled = true;
            _cancellationTokenSource.Cancel();
        }

        public async Task<ScanStatistics> ScanIncrementallyAsync(
            string rootFolderPath,
            IProgress<ScanProgress> progress = null)
        {
            var stopwatch = Stopwatch.StartNew();
            var stats = new ScanStatistics();
            _isCancelled = false;

            try
            {
                if (!Directory.Exists(rootFolderPath))
                    throw new DirectoryNotFoundException($"目录不存在: {rootFolderPath}");

                progress?.Report(new ScanProgress
                {
                    Status = "正在搜索壁纸文件夹..."
                });

                // 获取所有壁纸文件夹
                var wallpaperFolders = GetWallpaperFolders(rootFolderPath);
                stats.TotalFolders = wallpaperFolders.Count;

                progress?.Report(new ScanProgress
                {
                    Status = $"找到 {stats.TotalFolders} 个壁纸文件夹，开始增量扫描..."
                });

                int processed = 0;
                foreach (var folder in wallpaperFolders)
                {
                    if (_isCancelled)
                    {
                        progress?.Report(new ScanProgress
                        {
                            Status = "扫描已被用户取消"
                        });
                        break;
                    }

                    processed++;
                    stats.ScannedFolders = processed;

                    // 检查是否需要更新
                    var needsUpdate = await CheckIfNeedsUpdateAsync(folder);

                    if (!needsUpdate)
                    {
                        stats.SkippedFolders++;
                        progress?.Report(new ScanProgress
                        {
                            Percentage = processed * 100 / Math.Max(1, stats.TotalFolders),
                            ProcessedCount = processed,
                            TotalCount = stats.TotalFolders,
                            CurrentFolder = folder,
                            Status = $"跳过: {Path.GetFileName(folder)}"
                        });
                        continue;
                    }

                    // 处理文件夹
                    try
                    {
                        var wallpaper = await ProcessWallpaperFolderAsync(folder);
                        if (wallpaper != null)
                        {
                            // 检查是新增还是更新
                            var existingWallpaper = _dbManager.GetWallpaperByFolderPath(folder);
                            if (existingWallpaper == null)
                            {
                                stats.NewWallpapers++;
                                _dbManager.SaveWallpaper(wallpaper);
                            }
                            else
                            {
                                stats.UpdatedWallpapers++;
                                // 保留收藏状态
                                wallpaper.IsFavorite = existingWallpaper.IsFavorite;
                                wallpaper.FavoritedDate = existingWallpaper.FavoritedDate;
                                _dbManager.SaveWallpaper(wallpaper);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"处理壁纸文件夹失败 {folder}: {ex.Message}");
                    }

                    progress?.Report(new ScanProgress
                    {
                        Percentage = processed * 100 / Math.Max(1, stats.TotalFolders),
                        ProcessedCount = processed,
                        TotalCount = stats.TotalFolders,
                        CurrentFolder = folder,
                        Status = $"处理: {Path.GetFileName(folder)}"
                    });

                    await Task.Delay(10, _cancellationTokenSource.Token);
                }

                // 清理不存在的壁纸
                //await CleanupDeletedWallpapersAsync(wallpaperFolders);

                stopwatch.Stop();
                stats.DurationMs = stopwatch.ElapsedMilliseconds;

                // 保存扫描记录
                _dbManager.SaveScanRecord(
                    rootFolderPath,
                    stats.TotalFolders,
                    stats.NewWallpapers,
                    stats.DurationMs);

                progress?.Report(new ScanProgress
                {
                    Status = $"增量扫描完成！新增: {stats.NewWallpapers}, 更新: {stats.UpdatedWallpapers}, 跳过: {stats.SkippedFolders}"
                });

                return stats;
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                progress?.Report(new ScanProgress { Status = "扫描已被取消" });
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                stats.DurationMs = stopwatch.ElapsedMilliseconds;
                _dbManager.SaveScanRecord(
                    rootFolderPath, 0, 0, stats.DurationMs, $"Error: {ex.Message}");
                throw;
            }
        }

        private List<string> GetWallpaperFolders(string rootPath)
        {
            var folders = new List<string>();

            try
            {
                // 获取所有子文件夹
                var allFolders = Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories);

                foreach (var folder in allFolders)
                {
                    var projectFile = Path.Combine(folder, "project.json");
                    if (File.Exists(projectFile))
                    {
                        folders.Add(folder);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"获取文件夹列表失败: {ex.Message}");
            }

            return folders;
        }

        private async Task<bool> CheckIfNeedsUpdateAsync(string folderPath)
        {
            try
            {
                var projectFile = Path.Combine(folderPath, "project.json");
                if (!File.Exists(projectFile))
                    return false;

                // 获取文件夹的最后修改时间
                var folderInfo = new DirectoryInfo(folderPath);
                var lastModified = folderInfo.LastWriteTime;

                // 获取主文件信息
                var jsonContent = await File.ReadAllTextAsync(projectFile);
                var project = Newtonsoft.Json.JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);
                if (project == null) return true;

                var mainFile = Path.Combine(folderPath, project.File);
                if (!File.Exists(mainFile)) return true;

                var fileInfo = new FileInfo(mainFile);
                var fileSize = fileInfo.Length;

                // 检查数据库中的记录
                return _dbManager.NeedsUpdate(folderPath, lastModified, fileSize);
            }
            catch
            {
                // 如果发生错误，强制重新扫描
                return true;
            }
        }

        private async Task<WallpaperItem> ProcessWallpaperFolderAsync(string folderPath)
        {
            try
            {
                var projectFile = Path.Combine(folderPath, "project.json");
                if (!File.Exists(projectFile))
                    return null;

                var jsonContent = await File.ReadAllTextAsync(projectFile);
                var project = Newtonsoft.Json.JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);

                if (project == null) return null;

                // 验证必要文件
                var previewPath = Path.Combine(folderPath, project.Preview);
                if (!File.Exists(previewPath))
                {
                    // 尝试查找常见的预览图名称
                    var commonPreviews = new[] { "preview.jpg", "preview.png", "thumbnail.jpg", "thumb.jpg" };
                    foreach (var commonPreview in commonPreviews)
                    {
                        var altPreviewPath = Path.Combine(folderPath, commonPreview);
                        if (File.Exists(altPreviewPath))
                        {
                            project.Preview = commonPreview;
                            previewPath = altPreviewPath;
                            break;
                        }
                    }

                    if (!File.Exists(previewPath))
                        return null;
                }

                var contentPath = Path.Combine(folderPath, project.File);
                if (!File.Exists(contentPath))
                    return null;

                // 自动分类
                var category = "未分类";
                if (project.Tags != null && project.Tags.Count > 0)
                {
                    var tagCategories = new Dictionary<string, string[]>
                    {
                        { "自然", new[] { "nature", "自然", "风景", "landscape" } },
                        { "抽象", new[] { "abstract", "抽象", "艺术", "art" } },
                        { "游戏", new[] { "game", "游戏", "gaming" } },
                        { "动漫", new[] { "anime", "动漫", "动画" } },
                        { "科幻", new[] { "sci-fi", "科幻", "space" } },
                    };

                    foreach (var tag in project.Tags)
                    {
                        foreach (var cat in tagCategories)
                        {
                            if (cat.Value.Any(t => tag.ToLower().Contains(t.ToLower())))
                            {
                                category = cat.Key;
                                break;
                            }
                        }
                        if (category != "未分类") break;
                    }
                }

                return new WallpaperItem
                {
                    FolderPath = folderPath,
                    Project = project,
                    Category = category,
                    AddedDate = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"处理壁纸文件夹错误 {folderPath}: {ex.Message}");
                return null;
            }
        }

        //    private async Task CleanupDeletedWallpapersAsync(List<string> existingFolders)
        //    {
        //        try
        //        {
        //            // 获取数据库中所有的壁纸路径
        //            var command = m_connection.CreateCommand();
        //            command.CommandText = "SELECT Id, FolderPath FROM Wallpapers";

        //            var wallpapersToDelete = new List<string>();
        //            using var reader = command.ExecuteReader();

        //            while (reader.Read())
        //            {
        //                var folderPath = reader["FolderPath"].ToString();
        //                if (!Directory.Exists(folderPath) && !existingFolders.Contains(folderPath))
        //                {
        //                    wallpapersToDelete.Add(reader["Id"].ToString());
        //                }
        //            }

        //            // 删除不存在的壁纸
        //            foreach (var id in wallpapersToDelete)
        //            {
        //                var deleteCommand = m_connection.CreateCommand();
        //                deleteCommand.CommandText = "DELETE FROM Wallpapers WHERE Id = @id";
        //                deleteCommand.Parameters.AddWithValue("@id", id);
        //                deleteCommand.ExecuteNonQuery();
        //            }

        //            if (wallpapersToDelete.Count > 0)
        //            {
        //                Debug.WriteLine($"清理了 {wallpapersToDelete.Count} 个不存在的壁纸记录");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"清理壁纸记录失败: {ex.Message}");
        //        }
        //    }
        //}
    }
}