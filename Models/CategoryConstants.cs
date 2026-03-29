using System.Collections.Generic;

namespace WallpaperEngine.Models
{
    /// <summary>
    /// 默认分类定义，包含分类名称和匹配标签
    /// </summary>
    public class DefaultCategoryDefinition
    {
        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 匹配标签列表
        /// </summary>
        public string[] MatchingTags { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">分类名称</param>
        /// <param name="matchingTags">匹配标签列表</param>
        public DefaultCategoryDefinition(string name, string[] matchingTags)
        {
            Name = name;
            MatchingTags = matchingTags;
        }
    }

    /// <summary>
    /// 分类常量定义，包括虚拟分类ID和受保护分类
    /// </summary>
    public static class CategoryConstants
    {
        /// <summary>
        /// "所有分类"虚拟分类ID
        /// </summary>
        public const string ALL_CATEGORIES_ID = "00000000-0000-0000-0000-000000000000";

        /// <summary>
        /// "未分类"虚拟分类ID
        /// </summary>
        public const string UNCATEGORIZED_ID = "00000000-0000-0000-0000-000000000001";

        /// <summary>
        /// "所有分类"显示名称
        /// </summary>
        public const string ALL_CATEGORIES_NAME = "所有分类";

        /// <summary>
        /// "未分类"显示名称
        /// </summary>
        public const string UNCATEGORIZED_NAME = "未分类";

        /// <summary>
        /// 默认分类定义列表（不包括虚拟分类）
        /// 包含10个合理的默认分类，基于Wallpaper Engine常见标签
        /// </summary>
        public static readonly IReadOnlyList<DefaultCategoryDefinition> DefaultCategories = new List<DefaultCategoryDefinition>
        {
            new DefaultCategoryDefinition("自然", new[] { "nature", "landscape", "forest", "water", "mountain", "自然", "风景", "森林", "水", "山", "山水", "大自然" }),
            new DefaultCategoryDefinition("抽象", new[] { "abstract", "pattern", "geometric", "minimal", "art", "抽象", "图案", "几何", "极简", "艺术", "现代艺术" }),
            new DefaultCategoryDefinition("游戏", new[] { "game", "gaming", "character", "fantasy", "rpg", "游戏", "电竞", "角色", "幻想", "角色扮演", "游戏角色" }),
            new DefaultCategoryDefinition("动漫", new[] { "anime", "manga", "cartoon", "japanese", "kawaii", "动漫", "漫画", "卡通", "日本", "可爱", "二次元" }),
            new DefaultCategoryDefinition("科幻", new[] { "sci-fi", "space", "future", "cyberpunk", "technology", "科幻", "太空", "未来", "赛博朋克", "科技", "未来主义" }),
            new DefaultCategoryDefinition("风景", new[] { "scenery", "cityscape", "sunset", "beach", "sky", "风景", "城市风光", "日落", "海滩", "天空", "景观" }),
            new DefaultCategoryDefinition("建筑", new[] { "architecture", "building", "interior", "modern", "urban", "建筑", "建筑物", "室内", "现代", "都市", "建筑设计" }),
            new DefaultCategoryDefinition("动物", new[] { "animal", "wildlife", "pet", "bird", "insect", "动物", "野生动物", "宠物", "鸟类", "昆虫", "动物世界" }),
            new DefaultCategoryDefinition("人物", new[] { "people", "portrait", "human", "face", "figure", "人物", "肖像", "人类", "面孔", "形象", "人像" }),
            new DefaultCategoryDefinition("车辆", new[] { "vehicle", "car", "motorcycle", "aircraft", "ship", "车辆", "汽车", "摩托车", "飞机", "船只", "交通工具" })
        };

        /// <summary>
        /// 受保护的分类名称列表
        /// </summary>
        public static readonly IReadOnlyList<string> ProtectedCategoryNames = new List<string>
        {
            ALL_CATEGORIES_NAME, UNCATEGORIZED_NAME
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
        public static bool IsVirtualCategoryId(string categoryId)
        {
            return categoryId == ALL_CATEGORIES_ID || categoryId == UNCATEGORIZED_ID;
        }

        /// <summary>
        /// 根据分类ID获取虚拟分类名称
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <returns>虚拟分类名称，如果不是虚拟分类则返回null</returns>
        public static string? GetVirtualCategoryName(string categoryId)
        {
            if (categoryId == ALL_CATEGORIES_ID) return ALL_CATEGORIES_NAME;
            if (categoryId == UNCATEGORIZED_ID) return UNCATEGORIZED_NAME;
            return null;
        }

        /// <summary>
        /// 根据虚拟分类名称获取ID
        /// </summary>
        /// <param name="categoryName">虚拟分类名称</param>
        /// <returns>虚拟分类ID，如果不是虚拟分类则返回null</returns>
        public static string? GetVirtualCategoryId(string categoryName)
        {
            return categoryName switch
            {
                "所有分类" => ALL_CATEGORIES_ID,
                "未分类" => UNCATEGORIZED_ID,
                _ => null
            };
        }

        /// <summary>
        /// 获取默认分类名称列表（用于向后兼容）
        /// </summary>
        /// <returns>默认分类名称列表</returns>
        public static List<string> GetDefaultCategoryNames()
        {
            return DefaultCategories.Select(d => d.Name).ToList();
        }

        /// <summary>
        /// 根据分类名称查找匹配的默认分类定义
        /// </summary>
        /// <param name="categoryName">分类名称</param>
        /// <returns>默认分类定义，如果不存在则返回null</returns>
        public static DefaultCategoryDefinition? FindDefaultCategoryByName(string categoryName)
        {
            return DefaultCategories.FirstOrDefault(d => d.Name == categoryName);
        }

        /// <summary>
        /// 根据标签获取推荐的默认分类
        /// </summary>
        /// <param name="tags">标签列表</param>
        /// <returns>推荐的默认分类列表，按匹配程度排序</returns>
        public static List<DefaultCategoryDefinition> SuggestCategoriesByTags(IEnumerable<string> tags)
        {
            var tagSet = new HashSet<string>(tags.Select(t => t.ToLowerInvariant()));
            var suggestions = new List<(DefaultCategoryDefinition Definition, int MatchCount)>();

            foreach (var category in DefaultCategories)
            {
                var matchCount = category.MatchingTags.Count(tag => tagSet.Contains(tag.ToLowerInvariant()));
                if (matchCount > 0)
                {
                    suggestions.Add((category, matchCount));
                }
            }

            return suggestions
                .OrderByDescending(s => s.MatchCount)
                .Select(s => s.Definition)
                .ToList();
        }
    }
}