using System;
using System.Collections.Generic;
using System.Linq;

namespace WallpaperEngine.Common {
    /// <summary>
    /// 提供文件类型验证和帮助功能
    /// </summary>
    public static class FileTypeHelper {
        /// <summary>
        /// 支持的图片文件扩展名（不区分大小写）
        /// </summary>
        public static readonly HashSet<string> SupportedImageExtensions = new(StringComparer.OrdinalIgnoreCase) {
            ".jpg", ".jpeg", ".png", ".bmp", ".webp"
        };

        /// <summary>
        /// 图片文件筛选器字符串，用于 OpenFileDialog.Filter 属性
        /// </summary>
        public static string ImageFileFilter => "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.webp|所有文件|*.*";

        /// <summary>
        /// 带通配符的图片文件扩展名列表（例如 "*.jpg", "*.jpeg"）
        /// </summary>
        public static IEnumerable<string> SupportedImageFileFilters =>
            SupportedImageExtensions.Select(ext => "*" + ext);

        /// <summary>
        /// 检查文件扩展名是否为支持的图片格式
        /// </summary>
        /// <param name="extension">文件扩展名，可带或不带点号</param>
        /// <returns>是否为支持的图片格式</returns>
        public static bool IsImageFile(string extension) {
            if (string.IsNullOrEmpty(extension)) {
                return false;
            }

            // 确保扩展名以点号开头
            if (!extension.StartsWith(".")) {
                extension = "." + extension;
            }

            return SupportedImageExtensions.Contains(extension);
        }

        /// <summary>
        /// 检查文件路径是否为支持的图片文件
        /// </summary>
        /// <param name="filePath">文件完整路径</param>
        /// <returns>是否为支持的图片文件</returns>
        public static bool IsImageFilePath(string filePath) {
            if (string.IsNullOrEmpty(filePath)) {
                return false;
            }

            var extension = System.IO.Path.GetExtension(filePath);
            return IsImageFile(extension);
        }
    }
}