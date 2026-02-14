using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class WallpaperDetailViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;
        private readonly IDataContextService _dataContextService;
        private WallpaperItem _originalItem;
        [ObservableProperty]
        private WallpaperItem? _currentWallpaper;
        [ObservableProperty]
        private bool _isEditMode;
        [ObservableProperty]
        private string _editStatus = "就绪";
        [ObservableProperty]
        private string _selectedType;
        [ObservableProperty]
        private string _description;
        [ObservableProperty]
        private string _title;
        [ObservableProperty]
        private string _previewFileName;
        [ObservableProperty]
        private string _contentFileName;

        // 类型列表数据源
        public List<string> WallpaperTypes { get; } = new List<string>
        {
            "scene",
            "video",
            "web",
            "application"
        };

        public IRelayCommand StartEditCommand { get; }
        public IAsyncRelayCommand SaveEditCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IAsyncRelayCommand<string?> SetPreviewFileNameCommand { get; }
        public IAsyncRelayCommand<string?> SetContentFileNameCommand { get; }

        public WallpaperDetailViewModel(IDataContextService dataContextService)
        {
            _dbManager = Ioc.Default.GetRequiredService<DatabaseManager>();
            _dataContextService = dataContextService;
            // 订阅状态变化事件
            _dataContextService.CurrentWallpaperChanged += OnCurrentWallpaperChanged;

            // 初始化命令
            StartEditCommand = new RelayCommand(StartEdit);
            SaveEditCommand = new AsyncRelayCommand(SaveEdit, CanSaveEdit);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SetPreviewFileNameCommand = new AsyncRelayCommand<string?>(SetPreviewFileName, CanSetPreviewFileName);
            SetContentFileNameCommand = new AsyncRelayCommand<string?>(SetContentFileName, CanSetContentFileName);
        }

        private void OnCurrentWallpaperChanged(object? sender, WallpaperItem? newWallpaper)
        {
            // 当服务中的状态改变时，更新自己的数据
            CurrentWallpaper = newWallpaper;
            _ = LoadFileListSafeAsync(newWallpaper);
            SelectedType = CurrentWallpaper?.Project?.Type;
            Description = CurrentWallpaper?.Project?.Description;
            Title = CurrentWallpaper?.Project?.Title;
            PreviewFileName = CurrentWallpaper?.Project?.Preview;
            ContentFileName = CurrentWallpaper?.Project?.File;
        }

        public void Initialize(WallpaperItem? wallpaper = null)
        {
            if (wallpaper == null) return;
            CurrentWallpaper = wallpaper;
            _originalItem = CreateBackup(wallpaper);
        }

        // 创建数据备份
        private WallpaperItem CreateBackup(WallpaperItem source)
        {
            return new WallpaperItem {
                Id = source.Id,
                FolderPath = source.FolderPath,
                Project = new WallpaperProject {
                    Title = source.Project.Title,
                    Description = source.Project.Description,
                    File = source.Project.File,
                    Preview = source.Project.Preview,
                    Type = source.Project.Type.ToLower(),
                    Tags = new List<string>(source.Project.Tags ?? new List<string>()),
                    ContentRating = source.Project.ContentRating,
                    Visibility = source.Project.Visibility
                },
                IsFavorite = source.IsFavorite
            };
        }

        // 从备份恢复数据
        private void RestoreFromBackup(WallpaperItem target, WallpaperItem backup)
        {
            target.Project.Title = backup.Project.Title;
            target.Project.Description = backup.Project.Description;
            target.Project.File = backup.Project.File;
            target.Project.Preview = backup.Project.Preview;
            target.Project.Type = backup.Project.Type;
            target.Project.Tags = new List<string>(backup.Project.Tags ?? new List<string>());
            target.Project.ContentRating = backup.Project.ContentRating;
            target.Project.Visibility = backup.Project.Visibility;
            target.IsFavorite = backup.IsFavorite;
        }

        private async Task LoadFileListSafeAsync(WallpaperItem? wallpaper)
        {
            if (wallpaper == null) return;
            try {
                await wallpaper.LoadFileListAsync();
            } catch (Exception ex) {
                Log.Warning($"加载文件列表失败: {ex.Message}");
            }
        }

        private void ShowSaveSuccessMessage()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await MaterialDialogService.ShowConfirmationAsync("壁纸详情已成功保存!", "保存成功");
            });
        }

        private void ShowErrorMessage(string message)
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await MaterialDialogService.ShowErrorAsync(message, "错误");
            });
        }
    }
}
