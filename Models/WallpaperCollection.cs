using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.Models {
    public partial class WallpaperCollection : ObservableObject {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [ObservableProperty]
        private string _name = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
