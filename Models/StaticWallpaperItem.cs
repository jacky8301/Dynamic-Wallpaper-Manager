using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 静态壁纸数据模型，表示一张独立的图片壁纸
    /// </summary>
    public partial class StaticWallpaperItem : ObservableObject {
        /// <summary>唯一标识符</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>图片文件完整路径</summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>文件名（含扩展名）</summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>文件大小（字节）</summary>
        public long FileSize { get; set; }

        /// <summary>图片像素宽度</summary>
        public int Width { get; set; }

        /// <summary>图片像素高度</summary>
        public int Height { get; set; }

        /// <summary>添加日期</summary>
        public DateTime AddedDate { get; set; } = DateTime.Now;

        /// <summary>是否被选中（UI 状态）</summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>分辨率显示文本</summary>
        public string Resolution => Width > 0 && Height > 0 ? $"{Width} x {Height}" : "未知";

        /// <summary>格式化的文件大小</summary>
        public string FormattedFileSize {
            get {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
                if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024.0):F1} MB";
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";
            }
        }
    }
}
