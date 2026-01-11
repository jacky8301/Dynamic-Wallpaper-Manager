using System;
using System.IO;

namespace WallpaperEngine.Models
{
    public class WallpaperItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FolderPath { get; set; } = string.Empty;
        public string FolderName => Path.GetFileName(FolderPath);
        public WallpaperProject Project { get; set; } = new WallpaperProject();
        public bool IsFavorite { get; set; }
        public string Category { get; set; } = "未分类";
        public DateTime AddedDate { get; set; } = DateTime.Now;

        public string PreviewImagePath => Path.Combine(FolderPath, Project.Preview);
        public string ContentPath => Path.Combine(FolderPath, Project.File);
        public bool IsSelected { get; set; }
    }
}