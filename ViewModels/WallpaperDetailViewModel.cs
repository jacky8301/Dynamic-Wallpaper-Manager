using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class WallpaperDetailViewModel : ObservableObject {
        private readonly DatabaseManager _dbManager;
        private readonly IDataContextService _dataContextService;
        private WallpaperItem _originalItem;
        [ObservableProperty]
        private WallpaperItem? _currentWallpaper;
        [ObservableProperty]
        private bool _isEditMode;
        [ObservableProperty]
        private string _editStatus = "就绪";
        public SmartTextFieldViewModel wallpaperTitleViewModel { get; set; }
        // 新增：类型列表数据源
        public ObservableCollection<string> WallpaperTypes { get; } = new ObservableCollection<string>
        {
            "scene",
            "video",
            "web",
            "application"
        };
        public WallpaperDetailViewModel(IDataContextService dataContextService)
        {
            _dbManager = new DatabaseManager();
            _dataContextService = dataContextService;
            // 订阅状态变化事件
            _dataContextService.CurrentWallpaperChanged += OnCurrentWallpaperChanged;

            wallpaperTitleViewModel = new SmartTextFieldViewModel
            {
                Label = "标题",
                Content = CurrentWallpaper?.Project?.Title,
                IsEditMode = false
            };

            // 初始化命令
            StartEditCommand = new RelayCommand(StartEdit);
            SaveEditCommand = new AsyncRelayCommand(SaveEdit, CanSaveEdit);
            CancelEditCommand = new RelayCommand(CancelEdit);
            
        }

        private void OnCurrentWallpaperChanged(object? sender, WallpaperItem? newWallpaper)
        {
            // 当服务中的状态改变时，更新自己的数据
            CurrentWallpaper = newWallpaper;
            CurrentWallpaper.LoadFileListAsync().ConfigureAwait(false);
            wallpaperTitleViewModel.Content = CurrentWallpaper?.Project?.Title;
        }

        public ICommand StartEditCommand { get; }
        public ICommand SaveEditCommand { get; }
        public ICommand CancelEditCommand { get; }

        public void Initialize(WallpaperItem? wallpaper = null)
        {
            if (wallpaper == null) return;
            CurrentWallpaper = wallpaper;
            _originalItem = CreateBackup(wallpaper);
        }
        [RelayCommand]
        private async Task SetContentFileName(string? fileName)
        {
            if (CurrentWallpaper != null) {
                CurrentWallpaper.Project.File = fileName;
                try {
                    // 保存到project.json
                    await SaveToProjectJsonAsync();
                    // 更新数据库
                    await UpdateDatabaseAsync();
                    // 重新加载壁纸信息
                } catch (Exception ex) {
                    ShowErrorMessage($"保存失败: {ex.Message}");
                }
            }
        }

        private void StartEdit()
        {
            if (CurrentWallpaper == null) return;

            IsEditMode = true;
            EditStatus = "编辑模式";

            // 创建编辑备份
            _originalItem = CreateBackup(CurrentWallpaper);
            CurrentWallpaper.IsEditing = true;
        }

        private async Task SaveEdit()
        {
            if (CurrentWallpaper == null) return;

            try {
                EditStatus = "正在保存...";

                // 保存到project.json
                await SaveToProjectJsonAsync();

                // 更新数据库
                await UpdateDatabaseAsync();

                // 退出编辑模式
                IsEditMode = false;
                CurrentWallpaper.IsEditing = false;
                EditStatus = "保存成功";
                // 显示成功消息
                ShowSaveSuccessMessage();
            } catch (Exception ex) {
                EditStatus = "保存失败";
                ShowErrorMessage($"保存失败: {ex.Message}");
            }
        }

        private bool CanSaveEdit()
        {
            return true;
            return CurrentWallpaper != null &&
                   IsEditMode &&
                   !string.IsNullOrWhiteSpace(CurrentWallpaper.Project.Title);
        }

        private void CancelEdit()
        {
            if (CurrentWallpaper == null) return;

            // 恢复原始数据
            if (_originalItem != null) {
                RestoreFromBackup(CurrentWallpaper, _originalItem);
            }

            IsEditMode = false;
            CurrentWallpaper.IsEditing = false;
            EditStatus = "编辑已取消";
        }

        // 保存到project.json文件
        private async Task SaveToProjectJsonAsync()
        {
            var projectJsonPath = Path.Combine(CurrentWallpaper.FolderPath, "project.json");

            try {
                var jsonSettings = new JsonSerializerSettings {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var jsonContent = JsonConvert.SerializeObject(CurrentWallpaper.Project, jsonSettings);
                await File.WriteAllTextAsync(projectJsonPath, jsonContent, Encoding.UTF8);
            } catch (Exception ex) {
                throw new InvalidOperationException($"无法保存project.json: {ex.Message}", ex);
            }
        }

        // 更新数据库
        private async Task UpdateDatabaseAsync()
        {
            await Task.Run(() => {
                _dbManager.UpdateWallpaper(CurrentWallpaper);
            });
        }

        // 创建数据备份
        private WallpaperItem CreateBackup(WallpaperItem source)
        {
            return new WallpaperItem {
                Id = source.Id,
                FolderPath = source.FolderPath,
                Project = new WallpaperProject {
                    Title = source.Project.Title,
                    Description = source.Project.Description,
                    File = source.Project.File,
                    Preview = source.Project.Preview,
                    Type = source.Project.Type,
                    Tags = new List<string>(source.Project.Tags ?? new List<string>()),
                    ContentRating = source.Project.ContentRating,
                    Visibility = source.Project.Visibility
                },
                IsFavorite = source.IsFavorite
            };
        }

        // 从备份恢复数据
        private void RestoreFromBackup(WallpaperItem target, WallpaperItem backup)
        {
            target.Project.Title = backup.Project.Title;
            target.Project.Description = backup.Project.Description;
            target.Project.File = backup.Project.File;
            target.Project.Preview = backup.Project.Preview;
            target.Project.Type = backup.Project.Type;
            target.Project.Tags = new List<string>(backup.Project.Tags ?? new List<string>());
            target.Project.ContentRating = backup.Project.ContentRating;
            target.Project.Visibility = backup.Project.Visibility;
            target.IsFavorite = backup.IsFavorite;
        }

        private void ShowSaveSuccessMessage()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                System.Windows.MessageBox.Show("壁纸详情已成功保存!", "保存成功",
                    System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            });
        }

        private void ShowErrorMessage(string message)
        {
             System.Windows.Application.Current.Dispatcher.Invoke(() => {
                System.Windows.MessageBox.Show(message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }
}
