using MaterialDesignThemes.Wpf;
using Serilog;
using WallpaperEngine.Models;

namespace WallpaperEngine.Services {
    /// <summary>
    /// 主题服务，使用 MaterialDesign PaletteHelper 在运行时切换配色方案
    /// </summary>
    public class ThemeService {
        private readonly PaletteHelper _paletteHelper = new();

        /// <summary>
        /// 应用指定的配色预设
        /// </summary>
        public void ApplyPreset(string presetName)
        {
            ThemePreset? preset = ThemePreset.Presets.Find(p => p.Name == presetName);
            if (preset == null) {
                Log.Warning("未找到主题预设: {Name}，使用默认", presetName);
                preset = ThemePreset.Presets[0];
            }

            ApplyPreset(preset);
        }

        /// <summary>
        /// 应用指定的配色预设
        /// </summary>
        public void ApplyPreset(ThemePreset preset)
        {
            try {
                Theme theme = (Theme)_paletteHelper.GetTheme();
                theme.SetPrimaryColor(preset.PrimaryColor);
                theme.SetSecondaryColor(preset.SecondaryColor);
                _paletteHelper.SetTheme(theme);
                Log.Information("已切换主题配色: {Name}", preset.Name);
            } catch (Exception ex) {
                Log.Error(ex, "切换主题配色失败");
            }
        }
    }
}
