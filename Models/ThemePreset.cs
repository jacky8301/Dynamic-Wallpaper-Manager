using System.Windows.Media;
using Color = System.Windows.Media.Color;

namespace WallpaperEngine.Models {
    /// <summary>
    /// 主题配色预设，定义主色调和强调色
    /// </summary>
    public class ThemePreset {
        public string Name { get; }
        public Color PrimaryColor { get; }
        public Color SecondaryColor { get; }
        public SolidColorBrush PrimaryBrush { get; }
        public SolidColorBrush SecondaryBrush { get; }

        public ThemePreset(string name, Color primaryColor, Color secondaryColor)
        {
            Name = name;
            PrimaryColor = primaryColor;
            SecondaryColor = secondaryColor;
            PrimaryBrush = new SolidColorBrush(primaryColor);
            SecondaryBrush = new SolidColorBrush(secondaryColor);
        }

        public static List<ThemePreset> Presets { get; } = new() {
            new("经典紫", Color.FromRgb(103, 58, 183), Color.FromRgb(205, 220, 57)),
            new("海洋蓝", Color.FromRgb(33, 150, 243), Color.FromRgb(0, 188, 212)),
            new("翡翠绿", Color.FromRgb(0, 150, 136), Color.FromRgb(76, 175, 80)),
            new("暖阳橙", Color.FromRgb(255, 152, 0), Color.FromRgb(255, 235, 59)),
            new("玫瑰红", Color.FromRgb(233, 30, 99), Color.FromRgb(156, 39, 176)),
            new("石墨灰", Color.FromRgb(96, 125, 139), Color.FromRgb(158, 158, 158)),
        };
    }
}
