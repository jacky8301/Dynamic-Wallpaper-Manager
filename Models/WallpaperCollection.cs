using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 壁纸合集模型，用于将多个壁纸归类到一个集合中
    /// </summary>
    public partial class WallpaperCollection : ObservableObject {
        /// <summary>
        /// 合集的唯一标识符
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 合集名称
        /// </summary>
        [ObservableProperty]
        private string _name = string.Empty;

        /// <summary>
        /// 合集创建日期
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
