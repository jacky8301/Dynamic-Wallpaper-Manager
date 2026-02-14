using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    /// <summary>
    /// 合集选择对话框视图模型，用于从合集列表中选择一个合集
    /// </summary>
    public partial class SelectCollectionDialogViewModel : ObservableObject {
        private readonly string _dialogHost;

        /// <summary>可选择的合集列表</summary>
        [ObservableProperty]
        private ObservableCollection<WallpaperCollection> _collections = new();

        /// <summary>当前选中的合集</summary>
        [ObservableProperty]
        private WallpaperCollection? _selectedCollection;

        /// <summary>
        /// 初始化合集选择对话框视图模型
        /// </summary>
        /// <param name="collections">可选择的合集列表</param>
        /// <param name="dialogHost">对话框宿主名称</param>
        public SelectCollectionDialogViewModel(List<WallpaperCollection> collections, string dialogHost = "MainRootDialog")
        {
            _dialogHost = dialogHost;
            foreach (var c in collections) {
                Collections.Add(c);
            }
        }

        /// <summary>
        /// 确认选择命令，关闭对话框并返回选中的合集
        /// </summary>
        [RelayCommand]
        private void Confirm()
        {
            if (SelectedCollection == null) return;
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = true, Data = SelectedCollection });
            }
        }

        /// <summary>
        /// 取消选择命令，关闭对话框
        /// </summary>
        [RelayCommand]
        private void Cancel()
        {
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = false });
            }
        }
    }
}
