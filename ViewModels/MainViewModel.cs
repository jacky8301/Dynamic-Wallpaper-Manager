using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Serilog;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class MainViewModel : ObservableObject {
        public readonly DatabaseManager _dbManager;
        private readonly WallpaperScanner _scanner;
        private readonly PreviewService _previewService;
        private readonly ISettingsService _settingsService;
        private readonly IDataContextService _dataContextService;
        private readonly IWallpaperFileService _wallpaperFileService;

        [ObservableProperty]
        private WallpaperItem _itemPendingDeletion;

        [ObservableProperty]
        private ApplicationSettings _settings;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _wallpapers = new();

        [ObservableProperty]
        private WallpaperItem _selectedWallpaper;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "所有分类";

        [ObservableProperty]
        private bool _showFavoritesOnly;

        [ObservableProperty]
        private int _currentTab;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private int _scanProgress;

        [ObservableProperty]
        private string _scanStatus = "准备扫描";

        [ObservableProperty]
        private string _currentScanFolder = string.Empty;

        [ObservableProperty]
        private int _scannedCount;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private string _lastScanTime = "从未扫描";

        [ObservableProperty]
        private bool _isIndeterminate;

        [ObservableProperty]
        private ObservableCollection<DatabaseManager.ScanInfo> _scanHistory = new();

        [ObservableProperty]
        private int _newFoundCount;

        [ObservableProperty]
        private int _updatedCount;

        [ObservableProperty]
        private int _skippedCount;

        public ICollectionView WallpapersView { get; }

        public List<string> Categories { get; } = new List<string>
        {
            "所有分类", "未分类", "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        public MainViewModel(IDataContextService dataContextService, IWallpaperFileService wallpaperFileService)
        {
            _dbManager = Ioc.Default.GetService<DatabaseManager>();
            _scanner = new WallpaperScanner(_dbManager);

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
        }

        private void OnWallpapersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Log.Debug("OnWallpapersCollectionChanged," + e.ToString());
            OnPropertyChanged(nameof(WallpaperCount));
        }

        public int WallpaperCount => Wallpapers.Count;

        public event EventHandler LoadWallpapersCompleted;

        public void OnEventLoadWallpapersCompleted()
        {
            LoadWallpapersCompleted?.Invoke(this, EventArgs.Empty);
        }

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
                OnEventLoadWallpapersCompleted();
            } catch (Exception ex) {
                Log.Fatal($"加载壁纸列表失败: {ex.Message}");
            }
        }

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
