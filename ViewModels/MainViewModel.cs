using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Data;
using WallpaperEngine.Data;
using WallpaperEngine.Events;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型，负责壁纸列表展示、搜索、筛选、扫描等核心功能
    /// </summary>
    public partial class MainViewModel : ObservableObject {
        /// <summary>数据库管理器</summary>
        private readonly DatabaseManager _dbManager;
        private readonly WallpaperScanner _scanner;
        private readonly PreviewService _previewService;
        private readonly ISettingsService _settingsService;
        private readonly IDataContextService _dataContextService;
        private readonly IWallpaperFileService _wallpaperFileService;
        private readonly ICategoryService _categoryService;

        /// <summary>用于取消上一次壁纸加载操作，防止并发加载导致界面空白</summary>
        private CancellationTokenSource? _loadWallpapersCts;

        /// <summary>待删除的壁纸项</summary>
        [ObservableProperty]
        private WallpaperItem? _itemPendingDeletion;

        /// <summary>应用程序设置</summary>
        [ObservableProperty]
        private ApplicationSettings _settings;

        /// <summary>壁纸列表集合</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _wallpapers = new();

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

        /// <summary>当前选中的分类ID（"00000000-0000-0000-0000-000000000000"表示"所有分类"）</summary>
        [ObservableProperty]
        private string _selectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;

        /// <summary>当前选中的分类项</summary>
        [ObservableProperty]
        private CategoryItem? _selectedCategory;

        /// <summary>是否隐藏成人内容（ContentRating 为 Mature 或 Questionable）</summary>
        [ObservableProperty]
        private bool _hideAdultContent;

        /// <summary>当前选中的标签页索引</summary>
        [ObservableProperty]
        private int _currentTab;

        /// <summary>是否正在扫描壁纸</summary>
        [ObservableProperty]
        private bool _isScanning;

        /// <summary>扫描进度百分比</summary>
        [ObservableProperty]
        private int _scanProgress;

        /// <summary>扫描状态描述文本</summary>
        [ObservableProperty]
        private string _scanStatus = "准备扫描";

        /// <summary>当前正在扫描的文件夹路径</summary>
        [ObservableProperty]
        private string _currentScanFolder = string.Empty;

        /// <summary>已扫描的壁纸数量</summary>
        [ObservableProperty]
        private int _scannedCount;

        /// <summary>壁纸总数量</summary>
        [ObservableProperty]
        private int _totalCount;

        /// <summary>数据库中的壁纸总数（不考虑筛选）</summary>
        [ObservableProperty]
        private int _totalWallpaperCount;

        /// <summary>上次扫描时间的显示文本</summary>
        [ObservableProperty]
        private string _lastScanTime = "从未扫描";

        /// <summary>进度条是否为不确定模式</summary>
        [ObservableProperty]
        private bool _isIndeterminate;

        /// <summary>扫描历史记录集合</summary>
        [ObservableProperty]
        private ObservableCollection<DatabaseManager.ScanInfo> _scanHistory = new();

        /// <summary>本次扫描新发现的壁纸数量</summary>
        [ObservableProperty]
        private int _newFoundCount;

        /// <summary>本次扫描更新的壁纸数量</summary>
        [ObservableProperty]
        private int _updatedCount;

        /// <summary>本次扫描跳过的壁纸数量</summary>
        [ObservableProperty]
        private int _skippedCount;

        /// <summary>壁纸列表的集合视图，支持筛选和排序</summary>
        public ICollectionView WallpapersView { get; }

        /// <summary>壁纸分类列表，包含默认分类和自定义分类</summary>
        public ObservableCollection<CategoryItem> Categories { get; } = new ObservableCollection<CategoryItem>();

        /// <summary>壁纸合集列表，用于右键菜单快速添加</summary>
        public ObservableCollection<WallpaperCollection> Collections { get; } = new ObservableCollection<WallpaperCollection>();

        /// <summary>右键菜单用的分类列表（排除虚拟分类：所有分类、未分类）</summary>
        public ObservableCollection<CategoryItem> MenuCategories { get; } = new ObservableCollection<CategoryItem>();

        /// <summary>
        /// 初始化主视图模型，注入服务并加载初始数据
        /// </summary>
        /// <param name="dataContextService">数据上下文服务，用于跨视图共享壁纸选中状态</param>
        /// <param name="wallpaperFileService">壁纸文件服务，用于文件系统操作</param>
        /// <param name="categoryService">分类服务</param>
        public MainViewModel(IDataContextService dataContextService, IWallpaperFileService wallpaperFileService, ICategoryService categoryService)
        {
            _dbManager = Ioc.Default.GetService<DatabaseManager>();
            _scanner = new WallpaperScanner(_dbManager!);

            WallpapersView = CollectionViewSource.GetDefaultView(Wallpapers);
            WallpapersView.Filter = FilterWallpapers;

            Wallpapers.CollectionChanged += OnWallpapersCollectionChanged;

            SelectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;
            SearchText = string.Empty;
            CheckLastScanTime();
            LoadScanHistory();

            _settingsService = Ioc.Default.GetService<ISettingsService>();
            _settings = _settingsService.LoadSettings();
            _previewService = new PreviewService(_settingsService);
            _dataContextService = dataContextService;
            _wallpaperFileService = wallpaperFileService;
            _categoryService = categoryService;

            // 订阅分类变更事件
            _categoryService.CategoryChanged += OnCategoryChanged;

            // 加载自定义分类
            LoadCustomCategories();

            // 加载合集列表
            LoadCollections();

            // 订阅详情页新增分类事件
            var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
            if (detailVm != null) {
                detailVm.CategoryAdded += OnCategoryAdded;
            }
        }

        /// <summary>
        /// 从数据库加载所有分类（包括默认分类）并同步到分类列表
        /// </summary>
        private async Task LoadCustomCategoriesAsync()
        {
            try {
                // 从分类服务获取所有分类（包括虚拟分类和数据库分类）
                var allCategories = await _categoryService.GetAllCategoriesAsync();

                // 保存当前选中的分类ID，防止Clear触发绑定重置
                string savedCategoryId = SelectedCategoryId;

                // 防止Clear/Add过程中的绑定联动重置SelectedCategoryId
                _updatingSelection = true;
                try
                {
                    // 清空并更新分类列表
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

                // 恢复选中分类
                SelectedCategoryId = savedCategoryId;
                var matchedCategory = Categories.FirstOrDefault(c => c.Id == savedCategoryId);
                SelectedCategory = matchedCategory;
            } catch (Exception ex) {
                Log.Error(ex, "加载分类失败");
            }
        }

        /// <summary>
        /// 同步包装方法，用于向后兼容
        /// </summary>
        private void LoadCustomCategories()
        {
            // 异步加载，不等待完成
            _ = LoadCustomCategoriesAsync();
        }

        /// <summary>
        /// 公共方法：刷新分类列表（供其他视图模型调用）
        /// </summary>
        public void RefreshCategoryList()
        {
            LoadCustomCategories();
        }

        /// <summary>
        /// 从数据库加载合集列表
        /// </summary>
        private void LoadCollections()
        {
            try {
                var collections = _dbManager.GetAllCollections();
                Collections.Clear();
                foreach (var collection in collections) {
                    Collections.Add(collection);
                }
            } catch (Exception ex) {
                Log.Warning(ex, "加载合集列表失败");
            }
        }

        /// <summary>
        /// 刷新合集列表，在合集视图添加新合集时调用
        /// </summary>
        public void RefreshCollections()
        {
            LoadCollections();
        }

        /// <summary>
        /// 分类变更事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void OnCategoryChanged(object? sender, CategoryChangedEventArgs e)
        {
            try
            {
            // 当分类发生变化时，刷新分类列表
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                // 分类重命名时，更新所有该分类下壁纸的分类名称
                if (e.ChangeType == CategoryChangeType.Renamed && !string.IsNullOrEmpty(e.OldCategoryName) && !string.IsNullOrEmpty(e.CategoryName))
                {
                    foreach (var wallpaper in Wallpapers)
                    {
                        if (wallpaper.Category == e.OldCategoryName)
                        {
                            wallpaper.Category = e.CategoryName;
                            wallpaper.Project.Category = e.CategoryName;
                        }
                    }
                }

                // 分类删除时，将该分类下所有壁纸重置为"未分类"
                if (e.ChangeType == CategoryChangeType.Deleted && !string.IsNullOrEmpty(e.CategoryName))
                {
                    foreach (var wallpaper in Wallpapers)
                    {
                        if (wallpaper.CategoryId == e.CategoryId)
                        {
                            wallpaper.CategoryId = CategoryConstants.UNCATEGORIZED_ID;
                            wallpaper.Category = "未分类";
                            wallpaper.Project.Category = "未分类";
                        }
                    }
                }

                // 等待分类列表加载完成后再刷新视图
                await LoadCustomCategoriesAsync();
                WallpapersView.Refresh();
                OnPropertyChanged(nameof(WallpaperCount));
            });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理分类变更事件失败");
            }
        }

        /// <summary>
        /// 详情页新增分类时的回调，同步分类到主视图分类列表
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="category">新增的分类名称</param>
        public async void OnCategoryAdded(object? sender, string category)
        {
            try
            {
                // 检查分类是否已存在（按名称）
                if (!Categories.Any(c => c.Name == category)) {
                    // 使用分类服务添加分类
                    var categoryId = await _categoryService.AddCategoryAsync(category);

                    if (!string.IsNullOrEmpty(categoryId))
                    {
                        // 获取分类项
                        var categoryItem = await _categoryService.GetCategoryByIdAsync(categoryId);
                        if (categoryItem != null)
                        {
                            // 添加到分类列表（插入到虚拟分类之后）
                            // 找到插入位置（在虚拟分类之后）
                            int insertIndex = 2; // "所有分类"(0) 和 "未分类"(1) 之后
                            if (insertIndex <= Categories.Count)
                            {
                                Categories.Insert(insertIndex, categoryItem);
                            }
                            else
                            {
                                Categories.Add(categoryItem);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "处理新增分类事件失败");
            }
        }

        private void OnWallpapersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(WallpaperCount));
        }

        /// <summary>当前筛选条件下的壁纸总数</summary>
        public int WallpaperCount => Wallpapers.Count(FilterWallpapers);

        /// <summary>壁纸加载完成事件</summary>
        public event EventHandler LoadWallpapersCompleted;

        /// <summary>
        /// 触发壁纸加载完成事件
        /// </summary>
        public void OnEventLoadWallpapersCompleted()
        {
            LoadWallpapersCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 异步加载壁纸列表，从数据库读取数据并更新UI集合
        /// </summary>
        public async Task LoadWallpapersAsync()
        {
            // 取消上一次未完成的加载操作，防止并发加载导致界面空白
            var oldCts = _loadWallpapersCts;
            oldCts?.Cancel();
            oldCts?.Dispose();
            var cts = new CancellationTokenSource();
            _loadWallpapersCts = cts;

            try {
                // 在UI线程上捕获当前筛选状态，避免后台线程读取到变化中的值
                string searchText = SearchText;
                string categoryId = SelectedCategoryId;
                bool hideAdult = HideAdultContent;

                var wallpapers = await Task.Run(() => {
                    cts.Token.ThrowIfCancellationRequested();
                    return LoadWallpapers(searchText, categoryId, hideAdult);
                }, cts.Token);

                cts.Token.ThrowIfCancellationRequested();

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    if (cts.Token.IsCancellationRequested) return;

                    Wallpapers.Clear();
                    if (wallpapers != null) {
                        foreach (var wallpaper in wallpapers) {
                            Wallpapers.Add(wallpaper);
                        }
                    }
                    WallpapersView.Refresh();
                });

                if (cts.Token.IsCancellationRequested) return;

                // 加载壁纸总数
                await LoadTotalWallpaperCountAsync();

                // 通知 FavoriteViewModel 刷新数据
                var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
                if (favoriteVm != null)
                {
                    await favoriteVm.LoadFavoritesAsync();
                }

                OnEventLoadWallpapersCompleted();
            } catch (OperationCanceledException) {
                // 加载被取消，忽略
            } catch (Exception ex) {
                Log.Error(ex, "加载壁纸列表失败");
            }
        }

        /// <summary>
        /// 从数据库加载壁纸列表，使用传入的筛选条件查询
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="categoryId">分类ID</param>
        /// <param name="hideAdultContent">是否隐藏成人内容</param>
        /// <returns>壁纸项列表，加载失败时返回null</returns>
        private async Task<List<WallpaperItem>> LoadWallpapers(string searchText, string categoryId, bool hideAdultContent)
        {
            List<WallpaperItem> wallpapers = new();
            try {
                Log.Debug("LoadWallpapers from db start");
                var results = _dbManager.SearchWallpapers(searchText,
                    categoryId,
                    false,
                    hideAdultContent);
                Log.Debug("LoadWallpapers from db finish");
                return results;
            } catch (Exception ex) {
                 await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message =  $"加载壁纸列表失败: {ex.Message}", Title = "错误",
                    ShowCancelButton =false, DialogType = DialogType.Error
                 });
                return new List<WallpaperItem>();
            }
        }

        /// <summary>
        /// 异步加载数据库中的壁纸总数
        /// </summary>
        public async Task LoadTotalWallpaperCountAsync()
        {
            try
            {
                var count = await Task.Run(() => _dbManager.GetTotalWallpaperCount());
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    TotalWallpaperCount = count;
                });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "加载壁纸总数失败");
                // 不设置值，保持默认0
            }
        }

        /// <summary>
        /// 格式化文件大小为可读字符串（如 1.5 MB）
        /// </summary>
        /// <param name="bytes">文件大小（字节）</param>
        /// <returns>格式化后的文件大小字符串</returns>
        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (counter < suffixes.Length - 1 && Math.Round(number / 1024) >= 1) {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        /// <summary>
        /// 显示通知消息对话框
        /// </summary>
        /// <param name="message">通知内容</param>
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

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(Wallpapers)) {
                SelectedWallpaper = null;
            }
        }

        /// <summary>选中壁纸变更时同步到数据上下文服务</summary>
        partial void OnSelectedWallpaperChanged(WallpaperItem? value)
        {
            _dataContextService.CurrentWallpaper = value;
        }

        /// <summary>
        /// 保存最后应用的壁纸信息到设置文件
        /// </summary>
        /// <param name="type">壁纸类型："dynamic" 或 "static"</param>
        /// <param name="path">壁纸路径</param>
        public void SaveLastWallpaper(string type, string path)
        {
            try {
                Settings.LastWallpaperType = type;
                Settings.LastWallpaperPath = path;
                _settingsService.SaveSettings(Settings);
                Log.Information("已保存最后应用的壁纸: Type={Type}, Path={Path}", type, path);
            } catch (Exception ex) {
                Log.Warning(ex, "保存最后应用的壁纸信息失败");
            }
        }

        /// <summary>
        /// 启动时恢复最后应用的壁纸
        /// </summary>
        public void RestoreLastWallpaper()
        {
            try {
                var settings = _settingsService.LoadSettings();
                string type = settings.LastWallpaperType;
                string path = settings.LastWallpaperPath;

                if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(path)) {
                    Log.Debug("没有保存的壁纸记录，跳过恢复");
                    return;
                }

                if (type == "dynamic") {
                    string projectJsonPath = Path.Combine(path, "project.json");
                    string toolPath = settings.WallpaperEnginePath;

                    if (string.IsNullOrEmpty(toolPath) || !File.Exists(toolPath)) {
                        Log.Warning("恢复壁纸失败: Wallpaper Engine 路径未配置或不存在: {Path}", toolPath);
                        return;
                    }
                    if (!File.Exists(projectJsonPath)) {
                        Log.Warning("恢复壁纸失败: project.json 不存在: {Path}", projectJsonPath);
                        return;
                    }

                    ProcessStartInfo startInfo = new ProcessStartInfo {
                        FileName = toolPath,
                        UseShellExecute = false
                    };
                    startInfo.ArgumentList.Add("-control");
                    startInfo.ArgumentList.Add("openWallpaper");
                    startInfo.ArgumentList.Add("-file");
                    startInfo.ArgumentList.Add(projectJsonPath);
                    Process.Start(startInfo)?.Dispose();
                    Log.Information("已恢复动态壁纸: {Path}", path);
                } else if (type == "static") {
                    if (!File.Exists(path)) {
                        Log.Warning("恢复壁纸失败: 静态壁纸文件不存在: {Path}", path);
                        return;
                    }

                    bool success = DesktopWallpaperService.SetDesktopWallpaper(path);
                    if (success) {
                        Log.Information("已恢复静态壁纸: {Path}", path);
                    }
                } else {
                    Log.Warning("未知的壁纸类型: {Type}", type);
                }
            } catch (Exception ex) {
                Log.Warning(ex, "恢复最后应用的壁纸失败");
            }
        }
    }
}
