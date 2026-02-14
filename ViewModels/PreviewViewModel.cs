using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 壁纸预览视图模型，用于预览窗口的数据绑定
    /// </summary>
    internal class PreviewViewModel : ObservableObject {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        /// <summary>
        /// 初始化预览视图模型，加载应用程序设置
        /// </summary>
        /// <param name="settingsService">设置服务</param>
        public PreviewViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
        }
    }
}
