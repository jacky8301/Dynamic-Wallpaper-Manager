using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 主题配色预设，定义主色调和强调色
    /// </summary>
    public class ThemePreset {
        public string Name { get; set; } = string.Empty;
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }
        public SolidColorBrush PrimaryBrush => new(PrimaryColor);
        public SolidColorBrush SecondaryBrush => new(SecondaryColor);

        /// <summary>
        /// 所有内置配色预设
        /// </summary>
        public static List<ThemePreset> Presets { get; } = new() {
            new ThemePreset { Name = "经典紫", PrimaryColor = Color.FromRgb(103, 58, 183), SecondaryColor = Color.FromRgb(205, 220, 57) },
            new ThemePreset { Name = "海洋蓝", PrimaryColor = Color.FromRgb(33, 150, 243), SecondaryColor = Color.FromRgb(0, 188, 212) },
            new ThemePreset { Name = "翡翠绿", PrimaryColor = Color.FromRgb(0, 150, 136), SecondaryColor = Color.FromRgb(76, 175, 80) },
            new ThemePreset { Name = "暖阳橙", PrimaryColor = Color.FromRgb(255, 152, 0), SecondaryColor = Color.FromRgb(255, 235, 59) },
            new ThemePreset { Name = "玫瑰红", PrimaryColor = Color.FromRgb(233, 30, 99), SecondaryColor = Color.FromRgb(156, 39, 176) },
            new ThemePreset { Name = "石墨灰", PrimaryColor = Color.FromRgb(96, 125, 139), SecondaryColor = Color.FromRgb(158, 158, 158) },
        };
    }
}
