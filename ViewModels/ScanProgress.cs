namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 扫描结果类型枚举
    /// </summary>
    public enum ScanResultType {
        /// <summary>新发现的壁纸</summary>
        New,
        /// <summary>已更新的壁纸</summary>
        Updated,
        /// <summary>已跳过的壁纸</summary>
        Skipped
    }

    /// <summary>
    /// 扫描进度数据类，用于在扫描过程中报告进度信息
    /// </summary>
    public class ScanProgress {
        /// <summary>扫描进度百分比（0-100）</summary>
        public int Percentage { get; set; }
        /// <summary>已处理的壁纸数量</summary>
        public int ProcessedCount { get; set; }
        /// <summary>壁纸总数量</summary>
        public int TotalCount { get; set; }
        /// <summary>当前正在扫描的文件夹路径</summary>
        public string? CurrentFolder { get; set; }
        /// <summary>扫描状态描述文本</summary>
        public string? Status { get; set; }
        /// <summary>新发现的壁纸数量</summary>
        public int NewCount { get; set; }
        /// <summary>已更新的壁纸数量</summary>
        public int UpdatedCount { get; set; }
        /// <summary>已跳过的壁纸数量</summary>
        public int SkippedCount { get; set; }
    }
}
