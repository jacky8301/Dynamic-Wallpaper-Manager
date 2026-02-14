namespace WallpaperEngine.ViewModels {
    public enum ScanResultType {
        New,
        Updated,
        Skipped
    }

    public class ScanProgress {
        public int Percentage { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string? CurrentFolder { get; set; }
        public string? Status { get; set; }
        public int NewCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedCount { get; set; }
    }
}
