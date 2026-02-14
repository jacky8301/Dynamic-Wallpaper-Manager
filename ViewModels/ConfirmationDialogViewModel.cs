using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using WallpaperEngine.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 确认对话框视图模型，提供确认和取消操作
    /// </summary>
    public class ConfirmationDialogViewModel : ObservableObject {
        private readonly MaterialDialogParams _parameters;

        /// <summary>对话框标题</summary>
        public string Title => _parameters.Title;
        /// <summary>对话框消息内容</summary>
        public string Message => _parameters.Message;
        /// <summary>确认按钮文本</summary>
        public string ConfirmButtonText => _parameters.ConfirmButtonText;
        /// <summary>取消按钮文本</summary>
        public string CancelButtonText => _parameters.CancelButtonText;
        /// <summary>是否显示取消按钮</summary>
        public bool ShowCancelButton => _parameters.ShowCancelButton;

        /// <summary>确认命令</summary>
        public ICommand ConfirmCommand { get; }
        /// <summary>取消命令</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 初始化确认对话框视图模型
        /// </summary>
        /// <param name="parameters">对话框参数配置</param>
        public ConfirmationDialogViewModel(MaterialDialogParams parameters)
        {
            _parameters = parameters;

            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        /// <summary>
        /// 确认操作，关闭对话框并返回确认结果
        /// </summary>
        private void OnConfirm()
        {
            if (DialogHost.IsDialogOpen(_parameters.DialogHost)) {
                DialogHost.Close(_parameters.DialogHost,
                    new MaterialDialogResult { Confirmed = true });
            }
        }

        /// <summary>
        /// 取消操作，关闭对话框并返回取消结果
        /// </summary>
        private void OnCancel()
        {
            if (DialogHost.IsDialogOpen(_parameters.DialogHost)) {
                DialogHost.Close(_parameters.DialogHost,
                    new MaterialDialogResult { Confirmed = false });
            }
        }
    }
}
