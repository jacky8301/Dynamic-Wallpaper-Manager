using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Serilog;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using WallpaperEngine.Models;
using WallpaperEngine.Services;
using WallpaperEngine.Views;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 主视图模型的操作命令部分，包含壁纸预览、应用、收藏、目录跳转、合集管理、分类管理等命令
    /// </summary>
    public partial class MainViewModel {
        /// <summary>
        /// 预览壁纸命令，支持传入壁纸对象或壁纸ID
        /// </summary>
        /// <param name="parameter">壁纸对象或壁纸ID字符串</param>
        [RelayCommand]
        private async Task PreviewWallpaper(object parameter)
        {
            // 优先使用传入的参数（用户点击了特定壁纸的预览按钮）
            if (parameter is WallpaperItem wp) {
                SelectedWallpaper = wp;
                Log.Information("预览壁纸: {Title}", wp.Project.Title);
                _dataContextService.CurrentWallpaper = wp;
                await OpenPreviewWindowNew(wp);
                return;
            } else if (parameter is string wallpaperId) {
                var myWallpaper = Wallpapers.FirstOrDefault(w => w.Id == wallpaperId);
                if (myWallpaper != null) {
                    SelectedWallpaper = myWallpaper;
                    _dataContextService.CurrentWallpaper = myWallpaper;
                    await OpenPreviewWindowNew(myWallpaper);
                }
                return;
            }

            // 如果没有参数但有选中的壁纸，预览第一个选中的壁纸（例如从工具栏触发）
            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    SelectedWallpaper = selectedWallpaper;
                    Log.Information("预览壁纸: {Title}", selectedWallpaper.Project.Title);
                    _dataContextService.CurrentWallpaper = selectedWallpaper;
                    await OpenPreviewWindowNew(selectedWallpaper);
                }
                return;
            }
        }

        /// <summary>
        /// 根据壁纸类型选择预览方式（Web/Scene类型使用引擎预览，其他类型使用内置预览窗口）
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        private async Task OpenPreviewWindowNew(WallpaperItem wallpaper)
        {
            var type = wallpaper.Project?.Type;
            if (type != null && (type.Equals("web", StringComparison.OrdinalIgnoreCase) || type.Equals("scene", StringComparison.OrdinalIgnoreCase))) {
                var options = new PreviewService.PreviewOptions();

                // 计算预览窗口居中位置
                var mainWindow = System.Windows.Application.Current.MainWindow;
                if (mainWindow != null)
                {
                    // 方法1: 使用WPF的PointToScreen方法进行精确坐标转换（考虑DPI缩放）
                    System.Windows.Point windowTopLeft = new System.Windows.Point(0, 0);
                    System.Windows.Point windowBottomRight = new System.Windows.Point(mainWindow.ActualWidth, mainWindow.ActualHeight);

                    // 将窗口的逻辑坐标转换为屏幕物理坐标
                    System.Windows.Point screenTopLeft = mainWindow.PointToScreen(windowTopLeft);
                    System.Windows.Point screenBottomRight = mainWindow.PointToScreen(windowBottomRight);

                    double mainWindowScreenLeft = screenTopLeft.X;
                    double mainWindowScreenTop = screenTopLeft.Y;
                    double mainWindowScreenWidth = screenBottomRight.X - screenTopLeft.X;
                    double mainWindowScreenHeight = screenBottomRight.Y - screenTopLeft.Y;

                    Log.Debug("主窗口屏幕坐标: Left={Left}, Top={Top}, Width={Width}, Height={Height}",
                        mainWindowScreenLeft, mainWindowScreenTop, mainWindowScreenWidth, mainWindowScreenHeight);

                    // 方法2: 备用方法，使用窗口的Left/Top和DPI缩放因子
                    double dpiScaleX = 1.0;
                    double dpiScaleY = 1.0;
                    var source = PresentationSource.FromVisual(mainWindow);
                    if (source != null && source.CompositionTarget != null)
                    {
                        Matrix matrix = source.CompositionTarget.TransformToDevice;
                        dpiScaleX = matrix.M11;
                        dpiScaleY = matrix.M22;
                        Log.Debug("DPI缩放因子: X={DpiScaleX}, Y={DpiScaleY}", dpiScaleX, dpiScaleY);
                    }

                    // 将预览窗口尺寸从逻辑像素转换为物理像素
                    int previewWidthPhysical = (int)(options.Width * dpiScaleX);
                    int previewHeightPhysical = (int)(options.Height * dpiScaleY);

                    // 确保预览窗口尺寸不超过主窗口（考虑边框）
                    if (previewWidthPhysical > mainWindowScreenWidth * 0.8)
                    {
                        previewWidthPhysical = (int)(mainWindowScreenWidth * 0.8);
                        Log.Debug("调整预览窗口宽度不超过主窗口80%: {NewWidth}", previewWidthPhysical);
                    }
                    if (previewHeightPhysical > mainWindowScreenHeight * 0.8)
                    {
                        previewHeightPhysical = (int)(mainWindowScreenHeight * 0.8);
                        Log.Debug("调整预览窗口高度不超过主窗口80%: {NewHeight}", previewHeightPhysical);
                    }

                    // 更新options中的尺寸为物理像素尺寸，确保与位置计算一致
                    options.Width = previewWidthPhysical;
                    options.Height = previewHeightPhysical;

                    // 计算居中位置（使用物理像素坐标）
                    int x = (int)(mainWindowScreenLeft + (mainWindowScreenWidth - previewWidthPhysical) / 2);
                    int y = (int)(mainWindowScreenTop + (mainWindowScreenHeight - previewHeightPhysical) / 2);

                    Log.Debug("计算居中位置(物理像素): x={X}, y={Y}, 预览窗口尺寸={Width}x{Height}",
                        x, y, previewWidthPhysical, previewHeightPhysical);

                    // 确保位置在屏幕范围内（考虑多个显示器）
                    try
                    {
                        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(x, y));
                        if (screen != null)
                        {
                            var screenBounds = screen.WorkingArea;
                            Log.Debug("屏幕工作区(物理像素): Left={Left}, Top={Top}, Right={Right}, Bottom={Bottom}",
                                screenBounds.Left, screenBounds.Top, screenBounds.Right, screenBounds.Bottom);

                            x = Math.Max(screenBounds.Left, Math.Min(x, screenBounds.Right - previewWidthPhysical));
                            y = Math.Max(screenBounds.Top, Math.Min(y, screenBounds.Bottom - previewHeightPhysical));
                            Log.Debug("调整后位置: x={X}, y={Y}", x, y);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning("获取屏幕信息失败: {Error}", ex.Message);
                    }

                    options.X = x;
                    options.Y = y;

                    // 调试日志
                    Log.Information("最终预览窗口位置(物理像素): 尺寸={Width}x{Height}, 位置=({X},{Y})",
                        previewWidthPhysical, previewHeightPhysical, options.X, options.Y);
                }
                else
                {
                    Log.Warning("无法获取主窗口引用");
                }

                _previewService.PreviewWallpaper(wallpaper, options);
            } else {
                await OpenPreviewWindow(wallpaper);
            }
        }

        /// <summary>
        /// 打开内置预览窗口显示壁纸
        /// </summary>
        /// <param name="wallpaper">要预览的壁纸项</param>
        private async Task OpenPreviewWindow(WallpaperItem wallpaper)
        {
            if (wallpaper?.Project == null) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "壁纸数据无效，无法预览",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }

            try {
                var previewWindow = new PreviewWindow(wallpaper) {
                    Owner = System.Windows.Application.Current.MainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                previewWindow.Closed += (s, e) => {
                    if (previewWindow.DialogResult == true) {
                        // 用户点击了"应用壁纸"
                    }
                };

                previewWindow.ShowDialog();
            } catch (Exception ex) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"打开预览窗口失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Warning }
                );
            }
        }

        /// <summary>
        /// 应用壁纸命令，通过Wallpaper Engine将壁纸设置为桌面壁纸
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void ApplyWallpaper(object parameter)
        {
            // 优先使用传入的参数（用户点击了特定壁纸的应用按钮）
            if (parameter is WallpaperItem wp) {
                ApplySingleWallpaper(wp);
                return;
            }

            // 如果没有参数但有选中的壁纸，应用第一个选中的壁纸（例如从工具栏触发）
            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    ApplySingleWallpaper(selectedWallpaper);
                }
                return;
            }
        }

        /// <summary>
        /// 应用单个壁纸
        /// </summary>
        private void ApplySingleWallpaper(WallpaperItem wallpaper)
        {
            Log.Information("应用壁纸: {Title}", wallpaper.Project.Title);
            string toolPath = Settings.WallpaperEnginePath;
            string projectJsonPath = Path.Combine(wallpaper.FolderPath, "project.json");

            if (File.Exists(toolPath) && File.Exists(projectJsonPath)) {
                string escapedPath = projectJsonPath.Replace("\"", "\\\"");
                string arguments = $"-control openWallpaper -file \"{escapedPath}\"";
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false
                };
                Process.Start(startInfo)?.Dispose();
            } else {
                Log.Warning("无法应用壁纸: 工具路径或project.json不存在. ToolPath={ToolPath}, ProjectJson={ProjectJson}", toolPath, projectJsonPath);
            }
        }

        /// <summary>
        /// 切换壁纸收藏状态命令，同时同步收藏状态到合集视图
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void ToggleFavorite(object parameter)
        {
            Log.Information("ToggleFavorite命令触发，参数类型: {ParameterType}, SelectedWallpapers.Count: {Count}",
                parameter?.GetType().Name ?? "null", SelectedWallpapers.Count);

            WallpaperItem? parameterWallpaper = null;
            if (parameter is WallpaperItem wp)
            {
                parameterWallpaper = wp;
                Log.Information("参数是WallpaperItem: {Title}, IsFavorite: {IsFavorite}", wp.Project?.Title, wp.IsFavorite);
            }

            if (parameterWallpaper != null)
            {
                bool isParameterInSelected = SelectedWallpapers.Contains(parameterWallpaper);

                if (isParameterInSelected && SelectedWallpapers.Count > 1)
                {
                    foreach (var wallpaperItem in SelectedWallpapers.ToList())
                    {
                        ToggleSingleFavorite(wallpaperItem);
                    }
                }
                else
                {
                    ToggleSingleFavorite(parameterWallpaper);
                    HandleWallpaperSelection(parameterWallpaper, false, false);
                }
                return;
            }

            if (SelectedWallpapers.Count > 0)
            {
                foreach (var wallpaperItem in SelectedWallpapers.ToList())
                {
                    ToggleSingleFavorite(wallpaperItem);
                }
                return;
            }

            Log.Warning("ToggleFavorite: 没有参数也没有选中的壁纸");
        }

        /// <summary>
        /// 切换单个壁纸的收藏状态
        /// </summary>
        private void ToggleSingleFavorite(WallpaperItem wallpaper)
        {
            Log.Information("切换收藏状态: {FolderPath}, 当前状态: {IsFavorite}, 新状态: {NewState}",
                wallpaper.FolderPath, wallpaper.IsFavorite, !wallpaper.IsFavorite);

            wallpaper.IsFavorite = !wallpaper.IsFavorite;
            wallpaper.FavoritedDate = wallpaper.IsFavorite ? DateTime.Now : DateTime.MinValue;

            try {
                _dbManager.ToggleFavorite(wallpaper.Id, wallpaper.IsFavorite);
                Log.Information("数据库更新成功");
            } catch (Exception ex) {
                Log.Warning(ex, "更新收藏状态失败");
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
                return;
            }

            // 同步到 FavoriteViewModel
            var favoriteVm = Ioc.Default.GetService<FavoriteViewModel>();
            if (favoriteVm != null)
            {
                if (wallpaper.IsFavorite)
                {
                    if (!favoriteVm.FavoriteWallpapers.Any(w => w.Id == wallpaper.Id))
                    {
                        favoriteVm.FavoriteWallpapers.Add(wallpaper);
                    }
                }
                else
                {
                    var favoriteItem = favoriteVm.FavoriteWallpapers.FirstOrDefault(w => w.Id == wallpaper.Id);
                    if (favoriteItem != null)
                    {
                        favoriteVm.FavoriteWallpapers.Remove(favoriteItem);
                    }
                }
            }

            // 同步收藏状态到合集视图中的壁纸实例
            var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
            if (collectionVm != null) {
                var collectionWallpaper = collectionVm.CollectionWallpapers
                    .FirstOrDefault(w => w.Id == wallpaper.Id);
                if (collectionWallpaper != null) {
                    collectionWallpaper.IsFavorite = wallpaper.IsFavorite;
                    collectionWallpaper.FavoritedDate = wallpaper.FavoritedDate;
                }
            }
        }

        /// <summary>
        /// 跳转到壁纸所在目录命令，在文件资源管理器中打开
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private async Task GoToWallpaperDirectory(object parameter)
        {
            // 优先使用传入的参数（用户点击了特定壁纸的目录按钮）
            if (parameter is WallpaperItem wp) {
                Log.Debug("打开壁纸目录: {FolderPath}", wp.FolderPath);
                SelectedWallpaper = wp;
                await OpenWallpaperDirectory(wp);
                return;
            }

            // 如果没有参数但有选中的壁纸，打开第一个选中的壁纸目录（例如从工具栏触发）
            if (SelectedWallpapers.Count > 0)
            {
                var selectedWallpaper = SelectedWallpapers.FirstOrDefault();
                if (selectedWallpaper != null)
                {
                    Log.Debug("打开壁纸目录: {FolderPath}", selectedWallpaper.FolderPath);
                    SelectedWallpaper = selectedWallpaper;
                    await OpenWallpaperDirectory(selectedWallpaper);
                }
                return;
            }
        }

        /// <summary>
        /// 打开壁纸所在文件夹
        /// </summary>
        /// <param name="wallpaper">壁纸项</param>
        private async Task OpenWallpaperDirectory(WallpaperItem wallpaper)
        {
            try {
                if (Directory.Exists(wallpaper.FolderPath)) {
                    Process.Start("explorer.exe", wallpaper.FolderPath)?.Dispose();
                } else {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"壁纸目录不存在：{wallpaper.FolderPath}",
                        Title = "错误",
                        ShowCancelButton = false,
                        DialogType = DialogType.Warning
                    });
                }
            } catch (Exception ex) {
                await MaterialDialogService.ShowErrorAsync($"打开目录失败：{ex.Message}", "错误");
            }
        }

        /// <summary>
        /// 选择壁纸命令，更新当前选中状态
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private void SelectWallpaper(object parameter)
        {
            if (parameter is WallpaperItem wallpaper) {
                UpdateSelection(wallpaper);
            }
        }

        /// <summary>
        /// 更新壁纸选中状态，取消其他壁纸的选中并设置新的选中项
        /// </summary>
        /// <param name="selectedWallpaper">要选中的壁纸</param>
        private void UpdateSelection(WallpaperItem selectedWallpaper)
        {
            foreach (var wallpaper in Wallpapers.Where(w => w.IsSelected)) {
                wallpaper.IsSelected = false;
            }

            selectedWallpaper.IsSelected = true;
            SelectedWallpaper = selectedWallpaper;
            _dataContextService.CurrentWallpaper = selectedWallpaper;
        }

        /// <summary>
        /// 打开设置窗口命令
        /// </summary>
        [RelayCommand]
        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = Ioc.Default.GetService<SettingsViewModel>(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var result = settingsWindow.ShowDialog();
            if (result == true) {
                Settings = _settingsService.LoadSettings();
                OnPropertyChanged(nameof(Settings));
            }
        }

        /// <summary>
        /// 将壁纸添加到指定合集命令
        /// </summary>
        /// <param name="parameter">包含壁纸对象和合集ID的参数</param>
        [RelayCommand]
        private async Task AddToSpecificCollection(object parameter)
        {
            Log.Information("=== AddToSpecificCollection called ===");
            Log.Information("  parameter type: {ParameterType}", parameter?.GetType().Name ?? "null");

            // 检查是否为DependencyProperty.UnsetValue
            if (parameter == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Error("AddToSpecificCollection: parameter is DependencyProperty.UnsetValue");
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "参数未设置，请检查绑定",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }

            if (parameter is object[] argsArray)
            {
                Log.Information("  parameter is object[] with length {Length}", argsArray.Length);
                for (int i = 0; i < argsArray.Length; i++)
                {
                    var arg = argsArray[i];
                    string typeName = "null";
                    string stringValue = "null";

                    if (arg != null)
                    {
                        typeName = arg.GetType().Name;
                        if (arg == System.Windows.DependencyProperty.UnsetValue)
                        {
                            typeName = "DependencyProperty.UnsetValue";
                            stringValue = "UnsetValue";
                        }
                        else
                        {
                            stringValue = arg.ToString() ?? "null";
                        }
                    }
                    Log.Information("    args[{Index}] type: {TypeName}, value: {Value}", i, typeName, stringValue);
                }
            }

            if (parameter is not object[] args || args.Length != 2)
            {
                Log.Warning("AddToSpecificCollection: Invalid parameter format, parameter is {TypeName}", parameter?.GetType().Name);
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "参数格式错误",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }
            if (args[1] is not string collectionId)
            {
                Log.Warning("AddToSpecificCollection: args[1] is string: {IsString}", args[1] is string);
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "参数类型错误",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }

            // 确定要添加的壁纸列表：优先使用选中的壁纸列表（支持多选操作），如果没有选中的壁纸则使用参数中的壁纸
            List<WallpaperItem> wallpapersToAdd = new List<WallpaperItem>();
            if (SelectedWallpapers.Count > 0)
            {
                wallpapersToAdd.AddRange(SelectedWallpapers);
                Log.Information("AddToSpecificCollection: Using {Count} selected wallpapers", SelectedWallpapers.Count);
            }
            else if (args[0] is WallpaperItem wp)
            {
                wallpapersToAdd.Add(wp);
                Log.Information("AddToSpecificCollection: Using parameter wallpaper");
            }
            else
            {
                Log.Warning("AddToSpecificCollection: args[0] is WallpaperItem: {IsWallpaper}", args[0] is WallpaperItem);
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "参数类型错误",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
                return;
            }

            if (wallpapersToAdd.Count == 0)
            {
                Log.Warning("AddToSpecificCollection: No wallpapers to add");
                return;
            }

            try {
                var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null)
                {
                    Log.Warning("AddToSpecificCollection: Collection not found with ID {CollectionId}", collectionId);
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = "合集不存在",
                        Title = "错误",
                        ShowCancelButton = false,
                        DialogType = DialogType.Error
                    });
                    return;
                }

                int addedCount = 0;
                int alreadyInCollectionCount = 0;
                foreach (var wallpaper in wallpapersToAdd)
                {
                    if (_dbManager.IsInCollection(collectionId, wallpaper.Id)) {
                        alreadyInCollectionCount++;
                        continue;
                    }

                    _dbManager.AddToCollection(collectionId, wallpaper.Id);
                    addedCount++;
                    collection.WallpaperCount++;
                    Log.Information("成功将壁纸 {Title} 添加到合集 {CollectionName}", wallpaper.Project?.Title, collection.Name);
                }

                // 显示成功提示
                if (addedCount > 0)
                {
                    string message;
                    if (wallpapersToAdd.Count == 1)
                    {
                        message = $"已将壁纸「{wallpapersToAdd[0].Project?.Title}」添加到合集「{collection.Name}」。";
                    }
                    else
                    {
                        message = $"已将 {addedCount} 个壁纸添加到合集「{collection.Name}」。";
                        if (alreadyInCollectionCount > 0)
                        {
                            message += $"\n{alreadyInCollectionCount} 个壁纸已存在于合集中。";
                        }
                    }
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = message,
                        Title = "成功",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                }
                else if (alreadyInCollectionCount > 0)
                {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"所有选中的壁纸已存在于合集「{collection.Name}」中。",
                        Title = "提示",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                }

                // 同步合集页面的壁纸数量
                var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                if (collectionVm != null && addedCount > 0) {
                    var collectionInVm = collectionVm.Collections.FirstOrDefault(c => c.Id == collectionId);
                    if (collectionInVm != null) {
                        collectionInVm.WallpaperCount += addedCount;
                    }

                    // 刷新合集页面的壁纸列表（如果当前正在查看该合集）
                    if (collectionVm.SelectedCollection?.Id == collectionId) {
                        collectionVm.LoadCollectionWallpapers();
                    }
                }

                // 刷新壁纸详情页的合集信息（如果详情页已打开）
                var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                if (detailVm?.CurrentWallpaper != null) {
                    // 检查添加的壁纸是否包含当前详情页显示的壁纸
                    var currentPath = detailVm.CurrentWallpaper.FolderPath;
                    foreach (var wp in wallpapersToAdd) {
                        if (wp.FolderPath == currentPath) {
                            _ = detailVm.LoadWallpaperCollections();
                            break;
                        }
                    }
                }
            } catch (Exception ex) {
                Log.Warning(ex, "添加壁纸到合集失败");
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"添加壁纸到合集失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
            }
        }

        /// <summary>
        /// 将选中的壁纸添加到指定分类，已有分类（非"未分类"）的壁纸将被跳过
        /// </summary>
        /// <param name="parameter">包含壁纸对象和分类ID的参数数组</param>
        [RelayCommand]
        private async Task AddToSpecificCategory(object parameter)
        {
            if (parameter is not object[] args || args.Length != 2)
                return;

            string categoryId;
            if (args[1] is string id)
                categoryId = id;
            else
                categoryId = args[1]?.ToString() ?? "";

            if (string.IsNullOrEmpty(categoryId))
                return;

            // 确定要操作的壁纸列表
            List<WallpaperItem> wallpapersToAdd = new List<WallpaperItem>();
            if (SelectedWallpapers.Count > 0)
            {
                wallpapersToAdd.AddRange(SelectedWallpapers);
            }
            else if (args[0] is WallpaperItem wp)
            {
                wallpapersToAdd.Add(wp);
            }

            if (wallpapersToAdd.Count == 0)
                return;

            var targetCategory = Categories.FirstOrDefault(c => c.Id == categoryId);
            if (targetCategory == null)
                return;

            try
            {
                int addedCount = 0;
                int skippedCount = 0;
                foreach (var wallpaper in wallpapersToAdd)
                {
                    // 已有分类（非"未分类"）的壁纸跳过
                    if (wallpaper.CategoryId != CategoryConstants.UNCATEGORIZED_ID)
                    {
                        skippedCount++;
                        continue;
                    }

                    wallpaper.CategoryId = categoryId;
                    wallpaper.Category = targetCategory.Name;
                    _dbManager.UpdateWallpaper(wallpaper);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    string message;
                    if (wallpapersToAdd.Count == 1)
                    {
                        message = $"已将壁纸「{wallpapersToAdd[0].Project?.Title}」添加到分类「{targetCategory.Name}」。";
                    }
                    else
                    {
                        message = $"已将 {addedCount} 个壁纸添加到分类「{targetCategory.Name}」。";
                        if (skippedCount > 0)
                        {
                            message += $"\n{skippedCount} 个壁纸已有分类，已跳过。";
                        }
                    }
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = message,
                        Title = "成功",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });

                    // 刷新分类统计和视图
                    LoadCustomCategories();
                    WallpapersView.Refresh();

                    // 刷新详情页：先置空再赋值，强制触发 CurrentWallpaperChanged 事件
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm?.CurrentWallpaper != null)
                    {
                        foreach (var wp2 in wallpapersToAdd)
                        {
                            if (wp2.Id == detailVm.CurrentWallpaper.Id)
                            {
                                _dataContextService.CurrentWallpaper = null;
                                _dataContextService.CurrentWallpaper = wp2;
                                break;
                            }
                        }
                    }
                }
                else if (skippedCount > 0)
                {
                    await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                        Message = $"所有选中的壁纸已有分类，无需操作。",
                        Title = "提示",
                        ShowCancelButton = false,
                        DialogType = DialogType.Information
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "添加壁纸到分类失败");
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"添加壁纸到分类失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
            }
        }

        /// <summary>受保护的分类名称集合，不允许重命名或删除</summary>
        private static readonly HashSet<string> ProtectedCategories = new() { "所有分类", "未分类" };

        /// <summary>
        /// 重命名分类命令，同步更新数据库、内存中的壁纸和详情页分类列表
        /// </summary>
        /// <param name="category">要重命名的分类名称</param>
        [RelayCommand]
        private async Task RenameCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || CategoryConstants.IsProtectedCategory(category)) return;

            // 获取分类ID
            var categoryId = _dbManager.GetCategoryIdByName(category);
            if (string.IsNullOrEmpty(categoryId) || CategoryConstants.IsVirtualCategoryId(categoryId))
            {
                // 虚拟分类或不存在
                return;
            }

            var result = await MaterialDialogService.ShowInputAsync("重命名分类", "请输入新的分类名称", "", category);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName) && newName != category) {
                // 检查新名称是否已存在
                if (Categories.Any(c => c.Name == newName)) {
                    await MaterialDialogService.ShowErrorAsync("该分类名称已存在", "错误");
                    return;
                }
                try {
                    // 重命名分类
                    _dbManager.RenameCategory(categoryId, newName);

                    // 更新分类列表中的名称
                    var categoryItem = Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (categoryItem != null)
                    {
                        categoryItem.Name = newName;
                        // 触发UI更新
                        var index = Categories.IndexOf(categoryItem);
                        Categories[index] = categoryItem; // 重新设置以触发通知
                    }

                    // 更新内存中壁纸的分类显示名称
                    foreach (var w in Wallpapers.Where(w => w.CategoryId == categoryId)) {
                        w.Category = newName;
                    }

                    // 如果当前选中的是这个分类，更新选中状态
                    if (SelectedCategoryId == categoryId)
                    {
                        // ID不变，名称会在UI中自动更新
                    }

                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm != null) {
                        // 详情页也需要更新分类名称
                        // 注意：详情页的CategoryList也需要更新
                    }

                    // 重新加载分类列表以确保一致性
                    LoadCustomCategories();
                } catch (Exception ex) {
                    Log.Warning("重命名分类失败: {Error}", ex.Message);
                }
            }
        }

        /// <summary>
        /// 删除分类命令，将该分类下的壁纸重置为"未分类"
        /// </summary>
        /// <param name="category">要删除的分类名称</param>
        [RelayCommand]
        private async Task DeleteCategory(string category)
        {
            if (string.IsNullOrEmpty(category) || CategoryConstants.IsProtectedCategory(category)) return;

            // 获取分类ID
            var categoryId = _dbManager.GetCategoryIdByName(category);
            if (string.IsNullOrEmpty(categoryId) || CategoryConstants.IsVirtualCategoryId(categoryId))
            {
                // 虚拟分类或不存在
                return;
            }

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除分类「{category}」吗？\n该分类下的壁纸将被重置为「未分类」。",
                "删除分类");

            if (confirmed) {
                try {
                    // 删除分类
                    _dbManager.DeleteCategory(categoryId);

                    // 从分类列表中移除
                    var categoryItem = Categories.FirstOrDefault(c => c.Id == categoryId);
                    if (categoryItem != null)
                    {
                        Categories.Remove(categoryItem);
                    }

                    // 更新内存中壁纸的分类ID和显示名称为"未分类"
                    foreach (var w in Wallpapers.Where(w => w.CategoryId == categoryId)) {
                        w.CategoryId = CategoryConstants.UNCATEGORIZED_ID;
                        w.Category = "未分类";
                    }

                    // 如果当前选中的是这个分类，切换到"所有分类" (ID = 0)
                    if (SelectedCategoryId == categoryId) {
                        SelectedCategoryId = CategoryConstants.ALL_CATEGORIES_ID;
                    }

                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm != null) {
                        // 刷新详情页的分类列表
                        detailVm.RefreshCategoryList();
                    }

                    // 重新加载分类列表以确保一致性
                    LoadCustomCategories();
                } catch (Exception ex) {
                    Log.Warning(ex, "删除分类失败");
                }
            }
        }

        /// <summary>
        /// 处理壁纸选择，支持Ctrl和Shift多选
        /// </summary>
        /// <param name="wallpaper">点击的壁纸项</param>
        /// <param name="isCtrlPressed">Ctrl键是否按下</param>
        /// <param name="isShiftPressed">Shift键是否按下</param>
        public void HandleWallpaperSelection(WallpaperItem wallpaper, bool isCtrlPressed, bool isShiftPressed)
        {
            if (wallpaper == null) return;

            // 如果没有修饰键，清空选择并选中当前项
            if (!isCtrlPressed && !isShiftPressed)
            {
                ClearSelection();
                AddToSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = wallpaper;
                return;
            }

            // Ctrl键：切换当前项的选中状态
            if (isCtrlPressed && !isShiftPressed)
            {
                ToggleSelection(wallpaper);
                _lastSelectedItem = wallpaper;
                SelectedWallpaper = SelectedWallpapers.LastOrDefault();
                return;
            }

            // Shift键：选择从上次选中项到当前项之间的所有壁纸
            if (isShiftPressed && !isCtrlPressed)
            {
                if (_lastSelectedItem == null)
                {
                    ClearSelection();
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                // 找到两个壁纸在列表中的索引
                int lastIndex = Wallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = Wallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                ClearSelection();
                for (int i = start; i <= end; i++)
                {
                    AddToSelection(Wallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                return;
            }

            // Ctrl+Shift组合：将当前项添加到选区，但不改变其他项
            if (isCtrlPressed && isShiftPressed)
            {
                // 扩展选区：从_lastSelectedItem到当前项，但保持已有选区
                if (_lastSelectedItem == null)
                {
                    AddToSelection(wallpaper);
                    _lastSelectedItem = wallpaper;
                    SelectedWallpaper = wallpaper;
                    return;
                }

                int lastIndex = Wallpapers.IndexOf(_lastSelectedItem);
                int currentIndex = Wallpapers.IndexOf(wallpaper);
                if (lastIndex == -1 || currentIndex == -1) return;

                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);

                for (int i = start; i <= end; i++)
                {
                    AddToSelection(Wallpapers[i]);
                }
                SelectedWallpaper = wallpaper;
                // 不更新_lastSelectedItem，保持之前的最后选中项
                return;
            }
        }

        /// <summary>
        /// 清空选中项
        /// </summary>
        private void ClearSelection()
        {
            foreach (var item in SelectedWallpapers.ToList())
            {
                item.IsSelected = false;
            }
            SelectedWallpapers.Clear();
        }

        /// <summary>
        /// 添加壁纸到选中项
        /// </summary>
        private void AddToSelection(WallpaperItem wallpaper)
        {
            if (!SelectedWallpapers.Contains(wallpaper))
            {
                SelectedWallpapers.Add(wallpaper);
                wallpaper.IsSelected = true;
            }
        }

        /// <summary>
        /// 从选中项中移除壁纸
        /// </summary>
        private void RemoveFromSelection(WallpaperItem wallpaper)
        {
            if (SelectedWallpapers.Contains(wallpaper))
            {
                SelectedWallpapers.Remove(wallpaper);
                wallpaper.IsSelected = false;
            }
        }

        /// <summary>
        /// 切换壁纸的选中状态
        /// </summary>
        private void ToggleSelection(WallpaperItem wallpaper)
        {
            if (SelectedWallpapers.Contains(wallpaper))
            {
                RemoveFromSelection(wallpaper);
            }
            else
            {
                AddToSelection(wallpaper);
            }
        }

        /// <summary>
        /// 打开分类管理窗口
        /// </summary>
        [RelayCommand]
        private void ManageCategories()
        {
            var categoryWindow = new CategoryManagementWindow
            {
                Owner = System.Windows.Application.Current.MainWindow,
                DataContext = Ioc.Default.GetService<CategoryManagementViewModel>(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            categoryWindow.ShowDialog();

            // 对话框关闭后刷新分类列表
            LoadCustomCategories();
        }
    }
}
