using Newtonsoft.Json;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.Models {
    public  partial class WallpaperProject : ObservableObject{
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("file")]
        [ObservableProperty]
        private string file  = string.Empty;

        [JsonProperty("preview")]
        public string Preview { get; set; } = "preview.jpg";

        [JsonProperty("type")]
        public string Type { get; set; } = "video";

        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new List<string>();

        [JsonProperty("visibility")]
        public string Visibility { get; set; } = "public";

        [JsonProperty("contentrating")]
        public string ContentRating { get; set; } = "Everyone";

        [JsonProperty("category")]
        public string Category { get; set; } = "未分类";
    }
}