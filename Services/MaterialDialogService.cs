using MaterialDesignThemes.Wpf;
using System.Windows;
using WallpaperEngine.ViewModels;
using WallpaperEngine.Views;
using WallpaperEngine.Services;

namespace WallpaperEngine.Services {
    /// <summary>
    /// Material Design 对话框静态服务，提供确认、输入和错误提示等模态对话框
    /// </summary>
    public static class MaterialDialogService {
        /// <summary>
        /// 使用 DialogHost 模态显示自定义对话框
        /// </summary>
        /// <param name="parameters">对话框配置参数</param>
        /// <returns>对话框结果，包含用户确认状态和附加数据</returns>
        public static async Task<MaterialDialogResult> ShowDialogAsync(MaterialDialogParams parameters)
        {
            var view = new ConfirmationDialog();
            view.DataContext = new ConfirmationDialogViewModel(parameters);

            var result = await DialogHost.Show(view, parameters.DialogHost);
            return result as MaterialDialogResult ?? new MaterialDialogResult { Confirmed = false };
        }

        /// <summary>
        /// 显示带文本输入框的模态对话框
        /// </summary>
        /// <param name="title">对话框标题</param>
        /// <param name="message">提示信息</param>
        /// <param name="placeholder">输入框占位文本</param>
        /// <param name="dialogHost">DialogHost 标识符</param>
        /// <returns>对话框结果，Data 属性包含用户输入的文本</returns>
        public static async Task<MaterialDialogResult> ShowInputAsync(string title, string message, string placeholder = "", string dialogHost = "MainRootDialog")
        {
            var view = new InputDialog();
            view.DataContext = new InputDialogViewModel(title, message, placeholder, dialogHost);

            var result = await DialogHost.Show(view, dialogHost);
            return result as MaterialDialogResult ?? new MaterialDialogResult { Confirmed = false };
        }
        /// <summary>
        /// 快速显示确认对话框
        /// </summary>
        /// <param name="message">确认提示信息</param>
        /// <param name="title">对话框标题，默认为"确认"</param>
        /// <returns>用户点击确认返回 true，取消返回 false</returns>
        public static async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var parameters = new MaterialDialogParams {
                Title = title,
                Message = message
            };

            var result = await ShowDialogAsync(parameters);
            return result.Confirmed;
        }
        /// <summary>
        /// 快速显示错误提示对话框（无取消按钮）
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="title">对话框标题，默认为"确认"</param>
        /// <returns>用户点击确认返回 true</returns>
        public static async Task<bool> ShowErrorAsync(string message, string title = "确认")
        {
            var parameters = new MaterialDialogParams {
                Title = title,
                Message = message,
                ShowCancelButton = false,
                DialogType = DialogType.Error
            };

            var result = await ShowDialogAsync(parameters);
            return result.Confirmed;
        }
    }
}
