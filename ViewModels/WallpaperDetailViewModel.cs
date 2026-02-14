using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.ObjectModel;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸详情视图模型，负责壁纸详细信息的展示和编辑，包括标签、类型、分类管理
    /// </summary>
    public partial class WallpaperDetailViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;
        private readonly IDataContextService _dataContextService;
        private WallpaperItem _originalItem;

        /// <summary>当前显示的壁纸项</summary>
        [ObservableProperty]
        private WallpaperItem? _currentWallpaper;

        /// <summary>是否处于编辑模式</summary>
        [ObservableProperty]
        private bool _isEditMode;

        /// <summary>编辑状态描述文本</summary>
        [ObservableProperty]
        private string _editStatus = "就绪";

        /// <summary>当前选中的壁纸类型</summary>
        [ObservableProperty]
        private string _selectedType;

        /// <summary>壁纸描述文本</summary>
        [ObservableProperty]
        private string _description;

        /// <summary>壁纸标题</summary>
        [ObservableProperty]
        private string _title;

        /// <summary>预览图文件名</summary>
        [ObservableProperty]
        private string _previewFileName;

        /// <summary>壁纸内容文件名</summary>
        [ObservableProperty]
        private string _contentFileName;

        /// <summary>当前选中的分类</summary>
        [ObservableProperty]
        private string _selectedCategory;

        /// <summary>标签编辑集合</summary>
        public ObservableCollection<string> Tags { get; } = new ObservableCollection<string>();

        /// <summary>壁纸类型列表数据源</summary>
        public List<string> WallpaperTypes { get; } = new List<string>
        {
            "scene",
            "video",
            "web",
            "application"
        };

        /// <summary>壁纸分类列表数据源</summary>
        public ObservableCollection<string> CategoryList { get; } = new ObservableCollection<string>
        {
            "未分类", "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        /// <summary>开始编辑命令</summary>
        public IRelayCommand StartEditCommand { get; }
        /// <summary>保存编辑命令</summary>
        public IAsyncRelayCommand SaveEditCommand { get; }
        /// <summary>取消编辑命令</summary>
        public IRelayCommand CancelEditCommand { get; }
        /// <summary>设置预览图文件名命令</summary>
        public IAsyncRelayCommand<string?> SetPreviewFileNameCommand { get; }
        /// <summary>设置内容文件名命令</summary>
        public IAsyncRelayCommand<string?> SetContentFileNameCommand { get; }
        /// <summary>新增分类命令</summary>
        public IAsyncRelayCommand AddCategoryCommand { get; }
        /// <summary>新增标签命令</summary>
        public IAsyncRelayCommand AddTagCommand { get; }
        /// <summary>移除标签命令</summary>
        public IRelayCommand<string> RemoveTagCommand { get; }

        /// <summary>新增分类事件，通知 MainViewModel 同步</summary>
        public event EventHandler<string>? CategoryAdded;

        /// <summary>
        /// 初始化壁纸详情视图模型，订阅壁纸变更事件并初始化命令
        /// </summary>
        /// <param name="dataContextService">数据上下文服务，用于监听当前壁纸变更</param>
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

        /// <summary>
        /// 当前壁纸变更时的回调，同步更新各编辑字段
        /// </summary>
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

        /// <summary>
        /// 使用指定壁纸初始化详情视图
        /// </summary>
        /// <param name="wallpaper">要显示的壁纸项</param>
        public void Initialize(WallpaperItem? wallpaper = null)
        {
            if (wallpaper == null) return;
            CurrentWallpaper = wallpaper;
            _originalItem = CreateBackup(wallpaper);
        }

        /// <summary>
        /// 创建壁纸数据的备份副本，用于编辑取消时恢复
        /// </summary>
        /// <param name="source">源壁纸项</param>
        /// <returns>备份的壁纸项副本</returns>
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

        /// <summary>
        /// 从备份恢复壁纸数据
        /// </summary>
        /// <param name="target">要恢复的目标壁纸项</param>
        /// <param name="backup">备份的壁纸项</param>
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

        /// <summary>
        /// 从当前壁纸的项目数据同步标签到编辑集合
        /// </summary>
        private void SyncTagsFromProject()
        {
            Tags.Clear();
            var projectTags = CurrentWallpaper?.Project?.Tags;
            if (projectTags == null) return;
            foreach (var tag in projectTags) {
                Tags.Add(tag);
            }
        }

        /// <summary>
        /// 新增标签，弹出输入对话框并添加到标签集合
        /// </summary>
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

        /// <summary>
        /// 移除指定标签
        /// </summary>
        /// <param name="tag">要移除的标签名称</param>
        private void RemoveTag(string? tag)
        {
            if (tag != null) {
                Tags.Remove(tag);
            }
        }

        /// <summary>
        /// 从数据库加载自定义分类并添加到分类列表
        /// </summary>
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

        /// <summary>
        /// 新增分类，弹出输入对话框并添加到分类列表和数据库
        /// </summary>
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

        /// <summary>
        /// 安全加载壁纸的文件列表，捕获并记录异常
        /// </summary>
        /// <param name="wallpaper">壁纸项</param>
        private async Task LoadFileListSafeAsync(WallpaperItem? wallpaper)
        {
            if (wallpaper == null) return;
            try {
                await wallpaper.LoadFileListAsync();
            } catch (Exception ex) {
                Log.Warning($"加载文件列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 显示保存成功消息
        /// </summary>
        private void ShowSaveSuccessMessage()
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await MaterialDialogService.ShowConfirmationAsync("壁纸详情已成功保存!", "保存成功");
            });
        }

        /// <summary>
        /// 显示错误消息对话框
        /// </summary>
        /// <param name="message">错误消息内容</param>
        private void ShowErrorMessage(string message)
        {
            System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => {
                await MaterialDialogService.ShowErrorAsync(message, "错误");
            });
        }
    }
}
