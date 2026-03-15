using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的筛选部分，包含壁纸搜索、分类筛选和收藏筛选逻辑
    /// </summary>
    public partial class MainViewModel {
        /// <summary>防止SelectedCategory和SelectedCategoryId之间递归更新的标志</summary>
        private bool _updatingSelection;

        /// <summary>
        /// 壁纸筛选谓词，根据搜索文本、分类和收藏状态过滤壁纸
        /// </summary>
        /// <param name="obj">待筛选的壁纸对象</param>
        /// <returns>是否满足筛选条件</returns>
        private bool FilterWallpapers(object obj)
        {
            if (obj is not WallpaperItem wallpaper) return false;

            bool matchesSearch = string.IsNullOrEmpty(SearchText) ||
                               (wallpaper.Project.Title?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                               (wallpaper.Project.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                               (wallpaper.Project.Tags?.Any(t => t.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) == true);

            bool matchesCategory = SelectedCategoryId == CategoryConstants.ALL_CATEGORIES_ID || wallpaper.CategoryId == SelectedCategoryId;
            bool matchesAdultFilter = !HideAdultContent || (wallpaper.Project.ContentRating != "Mature" && wallpaper.Project.ContentRating != "Questionable");

            return matchesSearch && matchesCategory && matchesAdultFilter;
        }

        /// <summary>搜索文本变更时刷新壁纸视图</summary>
        partial void OnSearchTextChanged(string value)
        {
            WallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }
        /// <summary>用户从ComboBox选择分类时，同步更新SelectedCategoryId以触发筛选</summary>
        partial void OnSelectedCategoryChanged(CategoryItem? value)
        {
            if (!_updatingSelection && value != null)
            {
                _updatingSelection = true;
                try
                {
                    SelectedCategoryId = value.Id;
                }
                finally
                {
                    _updatingSelection = false;
                }
            }
            // 重新从数据库加载壁纸，确保特定分类下的壁纸不会因LIMIT被截断
            _ = LoadWallpapersAsync();
        }

        /// <summary>选中分类变更时重新从数据库加载壁纸（因为LIMIT限制，客户端筛选可能遗漏数据）</summary>
        partial void OnSelectedCategoryIdChanged(string value)
        {
            if (!_updatingSelection)
            {
                _updatingSelection = true;
                try
                {
                    // 根据ID从Categories集合中查找对应的CategoryItem
                    var category = Categories.FirstOrDefault(c => c.Id == value);
                    SelectedCategory = category;
                }
                finally
                {
                    _updatingSelection = false;
                }
            }

            // 重新从数据库加载壁纸，确保特定分类下的壁纸不会因LIMIT被截断
            _ = LoadWallpapersAsync();
        }

        /// <summary>成人内容过滤状态变更时刷新壁纸视图</summary>
        partial void OnHideAdultContentChanged(bool value)
        {
            WallpapersView.Refresh();
            OnPropertyChanged(nameof(WallpaperCount));
        }
        /// <summary>标签页切换时切换活跃视图</summary>
        partial void OnCurrentTabChanged(int value)
        {
            // 切换标签页时清空选中状态，使每个标签页的选中列表独立
            ClearSelection();
            SelectedWallpaper = null;
            _lastSelectedItem = null;

            OnPropertyChanged(nameof(WallpaperCount));

            // 切换到收藏页面时通知 FavoriteViewModel 加载数据
            if (value == 1)
            {
                var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
                if (favoriteVm != null)
                {
                    _ = favoriteVm.LoadFavoritesAsync();
                }
            }

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
        /// 重置分类筛选到"所有分类"
        /// </summary>
        [RelayCommand]
        private void ResetCategory()
        {
            if (SelectedCategoryId != CategoryConstants.ALL_CATEGORIES_ID)
            {
                SelectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;
            }
        }

        /// <summary>
        /// 清除搜索命令，重置搜索文本、分类和标签页，重新加载壁纸
        /// </summary>
        [RelayCommand]
        private async Task ClearSearch()
        {
            SearchText = string.Empty;
            SelectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;
            CurrentTab = 0;

            await LoadWallpapersAsync();
        }
    }
}
