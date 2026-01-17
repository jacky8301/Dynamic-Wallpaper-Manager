using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
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

                var validFolders = new List<string>();

                var wallpaperFolders = Directory.GetDirectories(rootFolderPath);
                foreach (var folder in wallpaperFolders)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return false;
                    }
                    validFolders.Add(folder);
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
                if (!File.Exists(projectFile))
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string defaultProjectPath = Path.Combine(baseDirectory, "project.json");
                    File.Copy(defaultProjectPath, Path.Combine(folderPath, "project.json"));
                }

                var jsonContent = await File.ReadAllTextAsync(projectFile);
                var project = JsonConvert.DeserializeObject<WallpaperProject>(jsonContent);

                if (project == null)
                {
                    return new WallpaperItem
                    {
                        FolderPath = folderPath,
                        Project = null,
                        Category = "未分类",
                        AddedDate = DateTime.Now
                    };
                }

                // 验证预览文件
                bool isPreviewValid = false;
                var previewPath = Path.Combine(folderPath, project.Preview);
                if (!File.Exists(previewPath))
                {
                    var commonPreviews = new[] { "preview.jpg", "preview.png", "preview.gif", "preview.jpg.bak", "preview.gif.bak" };
                    foreach (var commonPreview in commonPreviews)
                    {
                        var altPreviewPath = Path.Combine(folderPath, commonPreview);
                        if (File.Exists(altPreviewPath))
                        {
                            isPreviewValid = true;
                            project.Preview = commonPreview;
                            previewPath = altPreviewPath;
                            if (previewPath.Contains(".bak"))
                            {
                                previewPath = previewPath.Replace(".bak", "");
                                // 恢复备份文件
                                File.Copy(altPreviewPath, previewPath, true);
                                File.Delete(altPreviewPath);
                                project.Preview = commonPreview.Replace(".bak", "");

                            }
                            break;
                        }
                    }
                }
                if (!isPreviewValid && !File.Exists(previewPath))
                {
                    string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string defaultPreviewPath = Path.Combine(baseDirectory, "preview.jpg");
                    project.Preview = "preview.jpg";
                    File.Copy(defaultPreviewPath, Path.Combine(folderPath, "preview.jpg"));
                }

                // 验证内容文件
                var contentPath = Path.Combine(folderPath, project.File);
                if (!File.Exists(contentPath))
                {
                    return new WallpaperItem
                    {
                        FolderPath = folderPath,
                        Project = project,
                        Category = "未分类",
                        AddedDate = DateTime.Now
                    };
                }

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

                // 1. 首先，尝试从数据库中查找现有记录，以文件夹路径作为关键标识[1](@ref)
                var existingWallpaper = _dbManager.GetWallpaperByFolderPath(folderPath);

                WallpaperItem wallpaperItem;

                if (existingWallpaper != null)
                {
                    // 2. 如果数据库中存在：将数据库中的记录作为基础
                    wallpaperItem = existingWallpaper;
                    // 标记这不是新添加的，因为我们只是重新扫描到了它
                    wallpaperItem.IsNewlyAdded = false;

                    // 重要：用最新扫描到的项目信息（如标题、描述）更新现有对象，
                    // 但保留之前已设置的收藏状态！
                    wallpaperItem.Project.Title = project.Title;
                    wallpaperItem.Project.Description = project.Description;
                    // ... 更新其他可能变化的属性，但 IsFavorite 和 FavoritedDate 保持不变！
                }
                else
                {
                    // 3. 如果数据库中不存在：创建新项
                    wallpaperItem = new WallpaperItem
                    {
                        FolderPath = folderPath,
                        Project = project,
                        Category = category, // 你的自动分类逻辑
                        AddedDate = DateTime.Now,
                        IsNewlyAdded = true // 标记为新添加
                                            // IsFavorite 和 FavoritedDate 使用默认值（false, MinValue）
                    };
                }

                _dbManager.SaveWallpaper(wallpaperItem);
                return wallpaperItem;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"处理壁纸文件夹错误 {folderPath}: {ex.Message}");
                return null;
            }
        }
    }
}