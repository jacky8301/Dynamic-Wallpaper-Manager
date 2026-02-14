using Newtonsoft.Json;
using System.IO;
using System.Text;
using WallpaperEngine.Models;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸详情视图模型的编辑部分，包含开始编辑、保存、取消和数据持久化逻辑
    /// </summary>
    public partial class WallpaperDetailViewModel {
        /// <summary>编辑模式变更时通知保存命令更新可执行状态</summary>
        partial void OnIsEditModeChanged(bool value)
        {
            SaveEditCommand.NotifyCanExecuteChanged();
        }

        /// <summary>当前壁纸变更时通知保存命令更新可执行状态</summary>
        partial void OnCurrentWallpaperChanged(WallpaperItem? value)
        {
            SaveEditCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 进入编辑模式，创建当前壁纸数据的备份
        /// </summary>
        private void StartEdit()
        {
            if (CurrentWallpaper == null) return;

            IsEditMode = true;
            EditStatus = "编辑模式";

            // 创建编辑备份
            _originalItem = CreateBackup(CurrentWallpaper);
            CurrentWallpaper.IsEditing = true;
        }

        /// <summary>
        /// 保存编辑内容，将修改写入project.json文件和数据库
        /// </summary>
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
                if (SelectedCategory != null && CurrentWallpaper.Category != SelectedCategory) {
                    CurrentWallpaper.Category = SelectedCategory;
                    CurrentWallpaper.Project.Category = SelectedCategory;
                }

                if (Description != null && CurrentWallpaper.Project.Description != Description) {
                    CurrentWallpaper.Project.Description = Description;
                }

                // 同步标签
                CurrentWallpaper.Project.Tags = new List<string>(Tags);

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

        /// <summary>
        /// 判断是否可以执行保存操作
        /// </summary>
        /// <returns>当前壁纸不为空且处于编辑模式时返回true</returns>
        private bool CanSaveEdit()
        {
            return CurrentWallpaper != null && IsEditMode;
        }

        /// <summary>
        /// 取消编辑，从备份恢复原始数据并退出编辑模式
        /// </summary>
        private void CancelEdit()
        {
            if (CurrentWallpaper == null) return;

            // 恢复原始数据
            if (_originalItem != null) {
                RestoreFromBackup(CurrentWallpaper, _originalItem);
                SelectedType = CurrentWallpaper.Project.Type;
                SelectedCategory = CurrentWallpaper.Category;
                Description = CurrentWallpaper.Project.Description;
                Title = CurrentWallpaper.Project.Title;
                SyncTagsFromProject();
            }

            IsEditMode = false;
            CurrentWallpaper.IsEditing = false;
            EditStatus = "编辑已取消";
        }

        /// <summary>
        /// 将壁纸项目数据序列化并保存到project.json文件
        /// </summary>
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

        /// <summary>
        /// 异步更新数据库中的壁纸记录
        /// </summary>
        private async Task UpdateDatabaseAsync()
        {
            await Task.Run(() => {
                _dbManager.UpdateWallpaper(CurrentWallpaper);
            });
        }
    }
}
