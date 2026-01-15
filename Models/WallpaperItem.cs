using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WallpaperEngine.Models
{
    public partial class WallpaperItem : ObservableObject
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FolderPath { get; set; } = string.Empty;
        public string FolderName => Path.GetFileName(FolderPath);
        public WallpaperProject Project { get; set; } = new WallpaperProject();
        //public bool IsFavorite { get; set; }
        public string Category { get; set; } = "未分类";
        public DateTime AddedDate { get; set; } = DateTime.Now;
        public string PreviewImagePath => Path.Combine(FolderPath, Project.Preview);
        public string ContentPath => Path.Combine(FolderPath, Project.File);
        public bool IsSelected { get; set; }
        //public string FavoritedDate { get; set; } = DateTime.Now.ToString("O");
        public string LastUpdated { get; set; } = DateTime.Now.ToString("O");
        // 一个便利属性，用于判断是否为新添加的壁纸（非数据库中存在）
        [ObservableProperty]
        private bool _isFavorite;
        [ObservableProperty]
        private DateTime _favoritedDate;
        public bool IsNewlyAdded { get; set; } = false;
        [ObservableProperty]
        private DateTime _lastScanned;

        [ObservableProperty]
        private DateTime _folderLastModified;
    }
}