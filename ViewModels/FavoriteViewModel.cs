using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Data;
using WallpaperEngine.Data;
using WallpaperEngine.Events;
using WallpaperEngine.Models;
using WallpaperEngine.Services;
using WallpaperEngine.Views;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 收藏壁纸视图模型，独立管理收藏壁纸的展示、搜索、筛选和操作
    /// </summary>
    public partial class FavoriteViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;
        private readonly PreviewService _previewService;
        private readonly ISettingsService _settingsService;
        private readonly IDataContextService _dataContextService;
        private readonly IWallpaperFileService _wallpaperFileService;
        private readonly ICategoryService _categoryService;

        /// <summary>收藏壁纸集合</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _favoriteWallpapers = new();

        /// <summary>当前选中的壁纸</summary>
        [ObservableProperty]
        private WallpaperItem? _selectedWallpaper;

        /// <summary>多选时选中的壁纸列表</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _selectedWallpapers = new();

        /// <summary>最后一次选中的壁纸（用于Shift多选范围）</summary>
        private WallpaperItem? _lastSelectedItem;

        /// <summary>搜索文本</summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>当前选中的分类ID</summary>
        [ObservableProperty]
        private string _selectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;

        /// <summary>当前选中的分类项</summary>
        [ObservableProperty]
        private CategoryItem? _selectedCategory;

        /// <summary>是否隐藏成人内容</summary>
        [ObservableProperty]
        private bool _hideAdultContent;

        /// <summary>应用程序设置</summary>
        [ObservableProperty]
        private ApplicationSettings _settings;

        /// <summary>收藏壁纸的集合视图，支持筛选和排序</summary>
        public ICollectionView FavoriteWallpapersView { get; }

        /// <summary>壁纸分类列表</summary>
        public ObservableCollection<CategoryItem> Categories { get; } = new ObservableCollection<CategoryItem>();

        /// <summary>右键菜单用的分类列表（排除虚拟分类）</summary>
        public ObservableCollection<CategoryItem> MenuCategories { get; } = new ObservableCollection<CategoryItem>();

        /// <summary>壁纸合集列表，用于右键菜单快速添加</summary>
        public ObservableCollection<WallpaperCollection> Collections { get; } = new ObservableCollection<WallpaperCollection>();

        /// <summary>当前筛选条件下的壁纸总数</summary>
        public int WallpaperCount => FavoriteWallpapers.Count(FilterWallpapers);

        /// <summary>防止SelectedCategory和SelectedCategoryId之间递归更新的标志</summary>
        private bool _updatingSelection;

        public FavoriteViewModel(IDataContextService dataContextService, IWallpaperFileService wallpaperFileService, ICategoryService categoryService)
        {
            _dbManager = Ioc.Default.GetService<DatabaseManager>();
            _settingsService = Ioc.Default.GetService<ISettingsService>();
            _settings = _settingsService.LoadSettings();
            _previewService = new PreviewService(_settingsService);
            _dataContextService = dataContextService;
            _wallpaperFileService = wallpaperFileService;
            _categoryService = categoryService;

            FavoriteWallpapersView = CollectionViewSource.GetDefaultView(FavoriteWallpapers);
            FavoriteWallpapersView.Filter = FilterWallpapers;

            FavoriteWallpapers.CollectionChanged += (s, e) => OnPropertyChanged(nameof(WallpaperCount));

            // 订阅分类变更事件
            _categoryService.CategoryChanged += OnCategoryChanged;

            // 加载分类和合集
            LoadCustomCategories();
            LoadCollections();

            // 订阅详情页新增分类事件
            var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
            if (detailVm != null) {
                detailVm.CategoryAdded += OnCategoryAdded;
            }
        }

        // ==================== 数据加载 ====================

        /// <summary>
        /// 异步加载收藏壁纸列表
        /// </summary>
        public async Task LoadFavoritesAsync()
        {
            try {
                string searchText = SearchText;
                string categoryId = SelectedCategoryId;
                bool hideAdult = HideAdultContent;

                var favorites = await Task.Run(() => {
                    return _dbManager.SearchWallpapers(searchText, categoryId, true, hideAdult);
                });

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    FavoriteWallpapers.Clear();
                    if (favorites != null) {
                        foreach (var wallpaper in favorites) {
                            FavoriteWallpapers.Add(wallpaper);
                        }
                    }
                    FavoriteWallpapersView.Refresh();
                    OnPropertyChanged(nameof(WallpaperCount));
                });
            } catch (Exception ex) {
                Log.Warning($"加载收藏壁纸列表失败: {ex.Message}");
            }
        }

        // ==================== 筛选 ====================

        private bool FilterWallpapers(object obj)
        {
            if (obj is not WallpaperItem wallpaper) return false;

            bool matchesSearch = string.IsNullOrEmpty(SearchText) ||
                               wallpaper.Project.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               wallpaper.Project.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               wallpaper.Project.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            bool matchesCategory = SelectedCategoryId == CategoryConstants.ALL_CATEGORIES_ID || wallpaper.CategoryId == SelectedCategoryId;
            bool matchesAdultFilter = !HideAdultContent || (wallpaper.Project.ContentRating != "Mature" && wallpaper.Project.ContentRating != "Questionable");

            return matchesSearch && matchesCategory && matchesAdultFilter;
        }

        partial void OnSearchTextChanged(string value)
        {
            FavoriteWallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }

        partial void OnSelectedCategoryIdChanged(string value)
        {
            if (!_updatingSelection)
            {
                _updatingSelection = true;
                try
                {
                    var category = Categories.FirstOrDefault(c => c.Id == value);
                    SelectedCategory = category;
                }
                finally
                {
                    _updatingSelection = false;
                }
            }

            FavoriteWallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }

        partial void OnSelectedCategoryChanged(CategoryItem? value)
        {
            if (!_updatingSelection)
            {
                _updatingSelection = true;
                try
                {
                    SelectedCategoryId = value?.Id ?? CategoryConstants.ALL_CATEGORIES_ID;
                }
                finally
                {
                    _updatingSelection = false;
                }
            }
        }

        partial void OnHideAdultContentChanged(bool value)
        {
            FavoriteWallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }

        [RelayCommand]
        private async Task SearchWallpapers()
        {
            await LoadFavoritesAsync();
        }

        [RelayCommand]
        private void ResetCategory()
        {
            if (SelectedCategoryId != CategoryConstants.ALL_CATEGORIES_ID)
            {
                SelectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;
            }
        }

        // ==================== 命令 ====================

        /// <summary>预览壁纸命令</summary>
        [RelayCommand]
        private async Task PreviewWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wp) {
                SelectedWallpaper = wp;
                Log.Information("预览壁纸: {Title}", wp.Project.Title);
                _dataContextService.CurrentWallpaper = wp;
                await OpenPreviewWindowNew(wp);
                return;
            } else if (parameter is string wallpaperId) {
                var myWallpaper = FavoriteWallpapers.FirstOrDefault(w => w.Id == wallpaperId);
                if (myWallpaper != null) {
                    SelectedWallpaper = myWallpaper;
                    _dataContextService.CurrentWallpaper = myWallpaper;
                    await OpenPreviewWindowNew(myWallpaper);
                }
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    SelectedWallpaper = selectedWallpaper;
                    _dataContextService.CurrentWallpaper = selectedWallpaper;
                    await OpenPreviewWindowNew(selectedWallpaper);
                }
            }
        }

        private async Task OpenPreviewWindowNew(WallpaperItem wallpaper)
        {
            var type = wallpaper.Project?.Type;
            if (type != null && (type.Equals("web", StringComparison.OrdinalIgnoreCase) || type.Equals("scene", StringComparison.OrdinalIgnoreCase))) {
                var options = new PreviewService.PreviewOptions();

                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    System.Windows.Point windowTopLeft = new System.Windows.Point(0, 0);
                    System.Windows.Point windowBottomRight = new System.Windows.Point(mainWindow.ActualWidth, mainWindow.ActualHeight);
                    System.Windows.Point screenTopLeft = mainWindow.PointToScreen(windowTopLeft);
                    System.Windows.Point screenBottomRight = mainWindow.PointToScreen(windowBottomRight);

                    double mainWindowScreenLeft = screenTopLeft.X;
                    double mainWindowScreenTop = screenTopLeft.Y;
                    double mainWindowScreenWidth = screenBottomRight.X - screenTopLeft.X;
                    double mainWindowScreenHeight = screenBottomRight.Y - screenTopLeft.Y;

                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;
                    var source = System.Windows.PresentationSource.FromVisual(mainWindow);
                    if (source != null && source.CompositionTarget != null)
                    {
                        System.Windows.Media.Matrix matrix = source.CompositionTarget.TransformToDevice;
                        dpiScaleX = matrix.M11;
                        dpiScaleY = matrix.M22;
                    }

                    int previewWidthPhysical = (int)(options.Width * dpiScaleX);
                    int previewHeightPhysical = (int)(options.Height * dpiScaleY);

                    if (previewWidthPhysical > mainWindowScreenWidth * 0.8)
                        previewWidthPhysical = (int)(mainWindowScreenWidth * 0.8);
                    if (previewHeightPhysical > mainWindowScreenHeight * 0.8)
                        previewHeightPhysical = (int)(mainWindowScreenHeight * 0.8);

                    options.Width = previewWidthPhysical;
                    options.Height = previewHeightPhysical;

                    int x = (int)(mainWindowScreenLeft + (mainWindowScreenWidth - previewWidthPhysical) / 2);
                    int y = (int)(mainWindowScreenTop + (mainWindowScreenHeight - previewHeightPhysical) / 2);

                    try
                    {
                        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y));
                        if (screen != null)
                        {
                            var screenBounds = screen.WorkingArea;
                            x = Math.Max(screenBounds.Left, Math.Min(x, screenBounds.Right - previewWidthPhysical));
                            y = Math.Max(screenBounds.Top, Math.Min(y, screenBounds.Bottom - previewHeightPhysical));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("获取屏幕信息失败: {Error}", ex.Message);
                    }

                    options.X = x;
                    options.Y = y;
                }

                _previewService.PreviewWallpaper(wallpaper, options);
            } else {
                await OpenPreviewWindow(wallpaper);
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
                    WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner
                };
                previewWindow.ShowDialog();
            } catch (Exception ex) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"打开预览窗口失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Warning
                });
            }
        }

        /// <summary>应用壁纸命令</summary>
        [RelayCommand]
        private void ApplyWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wp) {
                ApplySingleWallpaper(wp);
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    ApplySingleWallpaper(selectedWallpaper);
                }
            }
        }

        private void ApplySingleWallpaper(WallpaperItem wallpaper)
        {
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
            }
        }

        /// <summary>切换收藏状态命令</summary>
        [RelayCommand]
        private void ToggleFavorite(object parameter)
        {
            WallpaperItem? parameterWallpaper = null;
            if (parameter is WallpaperItem wp)
            {
                parameterWallpaper = wp;
            }

            if (parameterWallpaper != null)
            {
                bool isParameterInSelected = SelectedWallpapers.Contains(parameterWallpaper);

                if (isParameterInSelected && SelectedWallpapers.Count > 1)
                {
                    foreach (var wallpaperItem in SelectedWallpapers.ToList())
                    {
                        ToggleSingleFavorite(wallpaperItem);
                    }
                }
                else
                {
                    ToggleSingleFavorite(parameterWallpaper);
                    HandleWallpaperSelection(parameterWallpaper, false, false);
                }
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                foreach (var wallpaperItem in SelectedWallpapers.ToList())
                {
                    ToggleSingleFavorite(wallpaperItem);
                }
            }
        }

        private void ToggleSingleFavorite(WallpaperItem wallpaper)
        {
            wallpaper.IsFavorite = !wallpaper.IsFavorite;
            wallpaper.FavoritedDate = wallpaper.IsFavorite ? DateTime.Now : DateTime.MinValue;

            try {
                _dbManager.ToggleFavorite(wallpaper.Id, wallpaper.IsFavorite);
            } catch (Exception ex) {
                Log.Warning($"更新收藏状态失败: {ex.Message}");
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
                return;
            }

            // 取消收藏时从集合移除
            if (!wallpaper.IsFavorite)
            {
                FavoriteWallpapers.Remove(wallpaper);
            }

            // 同步到 MainViewModel.Wallpapers
            var mainVm = Ioc.Default.GetService<MainViewModel>();
            if (mainVm != null)
            {
                var mainItem = mainVm.Wallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
                if (mainItem != null)
                {
                    mainItem.IsFavorite = wallpaper.IsFavorite;
                    mainItem.FavoritedDate = wallpaper.FavoritedDate;
                }
            }

            // 同步到 CollectionViewModel
            var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
            if (collectionVm != null) {
                var collectionWallpaper = collectionVm.CollectionWallpapers
                    .FirstOrDefault(w => w.Id == wallpaper.Id);
                if (collectionWallpaper != null) {
                    collectionWallpaper.IsFavorite = wallpaper.IsFavorite;
                    collectionWallpaper.FavoritedDate = wallpaper.FavoritedDate;
                }
            }
        }

        /// <summary>跳转到壁纸目录命令</summary>
        [RelayCommand]
        private async Task GoToWallpaperDirectory(object parameter)
        {
            if (parameter is WallpaperItem wp) {
                SelectedWallpaper = wp;
                await OpenWallpaperDirectory(wp);
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    SelectedWallpaper = selectedWallpaper;
                    await OpenWallpaperDirectory(selectedWallpaper);
                }
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
                await MaterialDialogService.ShowErrorAsync($"打开目录失败：{ex.Message}", "错误");
            }
        }

        /// <summary>删除壁纸命令</summary>
        [RelayCommand]
        private async Task DeleteWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                await ShowDeletionConfirmation(wallpaper);
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                await ShowMultiDeletionConfirmation(SelectedWallpapers.ToList());
            }
        }

        private async Task ShowDeletionConfirmation(WallpaperItem wallpaper)
        {
            wallpaper.IsMarkedForDeletion = true;

            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                DialogHost = "MainRootDialog",
                Title = "确认删除壁纸",
                Message = $"确定要删除壁纸 '{wallpaper.Project.Title}' 吗？\n\n此操作无法撤销，所有文件将被永久删除！",
                ConfirmButtonText = "删除",
                CancelButtonText = "取消",
                DialogType = DialogType.Warning
            });

            if (result.Confirmed) {
                await ExecuteDeletion(wallpaper);
            } else {
                wallpaper.IsMarkedForDeletion = false;
            }
        }

        private async Task ShowMultiDeletionConfirmation(List<WallpaperItem> wallpapers)
        {
            if (wallpapers == null || wallpapers.Count == 0) return;

            var result = await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                DialogHost = "MainRootDialog",
                Title = $"确认删除 {wallpapers.Count} 个壁纸",
                Message = $"确定要删除选中的 {wallpapers.Count} 个壁纸吗？\n\n此操作无法撤销，所有文件将被永久删除！",
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

        private async Task ExecuteDeletion(WallpaperItem wallpaper)
        {
            try {
                wallpaper.DeletionStatus = "正在删除...";
                var success = await Task.Run(() => {
                    var fileDeleted = _wallpaperFileService.DeleteWallpaperFiles(wallpaper.FolderPath);
                    if (fileDeleted) {
                        _dbManager.DeleteWallpaper(wallpaper.Id);
                    }
                    return fileDeleted;
                });

                if (success) {
                    Log.Information("壁纸删除成功: {Title}", wallpaper.Project.Title);
                    FavoriteWallpapers.Remove(wallpaper);

                    if (SelectedWallpaper == wallpaper) {
                        SelectedWallpaper = null;
                        _dataContextService.CurrentWallpaper = null;
                    }

                    // 同步到 MainViewModel
                    var mainVm = Ioc.Default.GetService<MainViewModel>();
                    if (mainVm != null) {
                        var mainItem = mainVm.Wallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
                        if (mainItem != null) {
                            mainVm.Wallpapers.Remove(mainItem);
                        }
                    }

                    _ = ShowNotification($"壁纸 '{wallpaper.Project.Title}' 已成功删除");
                } else {
                    wallpaper.DeletionStatus = "删除失败";
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"删除壁纸 '{wallpaper.Project.Title}' 失败",
                        Title = "删除错误",
                        ConfirmButtonText = "OK",
                        ShowCancelButton = false,
                        DialogType = DialogType.Error
                    });
                }
            } catch (Exception ex) {
                Log.Error("壁纸删除失败: {Title}, 错误: {Error}", wallpaper.Project.Title, ex.Message);
                wallpaper.DeletionStatus = "删除错误";
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"删除过程中发生错误: {ex.Message}",
                    Title = "删除错误",
                    ConfirmButtonText = "OK",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
            }
        }

        /// <summary>添加到指定合集命令</summary>
        [RelayCommand]
        private async Task AddToSpecificCollection(object parameter)
        {
            if (parameter is not object[] args || args.Length != 2) return;
            if (args[1] is not string collectionId) return;

            List<WallpaperItem> wallpapersToAdd = new List<WallpaperItem>();
            if (SelectedWallpapers.Count > 0)
            {
                wallpapersToAdd.AddRange(SelectedWallpapers);
            }
            else if (args[0] is WallpaperItem wp)
            {
                wallpapersToAdd.Add(wp);
            }

            if (wallpapersToAdd.Count == 0) return;

            try {
                var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null) return;

                int addedCount = 0;
                int alreadyInCollectionCount = 0;
                foreach (var wallpaper in wallpapersToAdd)
                {
                    if (_dbManager.IsInCollection(collectionId, wallpaper.Id)) {
                        alreadyInCollectionCount++;
                        continue;
                    }
                    _dbManager.AddToCollection(collectionId, wallpaper.Id);
                    addedCount++;
                    collection.WallpaperCount++;
                }

                if (addedCount > 0)
                {
                    string message = wallpapersToAdd.Count == 1
                        ? $"已将壁纸「{wallpapersToAdd[0].Project?.Title}」添加到合集「{collection.Name}」。"
                        : $"已将 {addedCount} 个壁纸添加到合集「{collection.Name}」。";
                    if (alreadyInCollectionCount > 0 && wallpapersToAdd.Count > 1)
                        message += $"\n{alreadyInCollectionCount} 个壁纸已存在于合集中。";

                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = message, Title = "成功",
                        ShowCancelButton = false, DialogType = DialogType.Information
                    });
                }
                else if (alreadyInCollectionCount > 0)
                {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"所有选中的壁纸已存在于合集「{collection.Name}」中。",
                        Title = "提示", ShowCancelButton = false, DialogType = DialogType.Information
                    });
                }

                var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                if (collectionVm?.SelectedCollection?.Id == collectionId) {
                    collectionVm.LoadCollectionWallpapers();
                }
            } catch (Exception ex) {
                Log.Warning($"添加壁纸到合集失败: {ex.Message}");
            }
        }

        /// <summary>添加到指定分类命令</summary>
        [RelayCommand]
        private async Task AddToSpecificCategory(object parameter)
        {
            if (parameter is not object[] args || args.Length != 2) return;

            string categoryId;
            if (args[1] is string id)
                categoryId = id;
            else
                categoryId = args[1]?.ToString() ?? "";

            if (string.IsNullOrEmpty(categoryId)) return;

            List<WallpaperItem> wallpapersToAdd = new List<WallpaperItem>();
            if (SelectedWallpapers.Count > 0)
                wallpapersToAdd.AddRange(SelectedWallpapers);
            else if (args[0] is WallpaperItem wp)
                wallpapersToAdd.Add(wp);

            if (wallpapersToAdd.Count == 0) return;

            var targetCategory = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (targetCategory == null) return;

            try
            {
                int addedCount = 0;
                int skippedCount = 0;
                foreach (var wallpaper in wallpapersToAdd)
                {
                    if (wallpaper.CategoryId != CategoryConstants.UNCATEGORIZED_ID)
                    {
                        skippedCount++;
                        continue;
                    }

                    wallpaper.CategoryId = categoryId;
                    wallpaper.Category = targetCategory.Name;
                    _dbManager.UpdateWallpaper(wallpaper);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    string message = wallpapersToAdd.Count == 1
                        ? $"已将壁纸「{wallpapersToAdd[0].Project?.Title}」添加到分类「{targetCategory.Name}」。"
                        : $"已将 {addedCount} 个壁纸添加到分类「{targetCategory.Name}」。";
                    if (skippedCount > 0 && wallpapersToAdd.Count > 1)
                        message += $"\n{skippedCount} 个壁纸已有分类，已跳过。";

                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = message, Title = "成功",
                        ShowCancelButton = false, DialogType = DialogType.Information
                    });

                    LoadCustomCategories();
                    FavoriteWallpapersView.Refresh();

                    // 刷新详情页
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm?.CurrentWallpaper != null)
                    {
                        foreach (var wp2 in wallpapersToAdd)
                        {
                            if (wp2.Id == detailVm.CurrentWallpaper.Id)
                            {
                                _dataContextService.CurrentWallpaper = null;
                                _dataContextService.CurrentWallpaper = wp2;
                                break;
                            }
                        }
                    }
                }
                else if (skippedCount > 0)
                {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"所有选中的壁纸已有分类，无需操作。",
                        Title = "提示", ShowCancelButton = false, DialogType = DialogType.Information
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"添加壁纸到分类失败: {ex.Message}");
            }
        }

        // ==================== 选择 ====================

        /// <summary>选择壁纸命令</summary>
        [RelayCommand]
        private void SelectWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                UpdateSelection(wallpaper);
            }
        }

        private void UpdateSelection(WallpaperItem selectedWallpaper)
        {
            foreach (var wallpaper in FavoriteWallpapers.Where(w => w.IsSelected)) {
                wallpaper.IsSelected = false;
            }

            selectedWallpaper.IsSelected = true;
            SelectedWallpaper = selectedWallpaper;
            _dataContextService.CurrentWallpaper = selectedWallpaper;
        }

        public void HandleWallpaperSelection(WallpaperItem wallpaper, bool isCtrlPressed, bool isShiftPressed)
        {
            if (wallpaper == null) return;

            if (!isCtrlPressed && !isShiftPressed)
            {
                ClearSelection();
                AddToSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = wallpaper;
                return;
            }

            if (isCtrlPressed && !isShiftPressed)
            {
                ToggleSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = SelectedWallpapers.LastOrDefault();
                return;
            }

            if (isShiftPressed && !isCtrlPressed)
            {
                if (_lastSelectedItem == null)
                {
                    ClearSelection();
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                int lastIndex = FavoriteWallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = FavoriteWallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                ClearSelection();
                for (int i = start; i <= end; i++)
                {
                    AddToSelection(FavoriteWallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                return;
            }

            if (isCtrlPressed && isShiftPressed)
            {
                if (_lastSelectedItem == null)
                {
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                int lastIndex = FavoriteWallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = FavoriteWallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                for (int i = start; i <= end; i++)
                {
                    AddToSelection(FavoriteWallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                return;
            }
        }

        private void ClearSelection()
        {
            foreach (var item in SelectedWallpapers.ToList())
            {
                item.IsSelected = false;
            }
            SelectedWallpapers.Clear();
        }

        private void AddToSelection(WallpaperItem wallpaper)
        {
            if (!SelectedWallpapers.Contains(wallpaper))
            {
                SelectedWallpapers.Add(wallpaper);
                wallpaper.IsSelected = true;
            }
        }

        private void RemoveFromSelection(WallpaperItem wallpaper)
        {
            if (SelectedWallpapers.Contains(wallpaper))
            {
                SelectedWallpapers.Remove(wallpaper);
                wallpaper.IsSelected = false;
            }
        }

        private void ToggleSelection(WallpaperItem wallpaper)
        {
            if (SelectedWallpapers.Contains(wallpaper))
                RemoveFromSelection(wallpaper);
            else
                AddToSelection(wallpaper);
        }

        // ==================== 分类管理 ====================

        private async Task LoadCustomCategoriesAsync()
        {
            try {
                var allCategories = await _categoryService.GetAllCategoriesAsync();
                string savedCategoryId = SelectedCategoryId;

                _updatingSelection = true;
                try
                {
                    Categories.Clear();
                    MenuCategories.Clear();
                    foreach (var category in allCategories)
                    {
                        Categories.Add(category);
                        if (!CategoryConstants.IsVirtualCategoryId(category.Id))
                        {
                            MenuCategories.Add(category);
                        }
                    }
                }
                finally
                {
                    _updatingSelection = false;
                }

                SelectedCategoryId = savedCategoryId;
                var matchedCategory = Categories.FirstOrDefault(c => c.Id == savedCategoryId);
                SelectedCategory = matchedCategory;
            } catch (Exception ex) {
                Log.Error($"加载分类失败: {ex.Message}");
            }
        }

        private void LoadCustomCategories()
        {
            _ = LoadCustomCategoriesAsync();
        }

        private void LoadCollections()
        {
            try {
                var collections = _dbManager.GetAllCollections();
                Collections.Clear();
                foreach (var collection in collections) {
                    Collections.Add(collection);
                }
            } catch (Exception ex) {
                Log.Warning($"加载合集列表失败: {ex.Message}");
            }
        }

        public void RefreshCollections()
        {
            LoadCollections();
        }

        private async void OnCategoryChanged(object? sender, CategoryChangedEventArgs e)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                if (e.ChangeType == CategoryChangeType.Renamed && !string.IsNullOrEmpty(e.OldCategoryName) && !string.IsNullOrEmpty(e.CategoryName))
                {
                    foreach (var wallpaper in FavoriteWallpapers)
                    {
                        if (wallpaper.Category == e.OldCategoryName)
                        {
                            wallpaper.Category = e.CategoryName;
                            wallpaper.Project.Category = e.CategoryName;
                        }
                    }
                }

                if (e.ChangeType == CategoryChangeType.Deleted && !string.IsNullOrEmpty(e.CategoryName))
                {
                    foreach (var wallpaper in FavoriteWallpapers)
                    {
                        if (wallpaper.CategoryId == e.CategoryId)
                        {
                            wallpaper.CategoryId = CategoryConstants.UNCATEGORIZED_ID;
                            wallpaper.Category = "未分类";
                            wallpaper.Project.Category = "未分类";
                        }
                    }
                }

                await LoadCustomCategoriesAsync();
                FavoriteWallpapersView.Refresh();
                OnPropertyChanged(nameof(WallpaperCount));
            });
        }

        public async void OnCategoryAdded(object? sender, string category)
        {
            try
            {
                if (!Categories.Any(c => c.Name == category)) {
                    var categoryId = await _categoryService.AddCategoryAsync(category);
                    if (!string.IsNullOrEmpty(categoryId))
                    {
                        var categoryItem = await _categoryService.GetCategoryByIdAsync(categoryId);
                        if (categoryItem != null)
                        {
                            int insertIndex = 2;
                            if (insertIndex <= Categories.Count)
                                Categories.Insert(insertIndex, categoryItem);
                            else
                                Categories.Add(categoryItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"处理新增分类事件失败: {ex.Message}");
            }
        }

        /// <summary>选中壁纸变更时同步到数据上下文服务</summary>
        partial void OnSelectedWallpaperChanged(WallpaperItem? value)
        {
            _dataContextService.CurrentWallpaper = value;
        }

        private async Task ShowNotification(string message)
        {
            await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                Message = message,
                Title = "通知",
                ConfirmButtonText = "OK",
                ShowCancelButton = false,
                DialogType = DialogType.Information
            });
        }
    }
}
