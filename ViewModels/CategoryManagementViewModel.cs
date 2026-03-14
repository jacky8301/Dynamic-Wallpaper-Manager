using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels
{
    /// <summary>
    /// 分类管理视图模型，提供分类的增加、删除、重命名功能
    /// </summary>
    public partial class CategoryManagementViewModel : ObservableObject
    {
        private readonly DatabaseManager _dbManager;
        private readonly ICategoryService _categoryService;

        /// <summary>
        /// 受保护的虚拟分类列表（所有分类、未分类），不是硬编码的默认分类
        /// </summary>
        private readonly List<string> _defaultCategories = new()
        {
            "所有分类", "未分类"
        };

        /// <summary>
        /// 分类列表（包含受保护的虚拟分类和自定义分类）
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<CategoryItem> _categories = new();

        /// <summary>
        /// 当前选中的分类项
        /// </summary>
        [ObservableProperty]
        private CategoryItem? _selectedCategory;

        /// <summary>
        /// 分类总数
        /// </summary>
        [ObservableProperty]
        private int _totalCategories;

        /// <summary>
        /// 壁纸总数
        /// </summary>
        [ObservableProperty]
        private int _totalWallpapers;

        /// <summary>
        /// 是否正在加载数据
        /// </summary>
        [ObservableProperty]
        private bool _isLoading;

        /// <summary>
        /// 构造函数，注入数据库管理器和分类服务
        /// </summary>
        /// <param name="dbManager">数据库管理器</param>
        /// <param name="categoryService">分类服务</param>
        public CategoryManagementViewModel(DatabaseManager dbManager, ICategoryService categoryService)
        {
            _dbManager = dbManager;
            _categoryService = categoryService;
        }

        /// <summary>
        /// 异步加载分类及统计信息
        /// </summary>
        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            IsLoading = true;
            try
            {
                await Task.Run(() =>
                {
                    // 获取数据库中的所有分类（包括默认分类和自定义分类）
                    var dbCategories = _dbManager.GetAllCategories();

                    // 计算壁纸总数
                    var totalWallpapers = _dbManager.GetTotalWallpaperCount();

                    // 构建完整的分类列表（虚拟分类 + 数据库分类）
                    var allCategoryItems = new List<CategoryItem>();

                    // 添加虚拟分类："所有分类" (ID = 0)
                    var allCategoriesCount = _dbManager.GetCategoryWallpaperCount(CategoryConstants.ALL_CATEGORIES_ID);
                    allCategoryItems.Add(new CategoryItem(CategoryConstants.ALL_CATEGORIES_ID, "所有分类", allCategoriesCount, true, false));

                    // 添加虚拟分类："未分类" (ID = 1)
                    var uncategorizedCount = _dbManager.GetCategoryWallpaperCount(CategoryConstants.UNCATEGORIZED_ID);
                    allCategoryItems.Add(new CategoryItem(CategoryConstants.UNCATEGORIZED_ID, "未分类", uncategorizedCount, true, false));

                    // 添加数据库中的分类（包括默认分类和自定义分类）
                    allCategoryItems.AddRange(dbCategories);

                    // 按分类类型和名称排序：虚拟分类在前，然后是默认分类，最后是自定义分类
                    var sortedCategoryItems = allCategoryItems
                        .OrderByDescending(c => CategoryConstants.IsVirtualCategoryId(c.Id)) // 虚拟分类在前
                        .ThenByDescending(c => c.IsDefault) // 默认分类其次
                        .ThenBy(c => c.Name) // 按名称排序
                        .ToList();

                    // 更新 UI 线程上的集合
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        Categories.Clear();
                        foreach (var item in sortedCategoryItems)
                        {
                            Categories.Add(item);
                        }

                        TotalCategories = Categories.Count;
                        TotalWallpapers = totalWallpapers;

                        // 默认选中第一个分类
                        if (Categories.Count > 0)
                        {
                            SelectedCategory = Categories.First();
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Log.Error($"加载分类列表失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 构建完整的分类列表（受保护的虚拟分类 + 数据库中的自定义分类）
        /// </summary>
        /// <returns>所有分类的列表</returns>
        private List<string> BuildCategoryList()
        {
            var allCategories = new List<string>(_defaultCategories);

            // 注意：硬编码的默认分类已移除，_defaultCategories只包含受保护的虚拟分类

            // 从数据库获取自定义分类
            try
            {
                var customCategories = _dbManager.GetCustomCategories();
                foreach (var category in customCategories)
                {
                    if (!allCategories.Contains(category))
                    {
                        allCategories.Add(category);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"获取自定义分类列表失败: {ex.Message}");
            }

            return allCategories;
        }

        /// <summary>
        /// 新增分类命令
        /// </summary>
        [RelayCommand]
        private async Task AddCategoryAsync()
        {
            var result = await MaterialDialogService.ShowInputAsync("新增分类", "请输入分类名称", "分类名称");
            if (result.Confirmed && result.Data is string categoryName && !string.IsNullOrWhiteSpace(categoryName))
            {
                // 检查分类是否已存在
                if (Categories.Any(c => c.Name == categoryName))
                {
                    await MaterialDialogService.ShowErrorAsync("该分类名称已存在", "错误");
                    return;
                }

                try
                {
                    _dbManager.AddCategory(categoryName);

                    // 重新加载分类列表
                    await LoadCategoriesAsync();

                    // 通知 MainViewModel 刷新分类列表
                    RefreshMainViewModelCategories();

                    // 通知 WallpaperDetailViewModel 刷新分类列表
                    RefreshWallpaperDetailViewModelCategories();
                }
                catch (Exception ex)
                {
                    Log.Error($"新增分类失败: {ex.Message}");
                    await MaterialDialogService.ShowErrorAsync($"新增分类失败: {ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 重命名分类命令
        /// </summary>
        [RelayCommand]
        private async Task RenameCategoryAsync()
        {
            if (SelectedCategory == null || SelectedCategory.IsProtected)
                return;

            var oldName = SelectedCategory.Name;
            var categoryId = SelectedCategory.Id;
            // 检查是否为虚拟分类或无效ID
            if (CategoryConstants.IsVirtualCategoryId(categoryId) || categoryId <= 0)
            {
                await MaterialDialogService.ShowErrorAsync("无法重命名虚拟分类或无效分类", "错误");
                return;
            }

            var result = await MaterialDialogService.ShowInputAsync("重命名分类", "请输入新的分类名称", "", oldName);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName) && newName != oldName)
            {
                // 检查新名称是否已存在
                if (Categories.Any(c => c.Name == newName))
                {
                    await MaterialDialogService.ShowErrorAsync("该分类名称已存在", "错误");
                    return;
                }

                try
                {
                    _dbManager.RenameCategory(categoryId, newName);

                    // 重新加载分类列表
                    await LoadCategoriesAsync();

                    // 通知 MainViewModel 刷新分类列表
                    RefreshMainViewModelCategories();

                    // 通知 WallpaperDetailViewModel 刷新分类列表
                    RefreshWallpaperDetailViewModelCategories();
                }
                catch (Exception ex)
                {
                    Log.Error($"重命名分类失败: {ex.Message}");
                    await MaterialDialogService.ShowErrorAsync($"重命名分类失败: {ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 删除分类命令
        /// </summary>
        [RelayCommand]
        private async Task DeleteCategoryAsync()
        {
            if (SelectedCategory == null || SelectedCategory.IsProtected)
                return;

            var categoryName = SelectedCategory.Name;
            var categoryId = SelectedCategory.Id;
            var wallpaperCount = SelectedCategory.WallpaperCount;

            // 检查是否为虚拟分类或无效ID
            if (CategoryConstants.IsVirtualCategoryId(categoryId) || categoryId <= 0)
            {
                await MaterialDialogService.ShowErrorAsync("无法删除虚拟分类或无效分类", "错误");
                return;
            }

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除分类「{categoryName}」吗？\n该分类下有 {wallpaperCount} 个壁纸，删除后这些壁纸将被重置为「未分类」。",
                "删除分类");

            if (confirmed)
            {
                try
                {
                    _dbManager.DeleteCategory(categoryId);

                    // 重新加载分类列表
                    await LoadCategoriesAsync();

                    // 通知 MainViewModel 刷新分类列表
                    RefreshMainViewModelCategories();

                    // 通知 WallpaperDetailViewModel 刷新分类列表
                    RefreshWallpaperDetailViewModelCategories();
                }
                catch (Exception ex)
                {
                    Log.Error($"删除分类失败: {ex.Message}");
                    await MaterialDialogService.ShowErrorAsync($"删除分类失败: {ex.Message}", "错误");
                }
            }
        }

        /// <summary>
        /// 通知 MainViewModel 刷新分类列表
        /// </summary>
        private void RefreshMainViewModelCategories()
        {
            try
            {
                var mainVm = Ioc.Default.GetService<MainViewModel>();
                if (mainVm != null)
                {
                    // 调用公共方法 RefreshCategoryList
                    mainVm.RefreshCategoryList();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"通知 MainViewModel 刷新分类列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 通知 WallpaperDetailViewModel 刷新分类列表
        /// </summary>
        private void RefreshWallpaperDetailViewModelCategories()
        {
            try
            {
                var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                if (detailVm != null)
                {
                    // 调用公共方法 RefreshCategoryList
                    detailVm.RefreshCategoryList();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"通知 WallpaperDetailViewModel 刷新分类列表失败: {ex.Message}");
            }
        }
    }
}