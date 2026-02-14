namespace WallpaperEngine.Services {
    /// <summary>
    /// 对话框返回结果类，包含用户确认状态和附加数据
    /// </summary>
    public class MaterialDialogResult {
        /// <summary>
        /// 用户是否点击了确认按钮
        /// </summary>
        public bool Confirmed { get; set; }

        /// <summary>
        /// 对话框返回的附加数据（如输入框中的文本）
        /// </summary>
        public object Data { get; set; }
    }
}
