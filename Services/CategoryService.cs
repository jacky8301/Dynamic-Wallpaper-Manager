using CommunityToolkit.Mvvm.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WallpaperEngine.Data;
using WallpaperEngine.Events;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services
{
    /// <summary>
    /// 分类服务实现，提供集中式的分类管理
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly DatabaseManager _dbManager;
        private List<CategoryItem> _cachedCategories = new();
        private bool _isInitialized = false;
        private readonly object _syncLock = new object();

        /// <summary>
        /// 分类变更事件
        /// </summary>
        public event EventHandler<CategoryChangedEventArgs> CategoryChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public CategoryService()
        {
            _dbManager = Ioc.Default.GetRequiredService<DatabaseManager>();
        }

        /// <summary>
        /// 初始化分类服务
        /// </summary>
        private Task InitializeAsync()
        {
            if (_isInitialized) return Task.CompletedTask;

            lock (_syncLock)
            {
                if (_isInitialized) return Task.CompletedTask;

                // 确保数据库中有默认分类
                EnsureDefaultCategories();
                _isInitialized = true;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// 确保数据库中存在默认分类
        /// </summary>
        private void EnsureDefaultCategories()
        {
            try
            {
                // 检查默认分类是否已存在，如果不存在则创建
                foreach (var defaultCategory in CategoryConstants.DefaultCategories)
                {
                    var categoryId = _dbManager.GetCategoryIdByName(defaultCategory.Name);
                    if (categoryId < 0)
                    {
                        // 分类不存在，创建它
                        _dbManager.AddCategory(defaultCategory.Name);
                        Log.Information("已创建默认分类: {CategoryName}", defaultCategory.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"确保默认分类时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有分类（包括虚拟分类和自定义分类）
        /// </summary>
        /// <returns>分类项列表</returns>
        public async Task<List<CategoryItem>> GetAllCategoriesAsync()
        {
            await InitializeAsync();

            lock (_syncLock)
            {
                if (_cachedCategories.Count > 0)
                    return new List<CategoryItem>(_cachedCategories);

                // 构建完整的分类列表
                var allCategories = new List<CategoryItem>();

                // 添加虚拟分类："所有分类" (ID = 0)
                allCategories.Add(new CategoryItem(CategoryConstants.ALL_CATEGORIES_ID, "所有分类", 0, true, false));

                // 添加虚拟分类："未分类" (ID = 1)
                allCategories.Add(new CategoryItem(CategoryConstants.UNCATEGORIZED_ID, "未分类", 0, true, false));

                // 从数据库加载所有分类（包括默认分类和自定义分类）
                var dbCategories = _dbManager.GetAllCategories();
                allCategories.AddRange(dbCategories);

                _cachedCategories = allCategories;
                return new List<CategoryItem>(_cachedCategories);
            }
        }

        /// <summary>
        /// 获取自定义分类（不包括虚拟分类）
        /// </summary>
        /// <returns>分类项列表</returns>
        public async Task<List<CategoryItem>> GetCustomCategoriesAsync()
        {
            var allCategories = await GetAllCategoriesAsync();
            return allCategories
                .Where(c => !CategoryConstants.IsVirtualCategoryId(c.Id) && !c.IsProtected)
                .ToList();
        }

        /// <summary>
        /// 根据分类ID获取分类项
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>分类项，如果不存在则返回null</returns>
        public async Task<CategoryItem?> GetCategoryByIdAsync(int categoryId)
        {
            var allCategories = await GetAllCategoriesAsync();
            return allCategories.FirstOrDefault(c => c.Id == categoryId);
        }

        /// <summary>
        /// 根据分类名称获取分类项
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>分类项，如果不存在则返回null</returns>
        public async Task<CategoryItem?> GetCategoryByNameAsync(string categoryName)
        {
            var allCategories = await GetAllCategoriesAsync();
            return allCategories.FirstOrDefault(c => c.Name == categoryName);
        }

        /// <summary>
        /// 添加新分类
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>新分类的ID</returns>
        public async Task<int> AddCategoryAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("分类名称不能为空", nameof(categoryName));

            await InitializeAsync();

            // 检查分类是否已存在
            var existingCategory = await GetCategoryByNameAsync(categoryName);
            if (existingCategory != null)
                return existingCategory.Id;

            // 检查是否为受保护分类
            if (CategoryConstants.IsProtectedCategory(categoryName))
                throw new InvalidOperationException($"不能添加受保护分类: {categoryName}");

            try
            {
                // 添加到数据库
                var categoryId = _dbManager.AddCategory(categoryName);

                // 清除缓存
                lock (_syncLock)
                {
                    _cachedCategories.Clear();
                }

                // 触发事件
                CategoryChanged?.Invoke(this, new CategoryChangedEventArgs(CategoryChangeType.Added, categoryId, categoryName));

                Log.Information("已添加分类: {CategoryName} (ID: {CategoryId})", categoryName, categoryId);
                return categoryId;
            }
            catch (Exception ex)
            {
                Log.Error($"添加分类失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 重命名分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="newName">新分类名称</param>
        /// <returns>是否成功</returns>
        public async Task<bool> RenameCategoryAsync(int categoryId, string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("新分类名称不能为空", nameof(newName));

            await InitializeAsync();

            // 检查是否为虚拟分类或受保护分类
            if (CategoryConstants.IsVirtualCategoryId(categoryId))
                throw new InvalidOperationException("不能重命名虚拟分类");

            var existingCategory = await GetCategoryByIdAsync(categoryId);
            if (existingCategory == null)
                throw new InvalidOperationException($"分类ID {categoryId} 不存在");

            if (existingCategory.IsProtected)
                throw new InvalidOperationException($"不能重命名受保护分类: {existingCategory.Name}");

            // 检查新名称是否已存在
            var categoryWithNewName = await GetCategoryByNameAsync(newName);
            if (categoryWithNewName != null && categoryWithNewName.Id != categoryId)
                throw new InvalidOperationException($"分类名称 '{newName}' 已存在");

            try
            {
                var oldName = existingCategory.Name;
                _dbManager.RenameCategory(categoryId, newName);

                // 更新该分类下所有壁纸的 project.json 文件
                await UpdateProjectJsonCategoryAsync(categoryId, newName);

                // 清除缓存
                lock (_syncLock)
                {
                    _cachedCategories.Clear();
                }

                // 触发事件
                CategoryChanged?.Invoke(this, new CategoryChangedEventArgs(
                    CategoryChangeType.Renamed, categoryId, newName, oldName));

                Log.Information("已重命名分类: {OldName} -> {NewName} (ID: {CategoryId})", oldName, newName, categoryId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"重命名分类失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新指定分类下所有壁纸的 project.json 文件中的分类名称
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="newCategoryName">新分类名称</param>
        private async Task UpdateProjectJsonCategoryAsync(int categoryId, string newCategoryName)
        {
            try
            {
                var folderPaths = _dbManager.GetWallpaperFolderPathsByCategoryId(categoryId);
                Log.Information("开始更新 {Count} 个壁纸的 project.json 分类为: {NewName}", folderPaths.Count, newCategoryName);

                foreach (var folderPath in folderPaths)
                {
                    var projectFile = Path.Combine(folderPath, "project.json");
                    if (!File.Exists(projectFile))
                        continue;

                    try
                    {
                        var json = await File.ReadAllTextAsync(projectFile);
                        var project = JsonConvert.DeserializeObject<WallpaperProject>(json);
                        if (project == null)
                            continue;

                        project.Category = newCategoryName;
                        var updatedJson = JsonConvert.SerializeObject(project, Formatting.Indented);
                        await File.WriteAllTextAsync(projectFile, updatedJson);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("更新 project.json 分类失败 {ProjectFile}: {Message}", projectFile, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("批量更新 project.json 分类失败: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// 删除分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> DeleteCategoryAsync(int categoryId)
        {
            await InitializeAsync();

            // 检查是否为虚拟分类或受保护分类
            if (CategoryConstants.IsVirtualCategoryId(categoryId))
                throw new InvalidOperationException("不能删除虚拟分类");

            var existingCategory = await GetCategoryByIdAsync(categoryId);
            if (existingCategory == null)
                throw new InvalidOperationException($"分类ID {categoryId} 不存在");

            if (existingCategory.IsProtected)
                throw new InvalidOperationException($"不能删除受保护分类: {existingCategory.Name}");

            try
            {
                var categoryName = existingCategory.Name;
                _dbManager.DeleteCategory(categoryId);

                // 清除缓存
                lock (_syncLock)
                {
                    _cachedCategories.Clear();
                }

                // 触发事件
                CategoryChanged?.Invoke(this, new CategoryChangedEventArgs(
                    CategoryChangeType.Deleted, categoryId, categoryName));

                Log.Information("已删除分类: {CategoryName} (ID: {CategoryId})", categoryName, categoryId);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"删除分类失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        /// <returns>分类名称到壁纸数量的字典</returns>
        public async Task<Dictionary<string, int>> GetCategoryStatisticsAsync()
        {
            var allCategories = await GetAllCategoriesAsync();
            var stats = new Dictionary<string, int>();

            foreach (var category in allCategories)
            {
                var count = await GetCategoryWallpaperCountAsync(category.Id);
                stats[category.Name] = count;
            }

            return stats;
        }

        /// <summary>
        /// 获取指定分类的壁纸数量
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>壁纸数量</returns>
        public async Task<int> GetCategoryWallpaperCountAsync(int categoryId)
        {
            return await Task.Run(() => _dbManager.GetCategoryWallpaperCount(categoryId));
        }

        /// <summary>
        /// 刷新分类列表（从数据库重新加载）
        /// </summary>
        public async Task RefreshCategoriesAsync()
        {
            lock (_syncLock)
            {
                _cachedCategories.Clear();
                _isInitialized = false;
            }

            await InitializeAsync();

            // 触发事件
            CategoryChanged?.Invoke(this, new CategoryChangedEventArgs(
                CategoryChangeType.Refreshed, -1));
        }

        /// <summary>
        /// 检查分类名称是否已存在
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>是否存在</returns>
        public async Task<bool> CategoryExistsAsync(string categoryName)
        {
            var category = await GetCategoryByNameAsync(categoryName);
            return category != null;
        }

        /// <summary>
        /// 检查分类ID是否有效（包括虚拟分类）
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否有效</returns>
        public async Task<bool> IsValidCategoryIdAsync(int categoryId)
        {
            var category = await GetCategoryByIdAsync(categoryId);
            return category != null;
        }

        /// <summary>
        /// 触发分类统计更新事件
        /// </summary>
        public void NotifyStatsUpdated()
        {
            CategoryChanged?.Invoke(this, new CategoryChangedEventArgs(
                CategoryChangeType.StatsUpdated, -1));
        }
    }
}