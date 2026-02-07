using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using WallpaperEngine.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WallpaperEngine.ViewModels {
    public class ConfirmationDialogViewModel : ObservableObject {
        private readonly MaterialDialogParams _parameters;

        public string Title => _parameters.Title;
        public string Message => _parameters.Message;
        public string ConfirmButtonText => _parameters.ConfirmButtonText;
        public string CancelButtonText => _parameters.CancelButtonText;
        public bool ShowCancelButton => _parameters.ShowCancelButton;

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public ConfirmationDialogViewModel(MaterialDialogParams parameters)
        {
            _parameters = parameters;

            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnConfirm()
        {
            if (DialogHost.IsDialogOpen(_parameters.DialogHost)) {
                DialogHost.Close(_parameters.DialogHost,
                    new MaterialDialogResult { Confirmed = true });
            }
        }

        private void OnCancel()
        {
            if (DialogHost.IsDialogOpen(_parameters.DialogHost)) {
                DialogHost.Close(_parameters.DialogHost,
                    new MaterialDialogResult { Confirmed = false });
            }
        }
    }
}
