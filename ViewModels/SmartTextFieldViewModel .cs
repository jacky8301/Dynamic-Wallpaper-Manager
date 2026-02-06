using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace WallpaperEngine.ViewModels {
    public partial class SmartTextFieldViewModel : ObservableObject {
        [ObservableProperty]
        private string? _content;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string? _label;

        WallpaperDetailViewModel _detail;

        // 原始内容备份（用于取消操作）
        private string? _originalContent;

        /// <summary>
        /// 开始编辑命令
        /// </summary>
        [RelayCommand]
        private void StartEdit()
        {
            _originalContent = Content; // 备份原始内容
            IsEditMode = true;
        }

        /// <summary>
        /// 保存编辑命令
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            // 这里可以添加验证逻辑
            IsEditMode = false;

            // 触发内容更改通知（如果需要）
            OnPropertyChanged(nameof(Content));
        }

        /// <summary>
        /// 取消编辑命令
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            // 恢复原始内容
            Content = _originalContent;
            IsEditMode = false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public SmartTextFieldViewModel(WallpaperDetailViewModel wallpaperDetailViewModel)
        {
            _detail = wallpaperDetailViewModel;
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当内容改变时，可以添加额外的逻辑
            if (e.PropertyName == nameof(Content) && !IsEditMode) {
                // 内容在非编辑模式下被外部更改时的处理
                if (_originalContent != Content) {
                    // 可以在这里添加逻辑，例如验证或通知
                    //_detail.CurrentWallpaper.Project.Title = Content ?? string.Empty;
                    //_detail.SaveEditCommand.Execute(null);
                }
            }
        }
    }
}
