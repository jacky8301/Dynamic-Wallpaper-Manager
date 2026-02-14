using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
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
        [ObservableProperty]
        private string _selectedCategory;

        // 标签编辑集合
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();

        // 类型列表数据源
        public List<string> WallpaperTypes { get; } = new List<string>
        {
            "scene",
            "video",
            "web",
            "application"
        };

        // 分类列表数据源
        public ObservableCollection<string> CategoryList { get; } = new ObservableCollection<string>
        {
            "未分类", "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        public IRelayCommand StartEditCommand { get; }
        public IAsyncRelayCommand SaveEditCommand { get; }
        public IRelayCommand CancelEditCommand { get; }
        public IAsyncRelayCommand<string?> SetPreviewFileNameCommand { get; }
        public IAsyncRelayCommand<string?> SetContentFileNameCommand { get; }
        public IAsyncRelayCommand AddCategoryCommand { get; }
        public IAsyncRelayCommand AddTagCommand { get; }
        public IRelayCommand<string> RemoveTagCommand { get; }

        // 新增分类事件，通知 MainViewModel 同步
        public event EventHandler<string>? CategoryAdded;

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
            AddCategoryCommand = new AsyncRelayCommand(AddCategory);
            AddTagCommand = new AsyncRelayCommand(AddTag);
            RemoveTagCommand = new RelayCommand<string>(RemoveTag);

            // 加载自定义分类
            LoadCustomCategories();
        }

        private void OnCurrentWallpaperChanged(object? sender, WallpaperItem? newWallpaper)
        {
            // 当服务中的状态改变时，更新自己的数据
            CurrentWallpaper = newWallpaper;
            _ = LoadFileListSafeAsync(newWallpaper);
            SelectedType = CurrentWallpaper?.Project?.Type;
            SelectedCategory = CurrentWallpaper?.Category;
            Description = CurrentWallpaper?.Project?.Description;
            Title = CurrentWallpaper?.Project?.Title;
            PreviewFileName = CurrentWallpaper?.Project?.Preview;
            ContentFileName = CurrentWallpaper?.Project?.File;
            SyncTagsFromProject();
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
                    Visibility = source.Project.Visibility,
                    Category = source.Project.Category
                },
                IsFavorite = source.IsFavorite,
                Category = source.Category
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
            target.Project.Category = backup.Project.Category;
            target.IsFavorite = backup.IsFavorite;
            target.Category = backup.Category;
        }

        private void SyncTagsFromProject()
        {
            Tags.Clear();
            var projectTags = CurrentWallpaper?.Project?.Tags;
            if (projectTags == null) return;
            foreach (var tag in projectTags) {
                Tags.Add(tag);
            }
        }

        private async Task AddTag()
        {
            var result = await MaterialDialogService.ShowInputAsync("新增标签", "请输入标签名称:", "标签名称");
            if (!result.Confirmed || result.Data is not string name || string.IsNullOrWhiteSpace(name)) return;

            if (Tags.Contains(name)) {
                await MaterialDialogService.ShowErrorAsync("该标签已存在", "提示");
                return;
            }

            Tags.Add(name);
        }

        private void RemoveTag(string? tag)
        {
            if (tag != null) {
                Tags.Remove(tag);
            }
        }

        private void LoadCustomCategories()
        {
            try {
                var customCategories = _dbManager.GetCustomCategories();
                foreach (var category in customCategories) {
                    if (!CategoryList.Contains(category)) {
                        CategoryList.Add(category);
                    }
                }
            } catch (Exception ex) {
                Log.Warning($"加载自定义分类失败: {ex.Message}");
            }
        }

        private async Task AddCategory()
        {
            var result = await MaterialDialogService.ShowInputAsync("新增分类", "请输入分类名称:", "分类名称");
            if (!result.Confirmed || result.Data is not string name || string.IsNullOrWhiteSpace(name)) return;

            if (CategoryList.Contains(name)) {
                await MaterialDialogService.ShowErrorAsync("该分类已存在", "提示");
                return;
            }

            _dbManager.AddCategory(name);
            CategoryList.Add(name);
            CategoryAdded?.Invoke(this, name);
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
