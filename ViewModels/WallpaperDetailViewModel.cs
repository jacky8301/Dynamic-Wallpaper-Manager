using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WallpaperEngine.Data;
using WallpaperEngine.Events;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸详情视图模型，负责壁纸详细信息的展示和编辑，包括标签、类型、分类管理
    /// </summary>
    public partial class WallpaperDetailViewModel : ObservableObject, IDisposable {
        private readonly DatabaseManager _dbManager;
        private readonly IDataContextService _dataContextService;
        private readonly ICategoryService _categoryService;
        private WallpaperItem _originalItem;
        private bool _disposed;

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
        private string _selectedType = string.Empty;

        /// <summary>壁纸描述文本</summary>
        [ObservableProperty]
        private string _description = string.Empty;

        /// <summary>壁纸标题</summary>
        [ObservableProperty]
        private string _title = string.Empty;

        /// <summary>预览图文件名</summary>
        [ObservableProperty]
        private string _previewFileName = string.Empty;

        /// <summary>壁纸内容文件名</summary>
        [ObservableProperty]
        private string _contentFileName = string.Empty;

        /// <summary>当前选中的分类</summary>
        [ObservableProperty]
        private string _selectedCategory = string.Empty;

        /// <summary>当前选中的内容分级</summary>
        [ObservableProperty]
        private string _selectedContentRating = "Everyone";

        /// <summary>当前壁纸所属的合集列表</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperCollection> _wallpaperCollections = new();

        /// <summary>是否显示合集区域（当壁纸不属于任何合集时隐藏）</summary>
        [ObservableProperty]
        private bool _showCollectionsSection = false;

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
            "未分类" // 受保护虚拟分类
        };

        /// <summary>内容分级列表数据源</summary>
        public List<string> ContentRatingList { get; } = new List<string>
        {
            "Everyone",
            "Questionable",
            "Mature"
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
        /// <summary>导航到合集命令</summary>
        public IRelayCommand<WallpaperCollection> NavigateToCollectionCommand { get; }

        /// <summary>新增分类事件，通知 MainViewModel 同步</summary>
        public event EventHandler<string>? CategoryAdded;

        /// <summary>
        /// 初始化壁纸详情视图模型，订阅壁纸变更事件并初始化命令
        /// </summary>
        /// <param name="dataContextService">数据上下文服务，用于监听当前壁纸变更</param>
        /// <param name="categoryService">分类服务</param>
        public WallpaperDetailViewModel(IDataContextService dataContextService, ICategoryService categoryService)
        {
            _dbManager = Ioc.Default.GetRequiredService<DatabaseManager>();
            _dataContextService = dataContextService;
            _categoryService = categoryService;
            // 订阅状态变化事件
            _dataContextService.CurrentWallpaperChanged += OnCurrentWallpaperChanged;

            // 订阅分类变更事件
            _categoryService.CategoryChanged += OnCategoryChanged;

            // 初始化命令
            StartEditCommand = new RelayCommand(StartEdit);
            SaveEditCommand = new AsyncRelayCommand(SaveEdit, CanSaveEdit);
            CancelEditCommand = new RelayCommand(CancelEdit);
            SetPreviewFileNameCommand = new AsyncRelayCommand<string?>(SetPreviewFileName, CanSetPreviewFileName);
            SetContentFileNameCommand = new AsyncRelayCommand<string?>(SetContentFileName, CanSetContentFileName);
            AddCategoryCommand = new AsyncRelayCommand(AddCategory);
            AddTagCommand = new AsyncRelayCommand(AddTag);
            RemoveTagCommand = new RelayCommand<string>(RemoveTag);
            NavigateToCollectionCommand = new RelayCommand<WallpaperCollection>(NavigateToCollection);

            // 注意：硬编码的默认分类已移除，不再添加到分类列表

            // 加载自定义分类
            LoadCustomCategories();
        }

        /// <summary>
        /// 加载当前壁纸所属的合集列表
        /// </summary>
        public async Task LoadWallpaperCollections()
        {
            if (CurrentWallpaper == null || string.IsNullOrEmpty(CurrentWallpaper.FolderPath))
            {
                WallpaperCollections.Clear();
                ShowCollectionsSection = false;
                return;
            }

            try
            {
                var collections = await Task.Run(() => _dbManager.GetCollectionsForWallpaper(CurrentWallpaper.Id));
                WallpaperCollections.Clear();
                foreach (var collection in collections)
                {
                    WallpaperCollections.Add(collection);
                }
                ShowCollectionsSection = WallpaperCollections.Count > 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "加载壁纸合集失败");
                ShowCollectionsSection = false;
            }
        }

        /// <summary>
        /// 导航到指定合集视图
        /// </summary>
        /// <param name="collection">要导航到的合集</param>
        private void NavigateToCollection(WallpaperCollection? collection)
        {
            if (collection == null) return;

            try
            {
                // 获取 MainViewModel 实例
                var mainViewModel = Ioc.Default.GetService<MainViewModel>();
                if (mainViewModel != null)
                {
                    // 切换到合集标签页（标签页2）
                    mainViewModel.CurrentTab = 2;

                    // 获取 CollectionViewModel 实例并选中指定合集
                    var collectionViewModel = Ioc.Default.GetService<CollectionViewModel>();
                    if (collectionViewModel != null)
                    {
                        // 查找并选中对应的合集
                        var targetCollection = collectionViewModel.Collections.FirstOrDefault(c => c.Id == collection.Id);
                        if (targetCollection != null)
                        {
                            collectionViewModel.SelectedCollection = targetCollection;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "导航到合集失败");
            }
        }

        /// <summary>
        /// 当前壁纸变更时的回调，同步更新各编辑字段
        /// </summary>
        private void OnCurrentWallpaperChanged(object? sender, WallpaperItem? newWallpaper)
        {
            // 当服务中的状态改变时，更新自己的数据
            CurrentWallpaper = newWallpaper;
            _ = LoadFileListSafeAsync(newWallpaper);
            // 确保分类列表是最新的，防止自定义分类缺失
            LoadCustomCategories();
            SelectedType = CurrentWallpaper?.Project?.Type ?? string.Empty;
            var category = CurrentWallpaper?.Category;
            // 如果分类为空，或者分类不在当前分类列表中，则使用"未分类"
            if (string.IsNullOrEmpty(category) || !CategoryList.Contains(category))
            {
                SelectedCategory = "未分类";
            }
            else
            {
                SelectedCategory = category;
            }
            SelectedContentRating = string.IsNullOrEmpty(CurrentWallpaper?.Project?.ContentRating) ? "Everyone" : CurrentWallpaper.Project.ContentRating;
            Description = CurrentWallpaper?.Project?.Description ?? string.Empty;
            Title = CurrentWallpaper?.Project?.Title ?? string.Empty;
            PreviewFileName = CurrentWallpaper?.Project?.Preview ?? string.Empty;
            ContentFileName = CurrentWallpaper?.Project?.File ?? string.Empty;
            SyncTagsFromProject();

            // 加载壁纸所属合集
            _ = LoadWallpaperCollections();
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
                Category = source.Category,
                CategoryId = source.CategoryId
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
            target.CategoryId = backup.CategoryId;
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
        /// 分类变更事件处理程序
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private async void OnCategoryChanged(object? sender, CategoryChangedEventArgs e)
        {
            // 当分类发生变化时，刷新分类列表
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                LoadCustomCategories();

                // 分类重命名时，同步更新当前壁纸的分类显示
                if (e.ChangeType == CategoryChangeType.Renamed && !string.IsNullOrEmpty(e.OldCategoryName) && !string.IsNullOrEmpty(e.CategoryName))
                {
                    if (CurrentWallpaper != null && CurrentWallpaper.Category == e.OldCategoryName)
                    {
                        CurrentWallpaper.Category = e.CategoryName;
                        CurrentWallpaper.Project.Category = e.CategoryName;
                        SelectedCategory = e.CategoryName;
                    }
                }

                // 分类删除时，将当前壁纸重置为"未分类"
                if (e.ChangeType == CategoryChangeType.Deleted)
                {
                    if (CurrentWallpaper != null && CurrentWallpaper.CategoryId == e.CategoryId)
                    {
                        CurrentWallpaper.CategoryId = CategoryConstants.UNCATEGORIZED_ID;
                        CurrentWallpaper.Category = "未分类";
                        CurrentWallpaper.Project.Category = "未分类";
                        SelectedCategory = "未分类";
                    }
                }
            });
        }

        /// <summary>
        /// 从数据库加载自定义分类并同步到分类列表
        /// </summary>
        private async Task LoadCustomCategoriesAsync()
        {
            try {
                var customCategories = await _categoryService.GetCustomCategoriesAsync();
                // 受保护虚拟分类列表（在详情页下拉列表中显示的分类）
                // 注意：只包含"未分类"，不包含"所有分类"，因为"所有分类"不是壁纸的有效分类
                var defaultCategories = new HashSet<string>
                {
                    "未分类" // 受保护虚拟分类
                };

                // 找出需要移除的自定义分类（存在于CategoryList中但不在数据库且不是默认分类）
                var categoriesToRemove = new List<string>();
                foreach (var category in CategoryList)
                {
                    if (!defaultCategories.Contains(category) && !customCategories.Any(c => c.Name == category))
                    {
                        categoriesToRemove.Add(category);
                    }
                }

                // 移除已删除的自定义分类
                foreach (var category in categoriesToRemove)
                {
                    CategoryList.Remove(category);
                }

                // 添加新的自定义分类
                foreach (var categoryItem in customCategories) {
                    if (!CategoryList.Contains(categoryItem.Name)) {
                        CategoryList.Add(categoryItem.Name);
                    }
                }
            } catch (Exception ex) {
                Log.Warning(ex, "加载自定义分类失败");
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

            try
            {
                await _categoryService.AddCategoryAsync(name);
                CategoryList.Add(name);
                CategoryAdded?.Invoke(this, name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "添加分类失败");
                await MaterialDialogService.ShowErrorAsync($"添加分类失败: {ex.Message}", "错误");
            }
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
                Log.Warning(ex, "加载文件列表失败");
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

        /// <summary>
        /// 释放资源并取消事件订阅
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _categoryService.CategoryChanged -= OnCategoryChanged;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
