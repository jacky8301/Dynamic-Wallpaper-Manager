using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;
using WallpaperEngine.Views;

namespace WallpaperEngine.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly DatabaseManager _dbManager;
        private readonly WallpaperScanner _scanner;
        private readonly WallpaperPlayer _player;
        private readonly IncrementalScanner _incrementalScanner;
        private string _currentSearchTerm = string.Empty;

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

        [ObservableProperty]
        private string _lastScanTime = "从未扫描";

        [ObservableProperty]
        private bool _isIndeterminate;

        [ObservableProperty]
        private ObservableCollection<DatabaseManager.ScanInfo> _scanHistory = new();

        [ObservableProperty]
        private bool _useIncrementalScan = true; // 默认启用增量扫描

        [ObservableProperty]
        private int _newFoundCount;

        [ObservableProperty]
        private int _updatedCount;

        [ObservableProperty]
        private int _skippedCount;

        public ICollectionView WallpapersView { get; }

        public List<string> Categories { get; } = new List<string>
        {
            "所有分类", "未分类", "自然", "抽象", "游戏", "动漫", "科幻", "风景", "建筑", "动物"
        };

        public MainViewModel()
        {
            _dbManager = new DatabaseManager();
            _scanner = new WallpaperScanner(_dbManager);
            _player = new WallpaperPlayer();
            _incrementalScanner = new IncrementalScanner(_dbManager);

            WallpapersView = CollectionViewSource.GetDefaultView(Wallpapers);
            WallpapersView.Filter = FilterWallpapers;

            LoadWallpapers();
            CheckLastScanTime();
            LoadScanHistory();
        }

        //// 带参数的重载版本，支持双击预览
        [RelayCommand]
        private void PreviewWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                SelectedWallpaper = wallpaper; // 更新选中项
                OpenPreviewWindow(wallpaper);
            } else if (parameter is string wallpaperId) {
                // 通过ID查找壁纸
                var myWallpaper = Wallpapers.FirstOrDefault(w => w.Id == wallpaperId);
                if (myWallpaper != null)
                {
                    SelectedWallpaper = myWallpaper;
                    OpenPreviewWindow(myWallpaper);
                }
            }
        }

        // 实际的预览窗口打开逻辑
        private void OpenPreviewWindow(WallpaperItem wallpaper)
        {
            if (wallpaper?.Project == null)
            {
                System.Windows.MessageBox.Show("壁纸数据无效，无法预览", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var previewWindow = new PreviewWindow(wallpaper)
                {
                    Owner = System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                // 处理预览窗口关闭后的回调
                previewWindow.Closed += (s, e) =>
                {
                    // 可以在这里处理预览后的操作，比如应用壁纸
                    if (previewWindow.DialogResult == true)
                    {
                        // 用户点击了"应用壁纸"
                        
                    }
                };

                previewWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"打开预览窗口失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ScanWallpapersAsync()
        {
            if (IsScanning) return;

            try
            {
                var dialog = new FolderBrowserDialog
                {
                    Description = "选择壁纸文件夹根目录",
                    ShowNewFolderButton = false,
                    RootFolder = Environment.SpecialFolder.MyComputer,
                    SelectedPath = GetLastUsedFolder()
                };

                var result = dialog.ShowDialog();
                if (result == DialogResult.OK && !string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    await StartScanningAsync(dialog.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                HandleScanError(ex);
            }
        }

        [RelayCommand]
        private void LoadScanHistory()
        {
            var history = _dbManager.GetScanHistory();
            ScanHistory.Clear();
            foreach (var item in history)
            {
                ScanHistory.Add(item);
            }
        }

        [RelayCommand]
        private void ClearScanHistory()
        {
            var result = System.Windows.MessageBox.Show(
                "确定要清除所有扫描历史记录吗？",
                "确认清除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // 这里需要添加清除历史记录的方法
                LoadScanHistory();
            }
        }

        [RelayCommand]
        private void CancelScan()
        {
            if (IsScanning)
            {
                _incrementalScanner.Cancel();
                ScanStatus = "正在取消扫描...";
            }
        }

        [RelayCommand]
        private void QuickScan()
        {
            var defaultWallpaperPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Wallpapers"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Wallpaper Engine"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonPictures), "Wallpapers")
            };

            foreach (var path in defaultWallpaperPaths)
            {
                if (Directory.Exists(path))
                {
                    _ = StartScanningAsync(path);
                    return;
                }
            }

            _ = ScanWallpapersAsync();
        }

        [RelayCommand]
        private void SearchWallpapers()
        {
            LoadWallpapers();
        }

        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SelectedCategory = "所有分类";
            ShowFavoritesOnly = false;
            LoadWallpapers();
        }

        [RelayCommand]
        private void ApplyWallpaper()
        {
            if (SelectedWallpaper != null)
            {
                _player.Apply(SelectedWallpaper);
            }
        }

        [RelayCommand]
        private void DeleteWallpaper()
        {
            if (SelectedWallpaper != null)
            {
                try
                {
                    if (Directory.Exists(SelectedWallpaper.FolderPath))
                    {
                        Directory.Delete(SelectedWallpaper.FolderPath, true);
                    }
                    _dbManager.DeleteWallpaper(SelectedWallpaper.Id);
                    Wallpapers.Remove(SelectedWallpaper);

                    System.Windows.MessageBox.Show("壁纸删除成功", "成功",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"删除失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // 切换收藏状态的命令
        [RelayCommand]
        private void ToggleFavorite(object parameter)
        {
            if (parameter is WallpaperItem wallpaper)
            {
                // 1. 切换内存中的状态
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
                wallpaper.FavoritedDate = wallpaper.IsFavorite ? DateTime.Now : DateTime.MinValue;

                // 2. 立即更新数据库[7](@ref)
                try
                {
                    _dbManager.ToggleFavorite(wallpaper.Id, wallpaper.IsFavorite);
                }
                catch (Exception ex)
                {
                    // 处理错误，例如通知用户
                    System.Diagnostics.Debug.WriteLine($"更新收藏状态失败: {ex.Message}");
                    // 可选：恢复内存中的状态
                    wallpaper.IsFavorite = !wallpaper.IsFavorite;
                }

                // 3. 更新界面集合（如果需要，例如有筛选）
                // 例如，如果当前正在显示“仅收藏”，并且取消了收藏，可能需要从视图中移除该项。
                if (ShowFavoritesOnly && !wallpaper.IsFavorite)
                {
                    // 可能会在 Wallpapers 集合中移除该项，取决于你的UI逻辑。
                    // 或者简单地刷新视图：WallpapersView.Refresh();
                }
            }
        }

        private void ShowNotification(string message)
        {
            // 可以使用 MaterialDesign 的 Snackbar 或其他通知机制
            System.Diagnostics.Debug.WriteLine($"通知: {message}");
        }

        // 转到壁纸目录命令
        [RelayCommand]
        private void GoToWallpaperDirectory(object parameter)
        {
            
            if (parameter is WallpaperItem wallpaper)
            {
                SelectedWallpaper = wallpaper;
                OpenWallpaperDirectory(wallpaper);
            }
        }

        // 打开壁纸目录的具体实现
        private void OpenWallpaperDirectory(WallpaperItem wallpaper)
        {
            try
            {
                if (Directory.Exists(wallpaper.FolderPath))
                {
                    // 使用explorer打开目录
                    Process.Start("explorer.exe", wallpaper.FolderPath);
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        $"壁纸目录不存在：{wallpaper.FolderPath}",
                        "错误",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"打开目录失败：{ex.Message}",
                    "错误",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task StartScanningAsync(string folderPath)
        {
            IsScanning = true;
            ScanStatus = "正在准备扫描...";
            CurrentScanFolder = folderPath;
            ScannedCount = 0;
            TotalCount = 0;
            NewFoundCount = 0;
            UpdatedCount = 0;
            SkippedCount = 0;

            try
            {
                if (UseIncrementalScan)
                {
                    await PerformIncrementalScanAsync(folderPath);
                }
                else
                {
                    await PerformFullScanAsync(folderPath);
                }
            }
            catch (Exception ex)
            {
                HandleScanError(ex);
            }
            finally
            {
                IsScanning = false;
                LoadWallpapers();
                LoadScanHistory();
            }
        }

        private async Task PerformIncrementalScanAsync(string folderPath)
        {
            ScanStatus = "正在执行增量扫描...";

            var progress = new Progress<ScanProgress>(UpdateProgress);
            var stats = await _incrementalScanner.ScanIncrementallyAsync(folderPath, progress);

            ScanStatus = $"增量扫描完成！\n" +
                        $"新增: {stats.NewWallpapers}, 更新: {stats.UpdatedWallpapers}, 跳过: {stats.SkippedFolders}\n" +
                        $"耗时: {stats.DurationMs / 1000.0:F1}秒";

            NewFoundCount = stats.NewWallpapers;
            UpdatedCount = stats.UpdatedWallpapers;
            SkippedCount = stats.SkippedFolders;

            ShowScanNotification(stats);
        }

        private async Task PerformFullScanAsync(string folderPath)
        {
            ScanStatus = "正在执行全量扫描...";

            var progress = new Progress<ScanProgress>(UpdateProgress);
            var result = await _scanner.ScanWallpapersAsync(folderPath, progress);

            ScanStatus = result ? "全量扫描完成！" : "扫描被取消";
        }




        //private async Task StartScanningAsync(string folderPath)
        //{
        //    IsScanning = true;
        //    IsIndeterminate = true;
        //    ScanStatus = "正在准备扫描...";
        //    CurrentScanFolder = folderPath;
        //    ScannedCount = 0;
        //    TotalCount = 0;
        //    NewFoundCount = 0;
        //    UpdatedCount = 0;
        //    SkippedCount = 0;

        //    try
        //    {
        //        SaveLastUsedFolder(folderPath);

        //        TotalCount = await CountWallpaperFoldersAsync(folderPath);

        //        if (TotalCount == 0)
        //        {
        //            ScanStatus = "未找到壁纸文件夹";
        //            System.Windows.MessageBox.Show("在选择的文件夹中未找到有效的壁纸文件夹。\n请确保文件夹包含project.json文件。",
        //                "扫描结果", MessageBoxButton.OK, MessageBoxImage.Information);
        //            return;
        //        }

        //        IsIndeterminate = false;
        //        ScanStatus = $"正在扫描 {TotalCount} 个壁纸文件夹...";

        //        var progress = new Progress<ScanProgress>(UpdateProgress);
        //        var result = await _scanner.ScanWallpapersAsync(folderPath, progress);

        //        ScanStatus = result ? $"扫描完成！共处理 {ScannedCount} 个壁纸" : "扫描被取消";
        //        LastScanTime = $"最后扫描: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

        //        LoadWallpapers();

        //        if (result)
        //        {
        //            ShowScanSummary();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        HandleScanError(ex);
        //    }
        //    finally
        //    {
        //        IsScanning = false;
        //        IsIndeterminate = false;
        //    }
        //}

        private async Task<int> CountWallpaperFoldersAsync(string rootPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(rootPath)) return 0;

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
            });
        }

        private void UpdateProgress(ScanProgress progress)
        {
            ScanProgress = progress.Percentage;
            ScannedCount = progress.ProcessedCount;
            TotalCount = progress.TotalCount;

            if (progress.CurrentFolder != null)
            {
                var folderName = Path.GetFileName(progress.CurrentFolder);
                ScanStatus = $"正在扫描: {folderName} ({ScannedCount}/{TotalCount})";
            }

            if (progress.Status != null)
            {
                ScanStatus = progress.Status;
            }
        }

        private void ShowScanNotification(IncrementalScanner.ScanStatistics stats)
        {
            var message = $"扫描完成！\n" +
                         $"扫描文件夹: {Path.GetFileName(CurrentScanFolder)}\n" +
                         $"总文件夹: {stats.TotalFolders}\n" +
                         $"新增壁纸: {stats.NewWallpapers}\n" +
                         $"更新壁纸: {stats.UpdatedWallpapers}\n" +
                         $"跳过文件夹: {stats.SkippedFolders}\n" +
                         $"耗时: {stats.DurationMs / 1000.0:F1}秒";

            System.Windows.MessageBox.Show(message, "扫描完成",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void HandleScanError(Exception ex)
        {
            ScanStatus = "扫描过程中发生错误";

            System.Diagnostics.Debug.WriteLine($"扫描错误: {ex.Message}");

            var errorMessage = ex switch
            {
                UnauthorizedAccessException => "没有权限访问指定的文件夹。请以管理员身份运行或选择其他文件夹。",
                DirectoryNotFoundException => "指定的文件夹不存在或已被移动。",
                IOException => "文件访问错误，可能是文件正在被其他程序使用。",
                _ => $"扫描壁纸时发生错误:\n{ex.Message}"
            };

            System.Windows.MessageBox.Show(errorMessage, "扫描错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void ShowScanSummary()
        {
            var newWallpapers = Wallpapers.Count - _previousWallpaperCount;
            _previousWallpaperCount = Wallpapers.Count;

            var message = $"壁纸扫描完成！\n" +
                         $"扫描文件夹: {Path.GetFileName(CurrentScanFolder)}\n" +
                         $"发现壁纸数量: {ScannedCount}\n" +
                         $"新增壁纸: {newWallpapers}";

            if (newWallpapers > 0)
            {
                System.Windows.MessageBox.Show(message, "扫描完成",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckLastScanTime()
        {
            // 简化实现
            if (Properties.Settings.Default.LastScanTime != DateTime.MinValue)
            {
                LastScanTime = $"最后扫描: {Properties.Settings.Default.LastScanTime:yyyy-MM-dd HH:mm:ss}";
            }
        }

        private string GetLastUsedFolder()
        {
            return Properties.Settings.Default.LastScanFolder ??
                   Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        private void SaveLastUsedFolder(string folderPath)
        {
            Properties.Settings.Default.LastScanFolder = folderPath;
            Properties.Settings.Default.LastScanTime = DateTime.Now;
            Properties.Settings.Default.Save();
        }

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

        private int _previousWallpaperCount = 0;

        partial void OnSearchTextChanged(string value) => WallpapersView.Refresh();
        partial void OnSelectedCategoryChanged(string value) => WallpapersView.Refresh();
        partial void OnShowFavoritesOnlyChanged(bool value) => WallpapersView.Refresh();

        // 选择命令
        [RelayCommand]
        private void SelectWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper)
            {
                // 更新选择状态
                UpdateSelection(wallpaper);

                // 可以在这里触发预览或其他操作
                PreviewWallpaperCommand.Execute(wallpaper);
            }
        }

        [RelayCommand]
        private async Task QuickIncrementalScanAsync()
        {
            if (IsScanning) return;

            // 获取上次扫描的文件夹
            var lastScan = ScanHistory.FirstOrDefault();
            if (lastScan != null)
            {
                if (Directory.Exists(lastScan.ScanPath))
                {
                    await StartScanningAsync(lastScan.ScanPath);
                    return;
                }
            }

            // 如果没有历史记录，让用户选择
            await ScanWallpapersAsync();
        }

        private void UpdateSelection(WallpaperItem selectedWallpaper)
        {
            // 清除之前的选择
            foreach (var wallpaper in Wallpapers.Where(w => w.IsSelected))
            {
                wallpaper.IsSelected = false;
            }

            // 设置新的选择
            selectedWallpaper.IsSelected = true;
            SelectedWallpaper = selectedWallpaper;
        }

        // 监听集合变化，确保选择状态同步
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(Wallpapers))
            {
                // 当壁纸集合更新时，重置选择状态
                SelectedWallpaper = null;
            }
        }
    }

    public class ScanProgress
    {
        public int Percentage { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentFolder { get; set; }
        public string Status { get; set; }
    }
}