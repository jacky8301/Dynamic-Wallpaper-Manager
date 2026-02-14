namespace WallpaperEngine.ViewModels {
    public class ScanProgress {
        public int Percentage { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string? CurrentFolder { get; set; }
        public string? Status { get; set; }
    }
}
