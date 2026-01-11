using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Services
{
    public class WallpaperScanner
    {
        private readonly DatabaseManager _dbManager;
        private CancellationTokenSource _cancellationTokenSource;

        public WallpaperScanner(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void CancelScan()
        {
            _cancellationTokenSource.Cancel();
        }

        public async Task<bool> ScanWallpapersAsync(string rootFolderPath, IProgress<ScanProgress> progress = null)
        {
            if (!Directory.Exists(rootFolderPath))
            {
                throw new DirectoryNotFoundException($"目录不存在: {rootFolderPath}");
            }

            if (_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            var cancellationToken = _cancellationTokenSource.Token;

            try
            {
                progress?.Report(new ScanProgress { Status = "正在搜索壁纸文件夹..." });

                var wallpaperFolders = Directory.GetDirectories(rootFolderPath);
                var validFolders = new List<string>();

                foreach (var folder in wallpaperFolders)
                {
                    if (cancellationToken.IsCancellationRequested) return false;

                    var projectFile = Path.Combine(folder, "project.json");
                    if (File.Exists(projectFile))
                    {
                        validFolders.Add(folder);
                    }
                }

                int total = validFolders.Count;
                int processed = 0;
                int validCount = 0;

                progress?.Report(new ScanProgress { Status = $"找到 {total} 个壁纸文件夹，开始处理..." });

                foreach (var folder in validFolders)
                {
                    if (cancellationToken.IsCancellationRequested) return false;

                    try
                    {
                        var wallpaper = await ProcessWallpaperFolderAsync(folder);
                        if (wallpaper != null)
                        {
                            _dbManager.SaveWallpaper(wallpaper);
                            validCount++;
                        }

                        processed++;

                        progress?.Report(new ScanProgress
                        {
                            Percentage = processed * 100 / Math.Max(1, total),
                            ProcessedCount = processed,
                            TotalCount = total,
                            CurrentFolder = folder,
                            Status = $"正在处理: {Path.GetFileName(folder)}"
                        });

                        await Task.Delay(50, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"处理壁纸文件夹失败 {folder}: {ex.Message}");
                    }
                }

                progress?.Report(new ScanProgress { Status = $"扫描完成，成功处理 {validCount} 个壁纸" });
                return true;
            }
            catch (OperationCanceledException)
            {
                progress?.Report(new ScanProgress { Status = "扫描已被取消" });
                return false;
            }
            catch (Exception ex)
            {
                progress?.Report(new ScanProgress { Status = $"扫描失败: {ex.Message}" });
                throw;
            }
        }

        private async Task<WallpaperItem> ProcessWallpaperFolderAsync(string folderPath)
        {
            try
            {
                var projectFile = Path.Combine(folderPath, "project.json");
                if (!File.Exists(projectFile)) return null;

                var jsonContent = await File.ReadAllTextAsync(projectFile);
                var project = JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);

                if (project == null) return null;

                // 验证预览文件
                var previewPath = Path.Combine(folderPath, project.Preview);
                if (!File.Exists(previewPath))
                {
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

                    if (!File.Exists(previewPath)) return null;
                }

                // 验证内容文件
                var contentPath = Path.Combine(folderPath, project.File);
                if (!File.Exists(contentPath)) return null;

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
                        { "风景", new[] { "scenery", "风景", "view" } },
                        { "建筑", new[] { "architecture", "建筑", "building" } },
                        { "动物", new[] { "animal", "动物", "pet" } }
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
                System.Diagnostics.Debug.WriteLine($"处理壁纸文件夹错误 {folderPath}: {ex.Message}");
                return null;
            }
        }
    }
}