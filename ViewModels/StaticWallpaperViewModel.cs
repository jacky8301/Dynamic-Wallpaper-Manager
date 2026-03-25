using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;
using WallpaperEngine.Views;
using Application = System.Windows.Application;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 静态壁纸视图模型，管理静态图片壁纸的导入、浏览、预览和设置
    /// </summary>
    public partial class StaticWallpaperViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;

        /// <summary>静态壁纸集合</summary>
        [ObservableProperty]
        private ObservableCollection<StaticWallpaperItem> _staticWallpapers = new();

        /// <summary>当前选中的壁纸</summary>
        [ObservableProperty]
        private StaticWallpaperItem? _selectedWallpaper;

        /// <summary>多选时选中的壁纸列表</summary>
        [ObservableProperty]
        private ObservableCollection<StaticWallpaperItem> _selectedWallpapers = new();

        /// <summary>最后一次选中的壁纸（用于Shift多选范围）</summary>
        private StaticWallpaperItem? _lastSelectedItem;

        /// <summary>搜索文本</summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>是否正在导入</summary>
        [ObservableProperty]
        private bool _isImporting;

        /// <summary>壁纸集合视图，支持筛选</summary>
        public ICollectionView StaticWallpapersView { get; }

        /// <summary>壁纸总数</summary>
        public int WallpaperCount => StaticWallpapers.Count(FilterWallpapers);

        /// <summary>是否有选中壁纸</summary>
        public bool HasSelection => SelectedWallpaper != null;

        public StaticWallpaperViewModel()
        {
            _dbManager = Ioc.Default.GetService<DatabaseManager>();
            StaticWallpapersView = CollectionViewSource.GetDefaultView(StaticWallpapers);
            StaticWallpapersView.Filter = FilterWallpapers;
            StaticWallpapers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(WallpaperCount));
        }

        // ==================== 数据加载 ====================

        /// <summary>
        /// 异步加载所有静态壁纸
        /// </summary>
        public async Task LoadStaticWallpapersAsync()
        {
            try {
                var items = await Task.Run(() => _dbManager.GetAllStaticWallpapers());
                await Application.Current.Dispatcher.InvokeAsync(() => {
                    StaticWallpapers.Clear();
                    foreach (var item in items) {
                        StaticWallpapers.Add(item);
                    }
                    StaticWallpapersView.Refresh();
                    OnPropertyChanged(nameof(WallpaperCount));
                });
            } catch (Exception ex) {
                Log.Warning(ex, "加载静态壁纸列表失败");
            }
        }

        // ==================== 筛选 ====================

        private bool FilterWallpapers(object obj)
        {
            if (obj is not StaticWallpaperItem wallpaper) return false;
            return string.IsNullOrEmpty(SearchText) ||
                   wallpaper.FileName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
        }

        partial void OnSearchTextChanged(string value)
        {
            StaticWallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }

        partial void OnSelectedWallpaperChanged(StaticWallpaperItem? value)
        {
            OnPropertyChanged(nameof(HasSelection));
        }

        // ==================== 导入命令 ====================

        /// <summary>导入图片文件</summary>
        [RelayCommand]
        private async Task ImportImages()
        {
            if (IsImporting) return;
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog {
                Title = "选择图片文件",
                Filter = "图片文件|*.jpg;*.jpeg;*.png;*.bmp;*.webp|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() != true) return;

            await ImportFiles(dialog.FileNames);
        }

        /// <summary>扫描文件夹</summary>
        [RelayCommand]
        private async Task ScanFolder()
        {
            if (IsImporting) return;
            var dialog = new System.Windows.Forms.FolderBrowserDialog {
                Description = "选择图片文件夹",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            IsImporting = true;
            try {
                string[] supportedExtensions = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.webp" };
                var files = new List<string>();
                foreach (string ext in supportedExtensions) {
                    files.AddRange(Directory.GetFiles(dialog.SelectedPath, ext, SearchOption.TopDirectoryOnly));
                }

                if (files.Count == 0) {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = "所选文件夹中没有找到支持的图片文件。",
                        Title = "提示",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                    return;
                }

                await ImportFiles(files.ToArray());
            } catch (Exception ex) {
                Log.Error(ex, "扫描文件夹失败");
                await MaterialDialogService.ShowErrorAsync($"扫描文件夹失败: {ex.Message}", "错误");
            } finally {
                IsImporting = false;
            }
        }

        private async Task ImportFiles(string[] filePaths)
        {
            IsImporting = true;
            try {
                var newItems = new List<StaticWallpaperItem>();

                await Task.Run(() => {
                    foreach (string filePath in filePaths) {
                        if (!DesktopWallpaperService.IsSupportedImage(filePath)) continue;
                        if (!File.Exists(filePath)) continue;

                        // 检查是否已存在
                        if (_dbManager.GetStaticWallpaperByFilePath(filePath) != null) continue;

                        var item = new StaticWallpaperItem {
                            Id = Guid.NewGuid().ToString(),
                            FilePath = filePath,
                            FileName = Path.GetFileName(filePath),
                            FileSize = new FileInfo(filePath).Length,
                            AddedDate = DateTime.Now
                        };

                        // 读取图片尺寸
                        try {
                            using var stream = File.OpenRead(filePath);
                            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                            if (decoder.Frames.Count > 0) {
                                item.Width = decoder.Frames[0].PixelWidth;
                                item.Height = decoder.Frames[0].PixelHeight;
                            }
                        } catch (Exception ex) {
                            Log.Warning("读取图片尺寸失败: {Path}, {Error}", filePath, ex.Message);
                        }

                        newItems.Add(item);
                    }

                    if (newItems.Count > 0) {
                        _dbManager.AddStaticWallpapers(newItems);
                    }
                });

                if (newItems.Count > 0) {
                    await Application.Current.Dispatcher.InvokeAsync(() => {
                        foreach (var item in newItems) {
                            StaticWallpapers.Insert(0, item);
                        }
                        StaticWallpapersView.Refresh();
                        OnPropertyChanged(nameof(WallpaperCount));
                    });

                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"成功导入 {newItems.Count} 张图片。",
                        Title = "导入完成",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                } else {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = "没有新的图片需要导入（可能已存在）。",
                        Title = "提示",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                }
            } catch (Exception ex) {
                Log.Error(ex, "导入图片失败");
                await MaterialDialogService.ShowErrorAsync($"导入图片失败: {ex.Message}", "错误");
            } finally {
                IsImporting = false;
            }
        }

        // ==================== 壁纸操作命令 ====================

        /// <summary>设置为桌面壁纸</summary>
        [RelayCommand]
        private async Task SetDesktopWallpaper(object? parameter)
        {
            StaticWallpaperItem? wallpaper = parameter as StaticWallpaperItem ?? SelectedWallpaper;
            if (wallpaper == null) return;

            // 先停止 Wallpaper Engine 动态壁纸，再设置静态壁纸
            var settingsService = Ioc.Default.GetService<ISettingsService>();
            if (settingsService != null) {
                var settings = settingsService.LoadSettings();
                DesktopWallpaperService.StopWallpaperEngine(settings.WallpaperEnginePath);
            }

            bool success = DesktopWallpaperService.SetDesktopWallpaper(wallpaper.FilePath);
            if (success) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"已将「{wallpaper.FileName}」设置为桌面壁纸。",
                    Title = "成功",
                    ShowCancelButton = false,
                    DialogType = DialogType.Information
                });
            } else {
                await MaterialDialogService.ShowErrorAsync("设置桌面壁纸失败，请检查图片文件是否存在。", "错误");
            }
        }

        /// <summary>从列表移除壁纸</summary>
        [RelayCommand]
        private async Task DeleteWallpaper(object? parameter)
        {
            if (parameter is StaticWallpaperItem wallpaper) {
                await DeleteSingleWallpaper(wallpaper);
                return;
            }

            if (SelectedWallpapers.Count > 0) {
                await DeleteMultipleWallpapers(SelectedWallpapers.ToList());
            }
        }

        private async Task DeleteSingleWallpaper(StaticWallpaperItem wallpaper)
        {
            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                DialogHost = "MainRootDialog",
                Title = "确认移除",
                Message = $"确定要从列表中移除「{wallpaper.FileName}」吗？\n\n注意：仅从列表移除，不会删除原始图片文件。",
                ConfirmButtonText = "移除",
                CancelButtonText = "取消",
                DialogType = DialogType.Warning
            });

            if (result.Confirmed) {
                try {
                    _dbManager.DeleteStaticWallpaper(wallpaper.Id);
                    StaticWallpapers.Remove(wallpaper);
                    if (SelectedWallpaper == wallpaper) {
                        SelectedWallpaper = null;
                    }
                    OnPropertyChanged(nameof(WallpaperCount));
                } catch (Exception ex) {
                    Log.Error(ex, "移除静态壁纸失败");
                    await MaterialDialogService.ShowErrorAsync($"移除失败: {ex.Message}", "错误");
                }
            }
        }

        private async Task DeleteMultipleWallpapers(List<StaticWallpaperItem> wallpapers)
        {
            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                DialogHost = "MainRootDialog",
                Title = $"确认移除 {wallpapers.Count} 张图片",
                Message = $"确定要从列表中移除选中的 {wallpapers.Count} 张图片吗？\n\n注意：仅从列表移除，不会删除原始图片文件。",
                ConfirmButtonText = "移除",
                CancelButtonText = "取消",
                DialogType = DialogType.Warning
            });

            if (result.Confirmed) {
                try {
                    _dbManager.DeleteStaticWallpapers(wallpapers.Select(w => w.Id));
                    foreach (var wallpaper in wallpapers) {
                        StaticWallpapers.Remove(wallpaper);
                    }
                    SelectedWallpaper = null;
                    ClearSelection();
                    OnPropertyChanged(nameof(WallpaperCount));
                } catch (Exception ex) {
                    Log.Error(ex, "批量移除静态壁纸失败");
                    await MaterialDialogService.ShowErrorAsync($"移除失败: {ex.Message}", "错误");
                }
            }
        }

        /// <summary>打开文件所在目录</summary>
        [RelayCommand]
        private async Task OpenFileLocation(object? parameter)
        {
            StaticWallpaperItem? wallpaper = parameter as StaticWallpaperItem ?? SelectedWallpaper;
            if (wallpaper == null) return;

            try {
                if (File.Exists(wallpaper.FilePath)) {
                    Process.Start("explorer.exe", $"/select,\"{wallpaper.FilePath}\"")?.Dispose();
                } else {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"文件不存在：{wallpaper.FilePath}",
                        Title = "错误",
                        ShowCancelButton = false,
                        DialogType = DialogType.Warning
                    });
                }
            } catch (Exception ex) {
                await MaterialDialogService.ShowErrorAsync($"打开目录失败：{ex.Message}", "错误");
            }
        }

        // ==================== 选择逻辑 ====================

        public void HandleWallpaperSelection(StaticWallpaperItem wallpaper, bool isCtrlPressed, bool isShiftPressed)
        {
            if (wallpaper == null) return;

            if (!isCtrlPressed && !isShiftPressed) {
                ClearSelection();
                AddToSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = wallpaper;
                return;
            }

            if (isCtrlPressed && !isShiftPressed) {
                ToggleSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = SelectedWallpapers.LastOrDefault();
                return;
            }

            if (isShiftPressed && !isCtrlPressed) {
                if (_lastSelectedItem == null) {
                    ClearSelection();
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                int lastIndex = StaticWallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = StaticWallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                ClearSelection();
                for (int i = start; i <= end; i++) {
                    AddToSelection(StaticWallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                return;
            }

            if (isCtrlPressed && isShiftPressed) {
                if (_lastSelectedItem == null) {
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                int lastIndex = StaticWallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = StaticWallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                for (int i = start; i <= end; i++) {
                    AddToSelection(StaticWallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                return;
            }
        }

        private void ClearSelection()
        {
            foreach (var item in SelectedWallpapers.ToList()) {
                item.IsSelected = false;
            }
            SelectedWallpapers.Clear();
        }

        private void AddToSelection(StaticWallpaperItem wallpaper)
        {
            if (!SelectedWallpapers.Contains(wallpaper)) {
                SelectedWallpapers.Add(wallpaper);
                wallpaper.IsSelected = true;
            }
        }

        private void ToggleSelection(StaticWallpaperItem wallpaper)
        {
            if (SelectedWallpapers.Contains(wallpaper)) {
                SelectedWallpapers.Remove(wallpaper);
                wallpaper.IsSelected = false;
            } else {
                AddToSelection(wallpaper);
            }
        }
    }
}
