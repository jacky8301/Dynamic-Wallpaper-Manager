using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸合集视图模型，管理合集的增删改查以及合集内壁纸的操作
    /// </summary>
    public partial class CollectionViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;

        /// <summary>合集列表</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperCollection> _collections = new();

        /// <summary>当前选中的合集</summary>
        [ObservableProperty]
        private WallpaperCollection? _selectedCollection;

        /// <summary>当前合集中的壁纸列表</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _collectionWallpapers = new();

        /// <summary>右键菜单用的合集列表（排除当前选中的合集）</summary>
        public ObservableCollection<WallpaperCollection> OtherCollections { get; } = new ObservableCollection<WallpaperCollection>();

        /// <summary>
        /// 初始化合集视图模型，加载合集列表
        /// </summary>
        /// <param name="dbManager">数据库管理器</param>
        public CollectionViewModel(DatabaseManager dbManager)
        {
            _dbManager = dbManager;
            LoadCollections();
        }

        /// <summary>
        /// 从数据库加载所有合集到列表
        /// </summary>
        public void LoadCollections()
        {
            try {
                var list = _dbManager.GetAllCollections();
                Collections.Clear();
                foreach (var c in list) {
                    Collections.Add(c);
                }

                // 如果没有选中合集且合集列表不为空，则自动选中第一个壁纸数不为0的合集
                SelectFirstNonEmptyCollection();

                RefreshOtherCollections();
            } catch (Exception ex) {
                Log.Error(ex, "加载合集列表失败");
            }
        }

        /// <summary>选中合集变更时加载对应的壁纸列表，并刷新右键菜单的其他合集列表</summary>
        partial void OnSelectedCollectionChanged(WallpaperCollection? value)
        {
            LoadCollectionWallpapers();
            RefreshOtherCollections();
        }

        public void SelectFirstNonEmptyCollection()
        {
            if (SelectedCollection == null && Collections.Count > 0)
            {
                SelectedCollection = Collections.FirstOrDefault(c => c.WallpaperCount > 0) ?? Collections.First();
            }
        }

        /// <summary>
        /// 刷新右键菜单用的其他合集列表（排除当前选中的合集）
        /// </summary>
        private void RefreshOtherCollections()
        {
            OtherCollections.Clear();
            foreach (var c in Collections)
            {
                if (c != SelectedCollection)
                {
                    OtherCollections.Add(c);
                }
            }
        }

        /// <summary>
        /// 加载当前选中合集的壁纸列表
        /// </summary>
        public void LoadCollectionWallpapers()
        {
            CollectionWallpapers.Clear();
            if (SelectedCollection == null) return;

            try {
                var wallpaperIds = _dbManager.GetCollectionItems(SelectedCollection.Id);
                foreach (var wallpaper in _dbManager.GetWallpapersByIds(wallpaperIds)) {
                    CollectionWallpapers.Add(wallpaper);
                }
            } catch (Exception ex) {
                Log.Error(ex, "加载合集壁纸失败");
            }
        }

        /// <summary>
        /// 创建合集命令，弹出输入对话框创建新合集
        /// </summary>
        [RelayCommand]
        private async Task CreateCollection()
        {
            var result = await MaterialDialogService.ShowInputAsync("新建合集", "请输入合集名称", "合集名称");
            if (result.Confirmed && result.Data is string name && !string.IsNullOrWhiteSpace(name)) {
                try {
                    var collection = _dbManager.AddCollection(name);
                    Collections.Insert(0, collection);
                    SelectedCollection = collection;
                    // 同步刷新MainViewModel的合集列表
                    var mainVm = Ioc.Default.GetService<MainViewModel>();
                    mainVm?.RefreshCollections();
                } catch (Exception ex) {
                    Log.Error(ex, "创建合集失败");
                }
            }
        }

        /// <summary>
        /// 重命名合集命令，弹出输入对话框修改合集名称
        /// </summary>
        [RelayCommand]
        private async Task RenameCollection()
        {
            if (SelectedCollection == null) return;

            var result = await MaterialDialogService.ShowInputAsync("重命名合集", "请输入新的合集名称", "", SelectedCollection.Name);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName)) {
                try {
                    _dbManager.RenameCollection(SelectedCollection.Id, newName);
                    SelectedCollection.Name = newName;
                    // 同步刷新MainViewModel的合集列表
                    var mainVm = Ioc.Default.GetService<MainViewModel>();
                    mainVm?.RefreshCollections();
                } catch (Exception ex) {
                    Log.Error(ex, "重命名合集失败");
                }
            }
        }

        /// <summary>
        /// 删除合集命令，弹出确认对话框后删除合集（不删除壁纸本身）
        /// </summary>
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
                    // 同步刷新MainViewModel的合集列表
                    var mainVm = Ioc.Default.GetService<MainViewModel>();
                    mainVm?.RefreshCollections();
                } catch (Exception ex) {
                    Log.Error(ex, "删除合集失败");
                }
            }
        }

        /// <summary>
        /// 从当前合集中移除壁纸命令
        /// </summary>
        /// <param name="wallpaper">要移除的壁纸项</param>
        [RelayCommand]
        private void RemoveFromCollection(WallpaperItem wallpaper)
        {
            if (SelectedCollection == null || wallpaper == null) return;

            try {
                _dbManager.RemoveFromCollection(SelectedCollection.Id, wallpaper.Id);
                CollectionWallpapers.Remove(wallpaper);
                SelectedCollection.WallpaperCount--;
            } catch (Exception ex) {
                Log.Error(ex, "从合集移除壁纸失败");
            }
        }

        /// <summary>
        /// 预览壁纸命令，委托给主视图模型执行
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        [RelayCommand]
        private void PreviewWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            mainVm?.PreviewWallpaperCommand.Execute(wallpaper);
        }

        /// <summary>
        /// 应用壁纸命令，委托给主视图模型执行
        /// </summary>
        /// <param name="wallpaper">要应用的壁纸项</param>
        [RelayCommand]
        private void ApplyWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            mainVm?.ApplyWallpaperCommand.Execute(wallpaper);
        }

        /// <summary>
        /// 切换壁纸收藏状态命令，同步主视图和合集视图中的收藏状态
        /// </summary>
        /// <param name="wallpaper">要切换收藏的壁纸项</param>
        [RelayCommand]
        private void ToggleFavorite(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            if (mainVm == null) return;

            // 找到主视图中对应的壁纸实例，保持状态同步
            var mainWallpaper = mainVm.Wallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
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

        /// <summary>
        /// 删除壁纸命令，确认后删除壁纸文件和数据库记录，并刷新所有视图
        /// </summary>
        /// <param name="wallpaper">要删除的壁纸项</param>
        [RelayCommand]
        private async Task DeleteWallpaper(WallpaperItem wallpaper)
        {
            if (wallpaper == null) return;

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除壁纸「{wallpaper.Project.Title}」吗？\n此操作无法撤销，所有文件将被永久删除！",
                "确认删除壁纸");

            if (!confirmed) return;

            try {
                var mainVm = Ioc.Default.GetService<MainViewModel>();
                var wallpaperFileService = Ioc.Default.GetService<IWallpaperFileService>();
                if (wallpaperFileService == null) return;

                var success = await Task.Run(() => {
                    var fileDeleted = wallpaperFileService.DeleteWallpaperFiles(wallpaper.FolderPath);
                    if (fileDeleted) {
                        _dbManager.DeleteWallpaper(wallpaper.Id);
                    }
                    return fileDeleted;
                });

                if (success) {
                    Log.Information("从合集视图删除壁纸成功: {Title}", wallpaper.Project.Title);

                    // 从当前合集视图移除
                    CollectionWallpapers.Remove(wallpaper);
                    if (SelectedCollection != null) {
                        SelectedCollection.WallpaperCount--;
                    }

                    // 从主视图移除
                    if (mainVm != null) {
                        var mainItem = mainVm.Wallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
                        if (mainItem != null) {
                            mainVm.Wallpapers.Remove(mainItem);
                        }
                        await mainVm.LoadTotalWallpaperCountAsync();
                    }

                    // 从收藏视图移除
                    var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
                    if (favoriteVm != null) {
                        var favItem = favoriteVm.FavoriteWallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
                        if (favItem != null) {
                            favoriteVm.FavoriteWallpapers.Remove(favItem);
                        }
                    }

                    // 更新其他合集的壁纸数量（该壁纸可能存在于其他合集中）
                    foreach (var collection in Collections) {
                        if (collection != SelectedCollection) {
                            var itemCount = _dbManager.GetCollectionItems(collection.Id).Count;
                            collection.WallpaperCount = itemCount;
                        }
                    }
                } else {
                    await MaterialDialogService.ShowErrorAsync($"删除壁纸「{wallpaper.Project.Title}」失败", "删除错误");
                }
            } catch (Exception ex) {
                Log.Error(ex, "删除壁纸失败: {Title}", wallpaper.Project.Title);
                await MaterialDialogService.ShowErrorAsync($"删除过程中发生错误: {ex.Message}", "删除错误");
            }
        }

        /// <summary>
        /// 将壁纸添加到指定合集命令
        /// </summary>
        /// <param name="parameter">包含壁纸对象和合集ID的参数</param>
        [RelayCommand]
        private async Task AddToSpecificCollection(object parameter)
        {
            if (parameter is not object[] args || args.Length != 2) return;
            if (args[1] is not string collectionId) return;
            if (args[0] is not WallpaperItem wallpaper) return;

            try {
                var collection = OtherCollections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null) return;

                if (_dbManager.IsInCollection(collectionId, wallpaper.Id)) {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"壁纸「{wallpaper.Project?.Title}��已存在于合集「{collection.Name}」中。",
                        Title = "提示", ShowCancelButton = false, DialogType = DialogType.Information
                    });
                    return;
                }

                _dbManager.AddToCollection(collectionId, wallpaper.Id);
                collection.WallpaperCount++;

                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"已将壁纸「{wallpaper.Project?.Title}」添加到合集「{collection.Name}」。",
                    Title = "成功", ShowCancelButton = false, DialogType = DialogType.Information
                });

                // 如果当前正在查看目标合集，刷新壁纸列表
                if (SelectedCollection?.Id == collectionId) {
                    LoadCollectionWallpapers();
                }

                // 同步主视图和收藏视图的合集列表
                var mainVm = Ioc.Default.GetService<MainViewModel>();
                mainVm?.RefreshCollections();

                var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
                favoriteVm?.RefreshCollections();
            } catch (Exception ex) {
                Log.Warning(ex, "添加壁纸到合集失败");
            }
        }

        /// <summary>
        /// 将壁纸从当前合集移动到指定合集命令
        /// </summary>
        /// <param name="parameter">包含壁纸对象和目标合集ID的参数</param>
        [RelayCommand]
        private async Task MoveToCollection(object parameter)
        {
            if (parameter is not object[] args || args.Length != 2) return;
            if (args[1] is not string targetCollectionId) return;
            if (args[0] is not WallpaperItem wallpaper) return;
            if (SelectedCollection == null) return;

            try {
                var targetCollection = OtherCollections.FirstOrDefault(c => c.Id == targetCollectionId);
                if (targetCollection == null) return;

                // 检查目标合集是否已包含该壁纸
                if (_dbManager.IsInCollection(targetCollectionId, wallpaper.Id)) {
                    // 已存在于目标合集，询问是否仅从当前合集移除
                    var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                        $"壁纸「{wallpaper.Project?.Title}」已存在于合集「{targetCollection.Name}」中。\n是否仅从当前合集「{SelectedCollection.Name}」中移除？",
                        "移动到合集");

                    if (!confirmed) return;
                } else {
                    // 添加到目标合集
                    _dbManager.AddToCollection(targetCollectionId, wallpaper.Id);
                    targetCollection.WallpaperCount++;
                }

                // 从当前合集移除
                _dbManager.RemoveFromCollection(SelectedCollection.Id, wallpaper.Id);
                CollectionWallpapers.Remove(wallpaper);
                SelectedCollection.WallpaperCount--;

                // 同步主视图和收藏视图的合集列表
                var mainVm = Ioc.Default.GetService<MainViewModel>();
                mainVm?.RefreshCollections();

                var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
                favoriteVm?.RefreshCollections();
            } catch (Exception ex) {
                Log.Warning(ex, "移动壁纸到合集失败");
            }
        }

        /// <summary>
        /// 打开壁纸所在目录命令，在文件资源管理器中打开
        /// </summary>
        /// <param name="wallpaper">壁纸项</param>
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
