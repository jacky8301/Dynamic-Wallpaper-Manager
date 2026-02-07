using WallpaperEngine.Services;
namespace WallpaperEngine.Services {
    public class MaterialDialogParams {
        public string DialogHost { get; set; } = "MainRootDialog";
        public string Title { get; set; } = "提示";
        public string Message { get; set; }
        public string ConfirmButtonText { get; set; } = "确认";
        public string CancelButtonText { get; set; } = "取消";
        public bool ShowCancelButton { get; set; } = true;
        public DialogType DialogType { get; set; } = DialogType.Confirmation;
    }
}
