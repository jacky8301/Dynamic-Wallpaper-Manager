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
    public partial class MainViewModel {
        [RelayCommand]
        private void PreviewWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                SelectedWallpaper = wallpaper;
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

        private void OpenPreviewWindowNew(WallpaperItem wallpaper)
        {
            var type = wallpaper.Project?.Type;
            if (type != null && (type.Equals("web", StringComparison.OrdinalIgnoreCase) || type.Equals("scene", StringComparison.OrdinalIgnoreCase))) {
                _previewService.PreviewWallpaper(wallpaper);
            } else {
                OpenPreviewWindow(wallpaper);
            }
        }

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

        [RelayCommand]
        private void ApplyWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
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
            }
        }

        [RelayCommand]
        private async Task GoToWallpaperDirectory(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                SelectedWallpaper = wallpaper;
                await OpenWallpaperDirectory(wallpaper);
            }
        }

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

        [RelayCommand]
        private void SelectWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                UpdateSelection(wallpaper);
            }
        }

        private void UpdateSelection(WallpaperItem selectedWallpaper)
        {
            foreach (var wallpaper in Wallpapers.Where(w => w.IsSelected)) {
                wallpaper.IsSelected = false;
            }

            selectedWallpaper.IsSelected = true;
            SelectedWallpaper = selectedWallpaper;
            _dataContextService.CurrentWallpaper = selectedWallpaper;
        }

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
    }
}
