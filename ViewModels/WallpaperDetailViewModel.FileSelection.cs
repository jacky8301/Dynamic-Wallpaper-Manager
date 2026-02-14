namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸详情视图模型的文件选择部分，包含预览图和内容文件的设置与验证逻辑
    /// </summary>
    public partial class WallpaperDetailViewModel {
        /// <summary>
        /// 设置预览图文件名，并保存到project.json和数据库
        /// </summary>
        /// <param name="fileName">预览图文件名</param>
        private async Task SetPreviewFileName(string? fileName)
        {
            if (CurrentWallpaper == null || CurrentWallpaper.Project.Preview == fileName) {
                return;
            }
            CurrentWallpaper.Project.Preview = fileName;
            PreviewFileName = fileName;
            try {
                // 保存到project.json
                await SaveToProjectJsonAsync();
                // 更新数据库
                await UpdateDatabaseAsync();
            } catch (Exception ex) {
                ShowErrorMessage($"保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置壁纸内容文件名，并保存到project.json和数据库
        /// </summary>
        /// <param name="fileName">内容文件名</param>
        private async Task SetContentFileName(string? fileName)
        {
            if (CurrentWallpaper == null ||
                fileName == null ||
                CurrentWallpaper.Project.File == fileName) {
                return;
            }
            try {
                ContentFileName = fileName;
                CurrentWallpaper.Project.File = fileName;
                // 保存到project.json
                await SaveToProjectJsonAsync();
                // 更新数据库
                await UpdateDatabaseAsync();
            } catch (Exception ex) {
                ShowErrorMessage($"保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否可以设置预览图文件名
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>当前壁纸不为空且文件名有效时返回true</returns>
        private bool CanSetPreviewFileName(string? fileName)
        {
            if (CurrentWallpaper == null || fileName == null) {
                return false;
            }
            return IsValidPreviewFileName(fileName);
        }

        /// <summary>
        /// 判断是否可以设置内容文件名
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>当前壁纸不为空且文件名有效时返回true</returns>
        private bool CanSetContentFileName(string? fileName)
        {
            if (CurrentWallpaper == null || fileName == null) {
                return false;
            }
            return IsValidContentFileName(fileName);
        }

        /// <summary>
        /// 验证预览图文件名是否有效（排除project.json和thumbs.db）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件名有效时返回true</returns>
        private bool IsValidPreviewFileName(string? fileName)
        {
            string lowerFileName = fileName?.ToLower() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lowerFileName)) {
                return false;
            }
            if (lowerFileName == "project.json" || lowerFileName == "thumbs.db") {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 验证内容文件名是否有效（排除project.json、预览图文件和thumbs.db）
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>文件名有效时返回true</returns>
        private bool IsValidContentFileName(string? fileName)
        {
            string lowerFileName = fileName?.ToLower() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lowerFileName)) {
                return false;
            }
            if (lowerFileName == "project.json" ||
                lowerFileName == "preview.jpg" ||
                lowerFileName == "preview.png" ||
                lowerFileName == "preview.jpeg" ||
                lowerFileName == "preview.gif" ||
                lowerFileName == "thumbs.db") {
                return false;
            }
            return true;
        }
    }
}
