using WallpaperEngine.ViewModels;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
namespace WallpaperEngine.Views {
    public partial class WallpaperDetailView : System.Windows.Controls.UserControl {
        
        public WallpaperDetailView(DatabaseManager dbManager, WallpaperItem wallpaper)
        {
            InitializeComponent();
            this.DataContext = new WallpaperDetailViewModel(dbManager);
            var vm = DataContext as WallpaperDetailViewModel;
            vm.Initialize(wallpaper);
        }
        public WallpaperDetailView()
        {
            InitializeComponent();
        }
    }
}
