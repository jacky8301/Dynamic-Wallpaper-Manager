namespace WallpaperEngine.ViewModels {
    public partial class WallpaperDetailViewModel {
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

        private bool CanSetPreviewFileName(string? fileName)
        {
            if (CurrentWallpaper == null || fileName == null) {
                return false;
            }
            return IsValidPreviewFileName(fileName);
        }

        private bool CanSetContentFileName(string? fileName)
        {
            if (CurrentWallpaper == null || fileName == null) {
                return false;
            }
            return IsValidContentFileName(fileName);
        }

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
