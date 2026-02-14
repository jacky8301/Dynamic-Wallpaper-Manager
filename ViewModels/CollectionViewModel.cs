using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
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
    }
}
