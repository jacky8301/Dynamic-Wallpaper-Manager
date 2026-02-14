using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using WallpaperEngine.Models;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public partial class SelectCollectionDialogViewModel : ObservableObject {
        private readonly string _dialogHost;

        [ObservableProperty]
        private ObservableCollection<WallpaperCollection> _collections = new();

        [ObservableProperty]
        private WallpaperCollection? _selectedCollection;

        public SelectCollectionDialogViewModel(List<WallpaperCollection> collections, string dialogHost = "MainRootDialog")
        {
            _dialogHost = dialogHost;
            foreach (var c in collections) {
                Collections.Add(c);
            }
        }

        [RelayCommand]
        private void Confirm()
        {
            if (SelectedCollection == null) return;
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = true, Data = SelectedCollection });
            }
        }

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
