using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.ViewModels {
    internal class PreviewViewModel : ObservableObject {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        public PreviewViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
        }
    }
}
