using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class CollectionViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;

        [ObservableProperty]
        private ObservableCollection<WallpaperCollection> _collections = new();

        [ObservableProperty]
        private WallpaperCollection? _selectedCollection;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _collectionWallpapers = new();

        public CollectionViewModel(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            LoadCollections();
        }

        public void LoadCollections()
        {
            try {
                var list = _dbManager.GetAllCollections();
                Collections.Clear();
                foreach (var c in list) {
                    Collections.Add(c);
                }
            } catch (Exception ex) {
                Log.Error($"加载合集列表失败: {ex.Message}");
            }
        }

        partial void OnSelectedCollectionChanged(WallpaperCollection? value)
        {
            LoadCollectionWallpapers();
        }

        public void LoadCollectionWallpapers()
        {
            CollectionWallpapers.Clear();
            if (SelectedCollection == null) return;

            try {
                var folderPaths = _dbManager.GetCollectionItems(SelectedCollection.Id);
                foreach (var path in folderPaths) {
                    var wallpaper = _dbManager.GetWallpaperByFolderPath(path);
                    if (wallpaper != null) {
                        CollectionWallpapers.Add(wallpaper);
                    }
                }
            } catch (Exception ex) {
                Log.Error($"加载合集壁纸失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task CreateCollection()
        {
            var result = await MaterialDialogService.ShowInputAsync("新建合集", "请输入合集名称", "合集名称");
            if (result.Confirmed && result.Data is string name && !string.IsNullOrWhiteSpace(name)) {
                try {
                    var collection = _dbManager.AddCollection(name);
                    Collections.Insert(0, collection);
                    SelectedCollection = collection;
                } catch (Exception ex) {
                    Log.Error($"创建合集失败: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task RenameCollection()
        {
            if (SelectedCollection == null) return;

            var result = await MaterialDialogService.ShowInputAsync("重命名合集", "请输入新的合集名称", SelectedCollection.Name);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName)) {
                try {
                    _dbManager.RenameCollection(SelectedCollection.Id, newName);
                    SelectedCollection.Name = newName;
                } catch (Exception ex) {
                    Log.Error($"重命名合集失败: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private async Task DeleteCollection()
        {
            if (SelectedCollection == null) return;

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除合集「{SelectedCollection.Name}」吗？\n删除后壁纸本身不会被删除。",
                "删除合集");

            if (confirmed) {
                try {
                    _dbManager.DeleteCollection(SelectedCollection.Id);
                    Collections.Remove(SelectedCollection);
                    SelectedCollection = Collections.FirstOrDefault();
                } catch (Exception ex) {
                    Log.Error($"删除合集失败: {ex.Message}");
                }
            }
        }

        [RelayCommand]
        private void RemoveFromCollection(WallpaperItem wallpaper)
        {
            if (SelectedCollection == null || wallpaper == null) return;

            try {
                _dbManager.RemoveFromCollection(SelectedCollection.Id, wallpaper.FolderPath);
                CollectionWallpapers.Remove(wallpaper);
            } catch (Exception ex) {
                Log.Error($"从合集移除壁纸失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void PreviewWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            mainVm?.PreviewWallpaperCommand.Execute(wallpaper);
        }

        [RelayCommand]
        private void ApplyWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            mainVm?.ApplyWallpaperCommand.Execute(wallpaper);
        }

        [RelayCommand]
        private void ToggleFavorite(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            if (mainVm == null) return;

            // 找到主视图中对应的壁纸实例，保持状态同步
            var mainWallpaper = mainVm.Wallpapers.FirstOrDefault(w => w.FolderPath == wallpaper.FolderPath);
            if (mainWallpaper != null) {
                mainVm.ToggleFavoriteCommand.Execute(mainWallpaper);
                // 同步状态到合集中的实例
                wallpaper.IsFavorite = mainWallpaper.IsFavorite;
                wallpaper.FavoritedDate = mainWallpaper.FavoritedDate;
            } else {
                // 主视图中没有对应实例，直接操作
                mainVm.ToggleFavoriteCommand.Execute(wallpaper);
            }
        }

        [RelayCommand]
        private async Task GoToWallpaperDirectory(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            try {
                if (Directory.Exists(wallpaper.FolderPath)) {
                    Process.Start("explorer.exe", wallpaper.FolderPath)?.Dispose();
                } else {
                    await MaterialDialogService.ShowErrorAsync($"壁纸目录不存在：{wallpaper.FolderPath}", "错误");
                }
            } catch (Exception ex) {
                await MaterialDialogService.ShowErrorAsync($"打开目录失败：{ex.Message}", "错误");
            }
        }
    }
}
