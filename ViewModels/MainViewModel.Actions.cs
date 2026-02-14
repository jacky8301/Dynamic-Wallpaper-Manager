using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WallpaperEngine.Models;
using WallpaperEngine.Services;
using WallpaperEngine.Views;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的操作命令部分，包含壁纸预览、应用、收藏、目录跳转、合集管理、分类管理等命令
    /// </summary>
    public partial class MainViewModel {
        /// <summary>
        /// 预览壁纸命令，支持传入壁纸对象或壁纸ID
        /// </summary>
        /// <param name="parameter">壁纸对象或壁纸ID字符串</param>
        [RelayCommand]
        private void PreviewWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                SelectedWallpaper = wallpaper;
                Log.Information("预览壁纸: {Title}", wallpaper.Project.Title);
                _dataContextService.CurrentWallpaper = wallpaper;
                OpenPreviewWindowNew(wallpaper);
            } else if (parameter is string wallpaperId) {
                var myWallpaper = Wallpapers.FirstOrDefault(w => w.Id == wallpaperId);
                if (myWallpaper != null) {
                    SelectedWallpaper = myWallpaper;
                    _dataContextService.CurrentWallpaper = myWallpaper;
                    OpenPreviewWindowNew(myWallpaper);
                }
            }
        }

        /// <summary>
        /// 根据壁纸类型选择预览方式（Web/Scene类型使用引擎预览，其他类型使用内置预览窗口）
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        private void OpenPreviewWindowNew(WallpaperItem wallpaper)
        {
            var type = wallpaper.Project?.Type;
            if (type != null && (type.Equals("web", StringComparison.OrdinalIgnoreCase) || type.Equals("scene", StringComparison.OrdinalIgnoreCase))) {
                _previewService.PreviewWallpaper(wallpaper);
            } else {
                OpenPreviewWindow(wallpaper);
            }
        }

        /// <summary>
        /// 打开内置预览窗口显示壁纸
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        private async Task OpenPreviewWindow(WallpaperItem wallpaper)
        {
            if (wallpaper?.Project == null) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "壁纸数据无效，无法预览",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }

            try {
                var previewWindow = new PreviewWindow(wallpaper) {
                    Owner = System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                previewWindow.Closed += (s, e) => {
                    if (previewWindow.DialogResult == true) {
                        // 用户点击了"应用壁纸"
                    }
                };

                previewWindow.ShowDialog();
            } catch (Exception ex) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"打开预览窗口失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Warning }
                );
            }
        }

        /// <summary>
        /// 应用壁纸命令，通过Wallpaper Engine将壁纸设置为桌面壁纸
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void ApplyWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                Log.Information("应用壁纸: {Title}", wallpaper.Project.Title);
                string toolPath = Settings.WallpaperEnginePath;
                string projectJsonPath = Path.Combine(wallpaper.FolderPath, "project.json");

                if (File.Exists(toolPath) && File.Exists(projectJsonPath)) {
                    string arguments = $"-control openWallpaper -file \"{projectJsonPath}\"";
                    ProcessStartInfo startInfo = new ProcessStartInfo {
                        FileName = toolPath,
                        Arguments = arguments,
                        UseShellExecute = false
                    };
                    Process.Start(startInfo)?.Dispose();
                } else {
                    // 处理错误
                }
            }
        }

        /// <summary>
        /// 切换壁纸收藏状态命令，同时同步收藏状态到合集视图
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void ToggleFavorite(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
                wallpaper.FavoritedDate = wallpaper.IsFavorite ? DateTime.Now : DateTime.MinValue;

                try {
                    _dbManager.ToggleFavorite(wallpaper.FolderPath, wallpaper.IsFavorite);
                } catch (Exception ex) {
                    Log.Warning($"更新收藏状态失败: {ex.Message}");
                    wallpaper.IsFavorite = !wallpaper.IsFavorite;
                }

                if (ShowFavoritesOnly && !wallpaper.IsFavorite) {
                    WallpapersView.Refresh();
                }

                // 同步收藏状态到合集视图中的壁纸实例
                var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                if (collectionVm != null) {
                    var collectionWallpaper = collectionVm.CollectionWallpapers
                        .FirstOrDefault(w => w.FolderPath == wallpaper.FolderPath);
                    if (collectionWallpaper != null) {
                        collectionWallpaper.IsFavorite = wallpaper.IsFavorite;
                        collectionWallpaper.FavoritedDate = wallpaper.FavoritedDate;
                    }
                }
            }
        }

        /// <summary>
        /// 跳转到壁纸所在目录命令，在文件资源管理器中打开
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private async Task GoToWallpaperDirectory(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                Log.Debug("打开壁纸目录: {FolderPath}", wallpaper.FolderPath);
                SelectedWallpaper = wallpaper;
                await OpenWallpaperDirectory(wallpaper);
            }
        }

        /// <summary>
        /// 打开壁纸所在文件夹
        /// </summary>
        /// <param name="wallpaper">壁纸项</param>
        private async Task OpenWallpaperDirectory(WallpaperItem wallpaper)
        {
            try {
                if (Directory.Exists(wallpaper.FolderPath)) {
                    Process.Start("explorer.exe", wallpaper.FolderPath)?.Dispose();
                } else {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"壁纸目录不存在：{wallpaper.FolderPath}",
                        Title = "错误",
                        ShowCancelButton = false,
                        DialogType = DialogType.Warning
                    });
                }
            } catch (Exception ex) {
                await MaterialDialogService.ShowErrorAsync($"打开目录失败：{ex.Message}","错误");
            }
        }

        /// <summary>
        /// 选择壁纸命令，更新当前选中状态
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void SelectWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                UpdateSelection(wallpaper);
            }
        }

        /// <summary>
        /// 更新壁纸选中状态，取消其他壁纸的选中并设置新的选中项
        /// </summary>
        /// <param name="selectedWallpaper">要选中的壁纸</param>
        private void UpdateSelection(WallpaperItem selectedWallpaper)
        {
            foreach (var wallpaper in Wallpapers.Where(w => w.IsSelected)) {
                wallpaper.IsSelected = false;
            }

            selectedWallpaper.IsSelected = true;
            SelectedWallpaper = selectedWallpaper;
            _dataContextService.CurrentWallpaper = selectedWallpaper;
        }

        /// <summary>
        /// 打开设置窗口命令
        /// </summary>
        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = Ioc.Default.GetService<SettingsViewModel>(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var result = settingsWindow.ShowDialog();
            if (result == true) {
                Settings = _settingsService.LoadSettings();
                OnPropertyChanged(nameof(Settings));
            }
        }

        /// <summary>
        /// 将壁纸添加到合集命令，弹出合集选择对话框
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private async Task AddToCollection(object parameter)
        {
            if (parameter is not WallpaperItem wallpaper) return;

            var collections = _dbManager.GetAllCollections();
            if (collections.Count == 0) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "还没有合集，请先在「壁纸合集」页面创建一个合集。",
                    Title = "提示",
                    ShowCancelButton = false,
                    DialogType = DialogType.Information
                });
                return;
            }

            var view = new SelectCollectionDialog();
            view.DataContext = new SelectCollectionDialogViewModel(collections);

            var result = await DialogHost.Show(view, "MainRootDialog");
            if (result is MaterialDialogResult dialogResult && dialogResult.Confirmed && dialogResult.Data is WallpaperCollection selected) {
                try {
                    if (_dbManager.IsInCollection(selected.Id, wallpaper.FolderPath)) {
                        await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                            Message = $"该壁纸已存在于合集「{selected.Name}」中。",
                            Title = "提示",
                            ShowCancelButton = false,
                            DialogType = DialogType.Information
                        });
                        return;
                    }
                    _dbManager.AddToCollection(selected.Id, wallpaper.FolderPath);
                    // 刷新合集页面（如果当前正在查看该合集）
                    var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                    if (collectionVm?.SelectedCollection?.Id == selected.Id) {
                        collectionVm.LoadCollectionWallpapers();
                    }
                } catch (Exception ex) {
                    Log.Warning($"添加壁纸到合集失败: {ex.Message}");
                }
            }
        }

        /// <summary>受保护的分类名称集合，不允许重命名或删除</summary>
        private static readonly HashSet<string> ProtectedCategories = new() { "所有分类", "未分类" };

        /// <summary>
        /// 重命名分类命令，同步更新数据库、内存中的壁纸和详情页分类列表
        /// </summary>
        /// <param name="category">要重命名的分类名称</param>
        [RelayCommand]
        private async Task RenameCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || ProtectedCategories.Contains(category)) return;

            var result = await MaterialDialogService.ShowInputAsync("重命名分类", "请输入新的分类名称", category);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName) && newName != category) {
                if (Categories.Contains(newName)) {
                    await MaterialDialogService.ShowErrorAsync("该分类名称已存在", "错误");
                    return;
                }
                try {
                    _dbManager.RenameCategory(category, newName);
                    var index = Categories.IndexOf(category);
                    Categories[index] = newName;
                    // 更新内存中壁纸的分类
                    foreach (var w in Wallpapers.Where(w => w.Category == category)) {
                        w.Category = newName;
                    }
                    if (SelectedCategory == category) {
                        SelectedCategory = newName;
                    }
                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm != null) {
                        var detailIndex = detailVm.CategoryList.IndexOf(category);
                        if (detailIndex >= 0) {
                            detailVm.CategoryList[detailIndex] = newName;
                        }
                    }
                } catch (Exception ex) {
                    Log.Warning($"重命名分类失败: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 删除分类命令，将该分类下的壁纸重置为"未分类"
        /// </summary>
        /// <param name="category">要删除的分类名称</param>
        [RelayCommand]
        private async Task DeleteCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || ProtectedCategories.Contains(category)) return;

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除分类「{category}」吗？\n该分类下的壁纸将被重置为「未分类」。",
                "删除分类");

            if (confirmed) {
                try {
                    _dbManager.DeleteCategory(category);
                    Categories.Remove(category);
                    // 更新内存中壁纸的分类
                    foreach (var w in Wallpapers.Where(w => w.Category == category)) {
                        w.Category = "未分类";
                    }
                    if (SelectedCategory == category) {
                        SelectedCategory = "所有分类";
                    }
                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    detailVm?.CategoryList.Remove(category);
                } catch (Exception ex) {
                    Log.Warning($"删除分类失败: {ex.Message}");
                }
            }
        }
    }
}
