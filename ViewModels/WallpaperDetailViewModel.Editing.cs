using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace WallpaperEngine.ViewModels {
    public partial class WallpaperDetailViewModel {
        private void StartEdit()
        {
            if (CurrentWallpaper == null) return;

            IsEditMode = true;
            EditStatus = "编辑模式";

            // 创建编辑备份
            _originalItem = CreateBackup(CurrentWallpaper);
            CurrentWallpaper.IsEditing = true;
        }

        private async Task SaveEdit()
        {
            if (CurrentWallpaper == null) return;

            try {
                EditStatus = "正在保存...";
                if (Title != null && CurrentWallpaper.Project.Title != Title) {
                    CurrentWallpaper.Project.Title = Title;
                }
                if (SelectedType != null && CurrentWallpaper.Project.Type != SelectedType) {
                    CurrentWallpaper.Project.Type = SelectedType.ToLower();
                }

                if (Description != null && CurrentWallpaper.Project.Description != Description) {
                    CurrentWallpaper.Project.Description = Description;
                }

                // 保存到project.json
                await SaveToProjectJsonAsync();

                // 更新数据库
                await UpdateDatabaseAsync();

                // 退出编辑模式
                IsEditMode = false;
                CurrentWallpaper.IsEditing = false;
                EditStatus = "保存成功";
                // 显示成功消息
                ShowSaveSuccessMessage();
            } catch (Exception ex) {
                EditStatus = "保存失败";
                ShowErrorMessage($"保存失败: {ex.Message}");
            }
        }

        private bool CanSaveEdit()
        {
            return CurrentWallpaper != null && IsEditMode;
        }

        private void CancelEdit()
        {
            if (CurrentWallpaper == null) return;

            // 恢复原始数据
            if (_originalItem != null) {
                RestoreFromBackup(CurrentWallpaper, _originalItem);
            }

            IsEditMode = false;
            CurrentWallpaper.IsEditing = false;
            EditStatus = "编辑已取消";
        }

        // 保存到project.json文件
        private async Task SaveToProjectJsonAsync()
        {
            var projectJsonPath = Path.Combine(CurrentWallpaper.FolderPath, "project.json");

            try {
                var jsonSettings = new JsonSerializerSettings {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var jsonContent = JsonConvert.SerializeObject(CurrentWallpaper.Project, jsonSettings);
                await File.WriteAllTextAsync(projectJsonPath, jsonContent, Encoding.UTF8);
            } catch (Exception ex) {
                throw new InvalidOperationException($"无法保存project.json: {ex.Message}", ex);
            }
        }

        // 更新数据库
        private async Task UpdateDatabaseAsync()
        {
            await Task.Run(() => {
                _dbManager.UpdateWallpaper(CurrentWallpaper);
            });
        }
    }
}
