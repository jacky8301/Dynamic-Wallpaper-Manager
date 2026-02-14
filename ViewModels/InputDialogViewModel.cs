using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 输入对话框视图模型，提供文本输入、确认和取消操作
    /// </summary>
    public class InputDialogViewModel : ObservableObject {
        private readonly string _dialogHost;
        private string _inputText = string.Empty;

        /// <summary>对话框标题</summary>
        public string Title { get; }
        /// <summary>对话框提示消息</summary>
        public string Message { get; }
        /// <summary>输入框占位符文本</summary>
        public string Placeholder { get; }

        /// <summary>用户输入的文本</summary>
        public string InputText {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        /// <summary>确认命令</summary>
        public ICommand ConfirmCommand { get; }
        /// <summary>取消命令</summary>
        public ICommand CancelCommand { get; }

        /// <summary>
        /// 初始化输入对话框视图模型
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">提示消息</param>
        /// <param name="placeholder">输入框占位符</param>
        /// <param name="dialogHost">对话框宿主名称</param>
        public InputDialogViewModel(string title, string message, string placeholder = "", string dialogHost = "MainRootDialog")
        {
            Title = title;
            Message = message;
            Placeholder = placeholder;
            _dialogHost = dialogHost;

            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        /// <summary>
        /// 确认操作，关闭对话框并返回输入的文本
        /// </summary>
        private void OnConfirm()
        {
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = true, Data = InputText?.Trim() });
            }
        }

        /// <summary>
        /// 取消操作，关闭对话框
        /// </summary>
        private void OnCancel()
        {
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = false });
            }
        }
    }
}
