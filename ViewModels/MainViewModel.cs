using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型，负责壁纸列表展示、搜索、筛选、扫描等核心功能
    /// </summary>
    public partial class MainViewModel : ObservableObject {
        /// <summary>数据库管理器</summary>
        public readonly DatabaseManager _dbManager;
        private readonly WallpaperScanner _scanner;
        private readonly PreviewService _previewService;
        private readonly ISettingsService _settingsService;
        private readonly IDataContextService _dataContextService;
        private readonly IWallpaperFileService _wallpaperFileService;

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

        /// <summary>搜索文本</summary>
        [ObservableProperty]
        private string _searchText = string.Empty;

        /// <summary>当前选中的分类</summary>
        [ObservableProperty]
        private string _selectedCategory = "所有分类";

        /// <summary>是否仅显示收藏的壁纸</summary>
        [ObservableProperty]
        private bool _showFavoritesOnly;

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
        public ObservableCollection<string> Categories { get; } = new ObservableCollection<string>
        {
            "所有分类", "未分类", "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        /// <summary>壁纸合集列表，用于右键菜单快速添加</summary>
        public ObservableCollection<WallpaperCollection> Collections { get; } = new ObservableCollection<WallpaperCollection>();

        /// <summary>
        /// 初始化主视图模型，注入服务并加载初始数据
        /// </summary>
        /// <param name="dataContextService">数据上下文服务，用于跨视图共享壁纸选中状态</param>
        /// <param name="wallpaperFileService">壁纸文件服务，用于文件系统操作</param>
        public MainViewModel(IDataContextService dataContextService, IWallpaperFileService wallpaperFileService)
        {
            _dbManager = Ioc.Default.GetService<DatabaseManager>();
            _scanner = new WallpaperScanner(_dbManager!);

            WallpapersView = CollectionViewSource.GetDefaultView(Wallpapers);
            WallpapersView.Filter = FilterWallpapers;

            Wallpapers.CollectionChanged += OnWallpapersCollectionChanged;

            SelectedCategory = "所有分类";
            ShowFavoritesOnly = false;
            SearchText = string.Empty;
            CheckLastScanTime();
            LoadScanHistory();

            _settingsService = Ioc.Default.GetService<ISettingsService>();
            _settings = _settingsService.LoadSettings();
            _previewService = new PreviewService(_settingsService);
            _dataContextService = dataContextService;
            _wallpaperFileService = wallpaperFileService;

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
        /// 从数据库加载自定义分类并添加到分类列表中
        /// </summary>
        private void LoadCustomCategories()
        {
            try {
                var customCategories = _dbManager.GetCustomCategories();
                foreach (var category in customCategories) {
                    if (!Categories.Contains(category)) {
                        Categories.Add(category);
                    }
                }
            } catch (Exception ex) {
                Log.Warning($"加载自定义分类失败: {ex.Message}");
            }
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
                Log.Warning($"加载合集列表失败: {ex.Message}");
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
        /// 详情页新增分类时的回调，同步分类到主视图分类列表
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="category">新增的分类名称</param>
        public void OnCategoryAdded(object? sender, string category)
        {
            if (!Categories.Contains(category)) {
                Categories.Add(category);
            }
        }

        private void OnWallpapersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Log.Debug("OnWallpapersCollectionChanged," + e.ToString());
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
            try {
                var wallpapers = await Task.Run(() => LoadWallpapers());
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
                    Wallpapers.Clear();
                    if (wallpapers != null) {
                        foreach (var wallpaper in wallpapers) {
                            Wallpapers.Add(wallpaper);
                        }
                    }
                });

                // 加载壁纸总数
                await LoadTotalWallpaperCountAsync();

                OnEventLoadWallpapersCompleted();
            } catch (Exception ex) {
                Log.Fatal($"加载壁纸列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从数据库加载壁纸列表，根据当前搜索和筛选条件查询
        /// </summary>
        /// <returns>壁纸项列表，加载失败时返回null</returns>
        private async Task<List<WallpaperItem>> LoadWallpapers()
        {
            List<WallpaperItem> wallpapers = new();
            try {
                Log.Debug("LoadWallpapers from db start");
                var results = _dbManager.SearchWallpapers(SearchText,
                    SelectedCategory == "所有分类" ? "" : SelectedCategory,
                    ShowFavoritesOnly);
                Log.Debug("LoadWallpapers from db finish");
                return results;
            } catch (Exception ex) {
                 await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message =  $"加载壁纸列表失败: {ex.Message}", Title = "错误",
                    ShowCancelButton =false, DialogType = DialogType.Error
                 });
                return null;
            }
        }

        /// <summary>
        /// 异步加载数据库中的壁纸总数
        /// </summary>
        private async Task LoadTotalWallpaperCountAsync()
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
                Log.Warning($"加载壁纸总数失败: {ex.Message}");
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
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1) {
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
    }
}
