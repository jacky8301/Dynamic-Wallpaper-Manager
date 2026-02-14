using WallpaperEngine.Services;
namespace WallpaperEngine.Services {
    /// <summary>
    /// 对话框配置参数类，定义对话框的标题、消息、按钮文本和类型等属性
    /// </summary>
    public class MaterialDialogParams {
        /// <summary>
        /// DialogHost 标识符，默认为 "MainRootDialog"
        /// </summary>
        public string DialogHost { get; set; } = "MainRootDialog";

        /// <summary>
        /// 对话框标题，默认为"提示"
        /// </summary>
        public string Title { get; set; } = "提示";

        /// <summary>
        /// 对话框显示的消息内容
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 确认按钮文本，默认为"确认"
        /// </summary>
        public string ConfirmButtonText { get; set; } = "确认";

        /// <summary>
        /// 取消按钮文本，默认为"取消"
        /// </summary>
        public string CancelButtonText { get; set; } = "取消";

        /// <summary>
        /// 是否显示取消按钮，默认为 true
        /// </summary>
        public bool ShowCancelButton { get; set; } = true;

        /// <summary>
        /// 对话框类型，默认为 Confirmation
        /// </summary>
        public DialogType DialogType { get; set; } = DialogType.Confirmation;
    }
}
