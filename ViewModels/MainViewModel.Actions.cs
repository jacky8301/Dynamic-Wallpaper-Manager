using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
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
                _previewService.PreviewWallpaper(wallpaper);
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
                string arguments = $"-control openWallpaper -file \"{projectJsonPath}\"";
                ProcessStartInfo startInfo = new ProcessStartInfo {
                    FileName = toolPath,
                    Arguments = arguments,
                    UseShellExecute = false
                };
                Process.Start(startInfo)?.Dispose();
            } else {
                // 处理错误
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

            // 调试参数内容
            WallpaperItem? parameterWallpaper = null;
            if (parameter is WallpaperItem wp)
            {
                parameterWallpaper = wp;
                Log.Information("参数是WallpaperItem: {Title}, IsFavorite: {IsFavorite}", wp.Project?.Title, wp.IsFavorite);
            }
            else if (parameter != null)
            {
                Log.Information("参数不是WallpaperItem，而是: {Type}", parameter.GetType().FullName);
            }

            // 如果参数是壁纸项
            if (parameterWallpaper != null)
            {
                // 检查参数壁纸是否在选中列表中，且选中数量大于1
                // 这种情况通常是右键菜单：用户选中了多个壁纸，然后右键点击其中一个打开菜单
                bool isParameterInSelected = SelectedWallpapers.Contains(parameterWallpaper);

                if (isParameterInSelected && SelectedWallpapers.Count > 1)
                {
                    // 收藏菜单场景：参数壁纸在选中列表中，且选中多个壁纸
                    // 操作所有选中壁纸（优化选中项）
                    bool anyUnfavorited = false;
                    foreach (var wallpaperItem in SelectedWallpapers.ToList())
                    {
                        bool wasFavorite = wallpaperItem.IsFavorite;
                        ToggleSingleFavorite(wallpaperItem);
                        if (!wallpaperItem.IsFavorite) // 切换后不再是收藏状态
                        {
                            anyUnfavorited = true;
                        }
                    }
                    // 如果当前仅显示收藏，且有壁纸被取消收藏，刷新视图
                    if (ShowFavoritesOnly && anyUnfavorited)
                    {
                        WallpapersView.Refresh();
                    }
                }
                else
                {
                    // 收藏按钮场景：参数壁纸不在选中列表中，或只有一个选中项
                    // 只操作参数壁纸，并选中它（优化参数传入）
                    ToggleSingleFavorite(parameterWallpaper);
                    // 点击收藏按钮时，使点击的壁纸变成选中壁纸（清空其他选中项）
                    HandleWallpaperSelection(parameterWallpaper, false, false);
                    // 如果当前仅显示收藏，且取消了收藏，刷新视图
                    if (ShowFavoritesOnly && !parameterWallpaper.IsFavorite) {
                        WallpapersView.Refresh();
                    }
                }
                return;
            }

            // 如果没有参数但有选中的壁纸（例如从工具栏触发），使用选中的壁纸列表
            if (SelectedWallpapers.Count > 0)
            {
                bool anyUnfavorited = false;
                foreach (var wallpaperItem in SelectedWallpapers.ToList())
                {
                    bool wasFavorite = wallpaperItem.IsFavorite;
                    ToggleSingleFavorite(wallpaperItem);
                    if (!wallpaperItem.IsFavorite) // 切换后不再是收藏状态
                    {
                        anyUnfavorited = true;
                    }
                }
                // 如果当前仅显示收藏，且有壁纸被取消收藏，刷新视图
                if (ShowFavoritesOnly && anyUnfavorited)
                {
                    WallpapersView.Refresh();
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
                _dbManager.ToggleFavorite(wallpaper.FolderPath, wallpaper.IsFavorite);
                Log.Information("数据库更新成功");
            } catch (Exception ex) {
                Log.Warning($"更新收藏状态失败: {ex.Message}");
                wallpaper.IsFavorite = !wallpaper.IsFavorite;
            }

            // 同步收藏状态到合集视图中的壁纸实例
            var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
            if (collectionVm != null) {
                var collectionWallpaper = collectionVm.CollectionWallpapers
                    .FirstOrDefault(w => w.FolderPath == wallpaper.FolderPath);
                if (collectionWallpaper != null) {
                    collectionWallpaper.IsFavorite = wallpaper.IsFavorite;
                    collectionWallpaper.FavoritedDate = wallpaper.FavoritedDate;
                    Log.Information("已同步收藏状态到合集视图");
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
            Log.Information($"=== AddToSpecificCollection called ===");
            Log.Information($"  parameter type: {parameter?.GetType().Name ?? "null"}");

            // 检查是否为DependencyProperty.UnsetValue
            if (parameter == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Error($"AddToSpecificCollection: parameter is DependencyProperty.UnsetValue");
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
                Log.Information($"  parameter is object[] with length {argsArray.Length}");
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
                    Log.Information($"    args[{i}] type: {typeName}, value: {stringValue}");
                }
            }

            if (parameter is not object[] args || args.Length != 2)
            {
                Log.Warning($"AddToSpecificCollection: Invalid parameter format, parameter is {parameter?.GetType().Name}");
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
                Log.Warning($"AddToSpecificCollection: args[1] is string: {args[1] is string}");
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
                Log.Information($"AddToSpecificCollection: Using {SelectedWallpapers.Count} selected wallpapers");
            }
            else if (args[0] is WallpaperItem wp)
            {
                wallpapersToAdd.Add(wp);
                Log.Information($"AddToSpecificCollection: Using parameter wallpaper");
            }
            else
            {
                Log.Warning($"AddToSpecificCollection: args[0] is WallpaperItem: {args[0] is WallpaperItem}");
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
                Log.Warning($"AddToSpecificCollection: No wallpapers to add");
                return;
            }

            try {
                var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
                if (collection == null)
                {
                    Log.Warning($"AddToSpecificCollection: Collection not found with ID {collectionId}");
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
                    if (_dbManager.IsInCollection(collectionId, wallpaper.FolderPath)) {
                        alreadyInCollectionCount++;
                        continue;
                    }

                    _dbManager.AddToCollection(collectionId, wallpaper.FolderPath);
                    addedCount++;
                    Log.Information($"成功将壁纸{wallpaper.Project?.Title} 添加到合集{collection.Name}");
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

                // 刷新合集页面（如果当前正在查看该合集）
                var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                if (collectionVm?.SelectedCollection?.Id == collectionId) {
                    collectionVm.LoadCollectionWallpapers();
                }
            } catch (Exception ex) {
                Log.Warning($"添加壁纸到合集失败: {ex.Message}");
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = $"添加壁纸到合集失败: {ex.Message}",
                    Title = "错误",
                    ShowCancelButton = false,
                    DialogType = DialogType.Error
                });
            }
        }

        /// <summary>
        /// 将壁纸添加到合集命令，弹出合集选择对话框（保留原功能，以备后用）
        /// </summary>
        /// <param name="parameter">壁纸对象</param>
        [RelayCommand]
        private async Task AddToCollection(object parameter)
        {
            if (parameter is not WallpaperItem wallpaper) return;

            var collections = _dbManager.GetAllCollections();
            if (collections.Count == 0) {
                await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                    Message = "还没有合集，请先在「壁纸合集」页面创建一个合集。",
                    Title = "提示",
                    ShowCancelButton = false,
                    DialogType = DialogType.Information
                });
                return;
            }

            var view = new SelectCollectionDialog();
            view.DataContext = new SelectCollectionDialogViewModel(collections);

            var result = await DialogHost.Show(view, "MainRootDialog");
            if (result is MaterialDialogResult dialogResult && dialogResult.Confirmed && dialogResult.Data is WallpaperCollection selected) {
                try {
                    if (_dbManager.IsInCollection(selected.Id, wallpaper.FolderPath)) {
                        await MaterialDialogService.ShowDialogAsync(new MaterialDialogParams {
                            Message = $"该壁纸已存在于合集「{selected.Name}」中。",
                            Title = "提示",
                            ShowCancelButton = false,
                            DialogType = DialogType.Information
                        });
                        return;
                    }
                    _dbManager.AddToCollection(selected.Id, wallpaper.FolderPath);
                    // 刷新合集页面（如果当前正在查看该合集）
                    var collectionVm = Ioc.Default.GetService<CollectionViewModel>();
                    if (collectionVm?.SelectedCollection?.Id == selected.Id) {
                        collectionVm.LoadCollectionWallpapers();
                    }
                } catch (Exception ex) {
                    Log.Warning($"添加壁纸到合集失败: {ex.Message}");
                }
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
            if (string.IsNullOrEmpty(category) || ProtectedCategories.Contains(category)) return;

            var result = await MaterialDialogService.ShowInputAsync("重命名分类", "请输入新的分类名称", category);
            if (result.Confirmed && result.Data is string newName && !string.IsNullOrWhiteSpace(newName) && newName != category) {
                if (Categories.Contains(newName)) {
                    await MaterialDialogService.ShowErrorAsync("该分类名称已存在", "错误");
                    return;
                }
                try {
                    _dbManager.RenameCategory(category, newName);
                    var index = Categories.IndexOf(category);
                    Categories[index] = newName;
                    // 更新内存中壁纸的分类
                    foreach (var w in Wallpapers.Where(w => w.Category == category)) {
                        w.Category = newName;
                    }
                    if (SelectedCategory == category) {
                        SelectedCategory = newName;
                    }
                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    if (detailVm != null) {
                        var detailIndex = detailVm.CategoryList.IndexOf(category);
                        if (detailIndex >= 0) {
                            detailVm.CategoryList[detailIndex] = newName;
                        }
                    }
                } catch (Exception ex) {
                    Log.Warning($"重命名分类失败: {ex.Message}");
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
            if (string.IsNullOrEmpty(category) || ProtectedCategories.Contains(category)) return;

            var confirmed = await MaterialDialogService.ShowConfirmationAsync(
                $"确定要删除分类「{category}」吗？\n该分类下的壁纸将被重置为「未分类」。",
                "删除分类");

            if (confirmed) {
                try {
                    _dbManager.DeleteCategory(category);
                    Categories.Remove(category);
                    // 更新内存中壁纸的分类
                    foreach (var w in Wallpapers.Where(w => w.Category == category)) {
                        w.Category = "未分类";
                    }
                    if (SelectedCategory == category) {
                        SelectedCategory = "所有分类";
                    }
                    // 同步详情页分类列表
                    var detailVm = Ioc.Default.GetService<WallpaperDetailViewModel>();
                    detailVm?.CategoryList.Remove(category);
                } catch (Exception ex) {
                    Log.Warning($"删除分类失败: {ex.Message}");
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
    }
}
