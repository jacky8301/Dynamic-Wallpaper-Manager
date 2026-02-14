using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 壁纸项目模型，对应 project.json 文件中的配置信息
    /// </summary>
    public  partial class WallpaperProject : ObservableObject{
        /// <summary>
        /// 壁纸标题
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 壁纸描述信息
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 壁纸主要内容文件名（如视频文件或网页文件）
        /// </summary>
        [JsonProperty("file")]
        [ObservableProperty]
        private string file  = string.Empty;

        /// <summary>
        /// 预览图文件名
        /// </summary>
        [JsonProperty("preview")]
        public string Preview { get; set; } = "preview.jpg";

        /// <summary>
        /// 壁纸类型（如 video、web、scene 等）
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; } = "video";

        /// <summary>
        /// 壁纸标签列表
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// 壁纸可见性（如 public、private）
        /// </summary>
        [JsonProperty("visibility")]
        public string Visibility { get; set; } = "public";

        /// <summary>
        /// 内容分级（如 Everyone、Questionable、Mature）
        /// </summary>
        [JsonProperty("contentrating")]
        public string ContentRating { get; set; } = "Everyone";

        /// <summary>
        /// 壁纸分类
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; } = "未分类";
    }
}