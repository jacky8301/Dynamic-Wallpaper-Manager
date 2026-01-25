using CommunityToolkit.Mvvm.DependencyInjection;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views {
    public partial class WallpaperDetailView : System.Windows.Controls.UserControl {

        public WallpaperDetailView()
        {
            InitializeComponent();
            var vm = Ioc.Default.GetService<WallpaperDetailViewModel>();
            this.DataContext = vm;
            vm.Initialize(null);

        }

        private void OnClickFavoriteButton(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void OnClickPreviewButton(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void OnClickApplyWallpaperButton(object sender, System.Windows.RoutedEventArgs e)
        {

        }

        private void OnClickDeleteWallpaperButton(object sender, System.Windows.RoutedEventArgs e)
        {

        }
    }
}
