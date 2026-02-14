// Settings.cs - 设置数据模型
using CommunityToolkit.Mvvm.ComponentModel;
public partial class ApplicationSettings : ObservableObject {
    [ObservableProperty]
    private string _wallpaperEnginePath;
}
