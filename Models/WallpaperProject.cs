using System.Collections.Generic;
using Newtonsoft.Json;

namespace WallpaperEngine.Models
{
    public class WallpaperProject
    {
        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("file")]
        public string File { get; set; } = string.Empty;

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
    }
}