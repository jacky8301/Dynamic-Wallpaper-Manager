// MainViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseManager _dbManager;
        private readonly WallpaperScanner _scanner;
        private readonly WallpaperPlayer _player;

        [ObservableProperty]
        private ObservableCollection<WallpaperItem> _wallpapers = new();

        [ObservableProperty]
        private WallpaperItem _selectedWallpaper;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "所有分类";

        [ObservableProperty]
        private bool _showFavoritesOnly;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private int _scanProgress;

        [ObservableProperty]
        private string _scanStatus = "准备扫描";

        [ObservableProperty]
        private string _currentScanFolder = string.Empty;

        [ObservableProperty]
        private int _scannedCount;

        [ObservableProperty]
        private int _totalCount;

        public ICollectionView WallpapersView { get; }

        public List<string> Categories { get; } = new List<string>
        {
            "所有分类", "未分类", "自然", "抽象", "游戏", "动漫", "科幻"
        };

        public MainViewModel()
        {
            _dbManager = new DatabaseManager();
            _scanner = new WallpaperScanner(_dbManager);
            _player = new WallpaperPlayer();

            WallpapersView = CollectionViewSource.GetDefaultView(Wallpapers);
            WallpapersView.Filter = FilterWallpapers;

            // 初始化命令
            ScanWallpapersCommand = new AsyncRelayCommand(ExecuteScanWallpapersAsync, CanExecuteScanWallpapers);

            // 加载壁纸
            LoadWallpapers();
        }

        public ICommand ScanWallpapersCommand { get; }

        private bool CanExecuteScanWallpapers()
        {
            return !IsScanning;
        }



        private async Task ExecuteScanWallpapersAsync()
        {
            try
            {
                // 打开文件夹选择对话框
                var dialog = new FolderBrowserDialog
                {
                    Description = "选择壁纸文件夹",
                    ShowNewFolderButton = false,
                    RootFolder = Environment.SpecialFolder.MyComputer
                };

                // 如果用户之前选择过文件夹，可以设置初始路径
                if (!string.IsNullOrEmpty(WallpaperEngine.Properties.Settings.Default.LastScanFolder) &&
                    Directory.Exists(WallpaperEngine.Properties.Settings.Default.LastScanFolder))
                {
                    dialog.SelectedPath = WallpaperEngine.Properties.Settings.Default.LastScanFolder;
                }

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    // 保存选择的文件夹路径
                    WallpaperEngine.Properties.Settings.Default.LastScanFolder = dialog.SelectedPath;
                    WallpaperEngine.Properties.Settings.Default.Save();

                    await StartScanAsync(dialog.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                HandleScanError(ex);
            }
        }

        private async Task StartScanAsync(string folderPath)
        {
            IsScanning = true;
            ScanStatus = "正在扫描壁纸...";
            CurrentScanFolder = folderPath;
            ScannedCount = 0;
            TotalCount = 0;

            try
            {
                // 计算总文件夹数量用于进度显示
                TotalCount = CountWallpaperFolders(folderPath);

                if (TotalCount == 0)
                {
                    ScanStatus = "未找到壁纸文件夹";
                    System.Windows.MessageBox.Show("在选择的文件夹中未找到有效的壁纸文件夹。", "扫描结果",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var progress = new Progress<ScanProgress>(UpdateProgress);
                await _scanner.ScanWallpapersAsync(folderPath, progress);

                ScanStatus = $"扫描完成！共处理 {ScannedCount} 个壁纸";

                // 重新加载壁纸列表
                LoadWallpapers();

                // 显示扫描结果
                ShowScanSummary();
            }
            catch (Exception ex)
            {
                HandleScanError(ex);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private int CountWallpaperFolders(string rootPath)
        {
            try
            {
                if (!Directory.Exists(rootPath))
                    return 0;

                var folders = Directory.GetDirectories(rootPath);
                int count = 0;

                foreach (var folder in folders)
                {
                    var projectFile = Path.Combine(folder, "project.json");
                    if (File.Exists(projectFile))
                    {
                        count++;
                    }
                }

                return count;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private void UpdateProgress(ScanProgress progress)
        {
            ScanProgress = progress.Percentage;
            ScannedCount = progress.ScannedCount;
            TotalCount = progress.TotalCount;

            if (progress.CurrentFolder != null)
            {
                ScanStatus = $"正在扫描: {Path.GetFileName(progress.CurrentFolder)}";
            }
        }

        private void HandleScanError(Exception ex)
        {
            ScanStatus = "扫描过程中发生错误";

            // 记录错误日志
            System.Diagnostics.Debug.WriteLine($"扫描错误: {ex.Message}");

            // 显示错误信息给用户
            System.Windows.MessageBox.Show($"扫描壁纸时发生错误:\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowScanSummary()
        {
            // 可以在这里添加更详细的扫描结果统计
            var message = $"壁纸扫描完成！\n" +
                         $"扫描文件夹: {CurrentScanFolder}\n" +
                         $"处理壁纸数量: {ScannedCount}";

            System.Windows.MessageBox.Show(message, "扫描完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void LoadWallpapers()
        {
            try
            {
                var results = _dbManager.SearchWallpapers(SearchText,
                    SelectedCategory == "所有分类" ? "" : SelectedCategory,
                    ShowFavoritesOnly);

                Wallpapers.Clear();
                foreach (var item in results)
                {
                    Wallpapers.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载壁纸列表失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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

        partial void OnSelectedWallpaperChanged(WallpaperItem value)
        {
            // 通知视图更新预览
            if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.UpdateDetailPreview(value);
            }
        }
    }

    // 进度报告类
    public class ScanProgress
    {
        public int Percentage { get; set; }
        public int ScannedCount { get; set; }
        public int TotalCount { get; set; }
        public string? CurrentFolder { get; set; }
        public string? Status { get; set; }
        public int ProcessedCount { get; set; }
    }
}