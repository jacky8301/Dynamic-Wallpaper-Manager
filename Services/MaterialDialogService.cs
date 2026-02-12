using MaterialDesignThemes.Wpf;
using System.Windows;
using WallpaperEngine.ViewModels;
using WallpaperEngine.Views;
using WallpaperEngine.Services;

namespace WallpaperEngine.Services {
    public static class MaterialDialogService {
        // 模态显示（使用DialogHost）
        public static async Task<MaterialDialogResult> ShowDialogAsync(MaterialDialogParams parameters)
        {
            var view = new ConfirmationDialog();
            view.DataContext = new ConfirmationDialogViewModel(parameters);

            var result = await DialogHost.Show(view, parameters.DialogHost);
            return result as MaterialDialogResult ?? new MaterialDialogResult { Confirmed = false };
        }
        // 快速调用方法
        public static async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
        {
            var parameters = new MaterialDialogParams {
                Title = title,
                Message = message
            };

            var result = await ShowDialogAsync(parameters);
            return result.Confirmed;
        }
        // 快速调用方法
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
