using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WallpaperEngine.Events;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services
{
    /// <summary>
    /// 分类服务接口，提供集中式的分类管理功能
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// 分类变更事件
        /// </summary>
        event EventHandler<CategoryChangedEventArgs> CategoryChanged;

        /// <summary>
        /// 获取所有分类（包括虚拟分类和自定义分类）
        /// </summary>
        /// <returns>分类项列表</returns>
        Task<List<CategoryItem>> GetAllCategoriesAsync();

        /// <summary>
        /// 获取自定义分类（不包括虚拟分类）
        /// </summary>
        /// <returns>分类项列表</returns>
        Task<List<CategoryItem>> GetCustomCategoriesAsync();

        /// <summary>
        /// 根据分类ID获取分类项
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>分类项，如果不存在则返回null</returns>
        Task<CategoryItem?> GetCategoryByIdAsync(string categoryId);

        /// <summary>
        /// 根据分类名称获取分类项
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>分类项，如果不存在则返回null</returns>
        Task<CategoryItem?> GetCategoryByNameAsync(string categoryName);

        /// <summary>
        /// 添加新分类
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>新分类的ID</returns>
        Task<string> AddCategoryAsync(string categoryName);

        /// <summary>
        /// 重命名分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="newName">新分类名称</param>
        /// <returns>是否成功</returns>
        Task<bool> RenameCategoryAsync(string categoryId, string newName);

        /// <summary>
        /// 删除分类
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteCategoryAsync(string categoryId);

        /// <summary>
        /// 获取分类统计信息
        /// </summary>
        /// <returns>分类名称到壁纸数量的字典</returns>
        Task<Dictionary<string, int>> GetCategoryStatisticsAsync();

        /// <summary>
        /// 获取指定分类的壁纸数量
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>壁纸数量</returns>
        Task<int> GetCategoryWallpaperCountAsync(string categoryId);

        /// <summary>
        /// 刷新分类列表（从数据库重新加载）
        /// </summary>
        Task RefreshCategoriesAsync();

        /// <summary>
        /// 检查分类名称是否已存在
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>是否存在</returns>
        Task<bool> CategoryExistsAsync(string categoryName);

        /// <summary>
        /// 检查分类ID是否有效（包括虚拟分类）
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否有效</returns>
        Task<bool> IsValidCategoryIdAsync(string categoryId);

        /// <summary>
        /// 触发分类统计更新事件
        /// </summary>
        void NotifyStatsUpdated();
    }
}