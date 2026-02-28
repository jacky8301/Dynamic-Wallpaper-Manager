using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的筛选部分，包含壁纸搜索、分类筛选和收藏筛选逻辑
    /// </summary>
    public partial class MainViewModel {
        /// <summary>
        /// 壁纸筛选谓词，根据搜索文本、分类和收藏状态过滤壁纸
        /// </summary>
        /// <param name="obj">待筛选的壁纸对象</param>
        /// <returns>是否满足筛选条件</returns>
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

        /// <summary>搜索文本变更时刷新壁纸视图</summary>
        partial void OnSearchTextChanged(string value)
        {
            WallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }
        /// <summary>选中分类变更时刷新壁纸视图</summary>
        partial void OnSelectedCategoryChanged(string value)
        {
            WallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }
        /// <summary>收藏筛选状态变更时刷新壁纸视图</summary>
        partial void OnShowFavoritesOnlyChanged(bool value)
        {
            WallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }
        /// <summary>标签页切换时同步收藏筛选状态</summary>
        partial void OnCurrentTabChanged(int value)
        {
            ShowFavoritesOnly = value == 1;
            // 切换标签页时清空选中状态，使每个标签页的选中列表独立
            ClearSelection();
            SelectedWallpaper = null;
            _lastSelectedItem = null;

            // 切换到合集页面时，如果没有选中合集，则自动选中第一个合集
            if (value == 2)
            {
                var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                if (collectionVm != null && collectionVm.SelectedCollection == null && collectionVm.Collections.Count > 0)
                {
                    collectionVm.SelectedCollection = collectionVm.Collections.First();
                }
            }
        }

        /// <summary>
        /// 搜索壁纸命令，重新从数据库加载壁纸列表
        /// </summary>
        [RelayCommand]
        private async Task SearchWallpapers()
        {
            await LoadWallpapersAsync();
        }

        /// <summary>
        /// 清除搜索命令，重置搜索文本、分类和标签页，重新加载壁纸
        /// </summary>
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
