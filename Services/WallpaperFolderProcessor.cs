using Newtonsoft.Json;
using System.IO;
using WallpaperEngine.Models;
using WallpaperEngine.Data;
using Serilog;


namespace WallpaperEngine.Services
{
    class WallpaperFolderProcessor
    {
        public static async Task<WallpaperItem> Process(string folderPath, bool isIncrement, DatabaseManager dbManager)
        {
            try {
                if (!Path.Exists(folderPath)) {  //  目录不存在，是无效项，跳过，如果数据库中有记录，删除记录
                    dbManager.DeleteWallpaperByPath(folderPath);
                    return null;
                }
                var existingWallpaper = dbManager.GetWallpaperByFolderPath(folderPath);
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
