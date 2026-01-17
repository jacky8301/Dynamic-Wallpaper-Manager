// Settings.cs - 设置数据模型
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

public class ApplicationSettings : INotifyPropertyChanged
{
    private string _wallpaperEnginePath;

    public string WallpaperEnginePath
    {
        get => _wallpaperEnginePath;
        set
        {
            _wallpaperEnginePath = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
