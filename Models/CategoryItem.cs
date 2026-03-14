using System;

namespace WallpaperEngine.Models
{
    /// <summary>
    /// 分类数据模型，用于分类管理界面
    /// </summary>
    public class CategoryItem
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// 该分类下的壁纸数量
        /// </summary>
        public int WallpaperCount { get; set; }

        /// <summary>
        /// 是否为受保护的分类（内置分类，不允许删除或重命名）
        /// </summary>
        public bool IsProtected { get; set; }

        /// <summary>
        /// 是否为默认分类（硬编码的默认分类，非自定义）
        /// </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="id">分类ID</param>
        /// <param name="name">分类名称</param>
        /// <param name="count">壁纸数量</param>
        /// <param name="isProtected">是否为受保护分类</param>
        /// <param name="isDefault">是否为默认分类</param>
        public CategoryItem(string id, string name, int count, bool isProtected = false, bool isDefault = false)
        {
            Id = id;
            Name = name;
            WallpaperCount = count;
            IsProtected = isProtected;
            IsDefault = isDefault;
        }

        /// <summary>
        /// 构造函数（兼容旧代码）
        /// </summary>
        /// <param name="name">分类名称</param>
        /// <param name="count">壁纸数量</param>
        /// <param name="isProtected">是否为受保护分类</param>
        public CategoryItem(string name, int count, bool isProtected = false)
        {
            Id = "";
            Name = name;
            WallpaperCount = count;
            IsProtected = isProtected;
            IsDefault = false;
        }

        /// <summary>
        /// 无参构造函数（用于 XAML 绑定）
        /// </summary>
        public CategoryItem() { }

        /// <summary>
        /// 判断分类是否为受保护的内置分类
        /// </summary>
        /// <param name="category">分类名称</param>
        /// <returns>如果为受保护分类返回 true，否则返回 false</returns>
        public static bool IsProtectedCategory(string category)
        {
            return CategoryConstants.IsProtectedCategory(category);
        }
    }
}