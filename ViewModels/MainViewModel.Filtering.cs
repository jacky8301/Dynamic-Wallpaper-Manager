using CommunityToolkit.Mvvm.Input;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class MainViewModel {
        private bool FilterWallpapers(object obj)
        {
            if (obj is not WallpaperItem wallpaper) return false;

            bool matchesSearch = string.IsNullOrEmpty(SearchText) ||
                               wallpaper.Project.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               wallpaper.Project.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                               wallpaper.Project.Tags.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            bool matchesCategory = SelectedCategory == "所有分类" || wallpaper.Category == SelectedCategory;
            bool matchesFavorites = !ShowFavoritesOnly || wallpaper.IsFavorite;

            return matchesSearch && matchesCategory && matchesFavorites;
        }

        partial void OnSearchTextChanged(string value) => WallpapersView.Refresh();
        partial void OnSelectedCategoryChanged(string value) => WallpapersView.Refresh();
        partial void OnShowFavoritesOnlyChanged(bool value) => WallpapersView.Refresh();
        partial void OnCurrentTabChanged(int value)
        {
            ShowFavoritesOnly = value == 1;
        }

        [RelayCommand]
        private async Task SearchWallpapers()
        {
            await LoadWallpapersAsync();
        }

        [RelayCommand]
        private async Task ClearSearch()
        {
            SearchText = string.Empty;
            SelectedCategory = "所有分类";
            CurrentTab = 0;

            await LoadWallpapersAsync();
        }
    }
}
