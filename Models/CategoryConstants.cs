using System.Collections.Generic;

namespace WallpaperEngine.Models
{
    /// <summary>
    /// 分类常量定义，包括虚拟分类ID和默认分类列表
    /// </summary>
    public static class CategoryConstants
    {
        /// <summary>
        /// "所有分类"虚拟分类ID
        /// </summary>
        public const int ALL_CATEGORIES_ID = 0;

        /// <summary>
        /// "未分类"虚拟分类ID
        /// </summary>
        public const int UNCATEGORIZED_ID = 1;

        /// <summary>
        /// 默认分类列表（不包括虚拟分类）
        /// </summary>
        public static readonly List<string> DefaultCategories = new()
        {
            "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        /// <summary>
        /// 受保护的分类名称列表
        /// </summary>
        public static readonly List<string> ProtectedCategoryNames = new()
        {
            "所有分类", "未分类"
        };

        /// <summary>
        /// 检查是否为受保护分类
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>是否为受保护分类</returns>
        public static bool IsProtectedCategory(string categoryName)
        {
            return ProtectedCategoryNames.Contains(categoryName);
        }

        /// <summary>
        /// 检查是否为虚拟分类ID
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>是否为虚拟分类</returns>
        public static bool IsVirtualCategoryId(int categoryId)
        {
            return categoryId == ALL_CATEGORIES_ID || categoryId == UNCATEGORIZED_ID;
        }

        /// <summary>
        /// 根据分类ID获取虚拟分类名称
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>虚拟分类名称，如果不是虚拟分类则返回null</returns>
        public static string? GetVirtualCategoryName(int categoryId)
        {
            return categoryId switch
            {
                ALL_CATEGORIES_ID => "所有分类",
                UNCATEGORIZED_ID => "未分类",
                _ => null
            };
        }

        /// <summary>
        /// 根据虚拟分类名称获取ID
        /// </summary>
        /// <param name="categoryName">虚拟分类名称</param>
        /// <returns>虚拟分类ID，如果不是虚拟分类则返回-1</returns>
        public static int GetVirtualCategoryId(string categoryName)
        {
            return categoryName switch
            {
                "所有分类" => ALL_CATEGORIES_ID,
                "未分类" => UNCATEGORIZED_ID,
                _ => -1
            };
        }
    }
}