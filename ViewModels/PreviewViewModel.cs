using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WallpaperEngine.ViewModels
{
    internal class PreviewViewModel : ObservableObject
    {
        private readonly ApplicationSettings _settings;
        private readonly ISettingsService _settingsService;

        public PreviewViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _settings = _settingsService.LoadSettings();
            // 初始化命令
        }

    }
}
