using System;

namespace WallpaperEngine.Events
{
    /// <summary>
    /// 分类变更事件参数
    /// </summary>
    public class CategoryChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更类型
        /// </summary>
        public CategoryChangeType ChangeType { get; }

        /// <summary>
        /// 受影响的分类ID
        /// </summary>
        public string CategoryId { get; }

        /// <summary>
        /// 分类名称（对于重命名操作是新名称）
        /// </summary>
        public string? CategoryName { get; }

        /// <summary>
        /// 旧分类名称（仅用于重命名操作）
        /// </summary>
        public string? OldCategoryName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="changeType">变更类型</param>
        /// <param name="categoryId">分类ID</param>
        /// <param name="categoryName">分类名称</param>
        /// <param name="oldCategoryName">旧分类名称（仅用于重命名）</param>
        public CategoryChangedEventArgs(CategoryChangeType changeType, string categoryId, string? categoryName = null, string? oldCategoryName = null)
        {
            ChangeType = changeType;
            CategoryId = categoryId;
            CategoryName = categoryName;
            OldCategoryName = oldCategoryName;
        }
    }

    /// <summary>
    /// 分类变更类型枚举
    /// </summary>
    public enum CategoryChangeType
    {
        /// <summary>
        /// 添加新分类
        /// </summary>
        Added,

        /// <summary>
        /// 重命名分类
        /// </summary>
        Renamed,

        /// <summary>
        /// 删除分类
        /// </summary>
        Deleted,

        /// <summary>
        /// 分类统计信息更新
        /// </summary>
        StatsUpdated,

        /// <summary>
        /// 所有分类刷新
        /// </summary>
        Refreshed
    }
}