using CommunityToolkit.Mvvm.DependencyInjection;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
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
