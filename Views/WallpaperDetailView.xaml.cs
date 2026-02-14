using CommunityToolkit.Mvvm.DependencyInjection;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    /// <summary>
    /// 壁纸详情用户控件，展示壁纸的详细信息并绑定 WallpaperDetailViewModel
    /// </summary>
    public partial class WallpaperDetailView : System.Windows.Controls.UserControl {

        WallpaperDetailViewModel ViewModel = Ioc.Default.GetService<WallpaperDetailViewModel>();
        public WallpaperDetailView()
        {
            InitializeComponent();
            this.DataContext = ViewModel;
            ViewModel.Initialize();            
        }
    }
}
