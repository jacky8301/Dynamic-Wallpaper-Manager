using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.IO;
using System.Text;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的删除部分，包含壁纸删除确认、执行和错误处理逻辑
    /// </summary>
    public partial class MainViewModel {
        /// <summary>
        /// 删除壁纸命令，弹出确认对话框后执行删除
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private async Task DeleteWallpaper(object parameter)
        {
            // 优先使用传入的参数（用户点击了特定壁纸的删除按钮）
            if (parameter is WallpaperItem wallpaper) {
                Log.Information("请求删除壁纸: {Title}", wallpaper.Project.Title);
                await ShowDeletionConfirmation(wallpaper);
                return;
            }

            // 如果没有参数但有选中的壁纸，使用选中的壁纸列表（例如从右键菜单触发）
            if (SelectedWallpapers.Count > 0)
            {
                await ShowMultiDeletionConfirmation(SelectedWallpapers.ToList());
                return;
            }
        }

        /// <summary>
        /// 显示删除确认对话框，包含壁纸详细信息
        /// </summary>
        /// <param name="wallpaper">待删除的壁纸项</param>
        private async Task ShowDeletionConfirmation(WallpaperItem wallpaper)
        {
            ItemPendingDeletion = wallpaper;
            wallpaper.IsMarkedForDeletion = true;

            var confirmationMessage = BuildDeletionConfirmationMessage(wallpaper);

            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    DialogHost = "MainRootDialog",
                    Title = "确认删除壁纸",
                    Message = confirmationMessage,
                    ConfirmButtonText = "删除",
                    CancelButtonText = "取消",
                    DialogType = DialogType.Warning
            });

            if (result.Confirmed) {
                await ExecuteDeletion(wallpaper);
            } else {
                wallpaper.IsMarkedForDeletion = false;
                wallpaper.DeletionStatus = "删除已取消";
            }

            ItemPendingDeletion = null;
        }

        /// <summary>
        /// 显示多选壁纸删除确认对话框
        /// </summary>
        /// <param name="wallpapers">待删除的壁纸列表</param>
        private async Task ShowMultiDeletionConfirmation(List<WallpaperItem> wallpapers)
        {
            if (wallpapers == null || wallpapers.Count == 0) return;

            var confirmationMessage = BuildMultiDeletionConfirmationMessage(wallpapers);

            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    DialogHost = "MainRootDialog",
                    Title = $"确认删除 {wallpapers.Count} 个壁纸",
                    Message = confirmationMessage,
                    ConfirmButtonText = "删除",
                    CancelButtonText = "取消",
                    DialogType = DialogType.Warning
            });

            if (result.Confirmed) {
                foreach (var wallpaper in wallpapers)
                {
                    await ExecuteDeletion(wallpaper);
                }
            }
        }

        /// <summary>
        /// 构建多选删除确认消息
        /// </summary>
        /// <param name="wallpapers">待删除的壁纸列表</param>
        /// <returns>格式化的确认消息文本</returns>
        private string BuildMultiDeletionConfirmationMessage(List<WallpaperItem> wallpapers)
        {
            var message = new StringBuilder();
            message.AppendLine($"确定要删除选中的 {wallpapers.Count} 个壁纸吗？");
            message.AppendLine();
            message.AppendLine("壁纸列表：");
            foreach (var wallpaper in wallpapers.Take(10))
            {
                message.AppendLine($"• {wallpaper.Project.Title}");
            }
            if (wallpapers.Count > 10)
            {
                message.AppendLine($"• ... 以及 {wallpapers.Count - 10} 个其他壁纸");
            }
            message.AppendLine();
            message.AppendLine("此操作无法撤销，所有文件将被永久删除！");
            return message.ToString();
        }

        /// <summary>
        /// 构建删除确认消息，包含文件位置、数量和大小等信息
        /// </summary>
        /// <param name="wallpaper">待删除的壁纸项</param>
        /// <returns>格式化的确认消息文本</returns>
        private string BuildDeletionConfirmationMessage(WallpaperItem wallpaper)
        {
            var message = new StringBuilder();
            message.AppendLine($"确定要删除壁纸 '{wallpaper.Project.Title}' 吗？");
            message.AppendLine();

            if (wallpaper.FilesExist) {
                var files = wallpaper.GetContainedFiles();
                var totalSize = wallpaper.GetFolderSize();

                message.AppendLine($"• 位置: {wallpaper.FolderPath}");
                message.AppendLine($"• 文件数量: {files.Count} 个");
                message.AppendLine($"• 总大小: {FormatFileSize(totalSize)}");
                message.AppendLine();

                if (files.Count > 0) {
                    message.AppendLine("包含文件:");
                    foreach (var file in files.Take(5))
                    {
                        message.AppendLine($"  - {Path.GetFileName(file)}");
                    }
                    if (files.Count > 5) {
                        message.AppendLine($"  - ... 以及 {files.Count - 5} 个其他文件");
                    }
                }
            } else {
                message.AppendLine("⚠️  警告: 对应的文件夹不存在或已被删除");
            }

            message.AppendLine();
            message.AppendLine("此操作无法撤销，所有文件将被永久删除！");

            return message.ToString();
        }

        /// <summary>
        /// 执行壁纸删除操作，删除文件系统中的文件和数据库记录
        /// </summary>
        /// <param name="wallpaper">待删除的壁纸项</param>
        private async Task ExecuteDeletion(WallpaperItem wallpaper)
        {
            try {
                wallpaper.DeletionStatus = "正在删除...";
                var success = await Task.Run(() => {
                    // 删除文件系统中的文件
                    var fileDeleted = _wallpaperFileService.DeleteWallpaperFiles(wallpaper.FolderPath);
                    if (fileDeleted) {
                        // 从数据库中移除记录
                        _dbManager.DeleteWallpaper(wallpaper.Id);
                    }
                    return fileDeleted;
                });

                if (success) {
                    Log.Information("壁纸删除成功: {Title}", wallpaper.Project.Title);
                    Wallpapers.Remove(wallpaper);
                    if (SelectedWallpaper == wallpaper) {
                        SelectedWallpaper = null;
                        _dataContextService.CurrentWallpaper = null;
                    }
                    ShowDeletionSuccess(wallpaper);
                    // 更新壁纸总数
                    await LoadTotalWallpaperCountAsync();
                } else {
                    wallpaper.DeletionStatus = "删除失败";
                    await ShowErrorMessage($"删除壁纸 '{wallpaper.Project.Title}' 失败");
                }
            } catch (Exception ex) {
                Log.Error("壁纸删除失败: {Title}, 错误: {Error}", wallpaper.Project.Title, ex.Message);
                wallpaper.DeletionStatus = "删除错误";
                await ShowErrorMessage($"删除过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示错误消息对话框
        /// </summary>
        /// <param name="message">错误消息内容</param>
        private async Task ShowErrorMessage(string message)
        {
            await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                Message = message,
                Title = "删除错误",
                ConfirmButtonText = "OK",
                ShowCancelButton = false,
                DialogType = DialogType.Error
            });
        }

        /// <summary>
        /// 显示删除成功通知
        /// </summary>
        /// <param name="wallpaper">已删除的壁纸项</param>
        private void ShowDeletionSuccess(WallpaperItem wallpaper)
        {
            _ = ShowNotification($"壁纸 '{wallpaper.Project.Title}' 已成功删除");
        }
    }
}
