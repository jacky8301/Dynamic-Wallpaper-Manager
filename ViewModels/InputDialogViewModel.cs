using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using WallpaperEngine.Services;

namespace WallpaperEngine.ViewModels {
    public class InputDialogViewModel : ObservableObject {
        private readonly string _dialogHost;
        private string _inputText = string.Empty;

        public string Title { get; }
        public string Message { get; }
        public string Placeholder { get; }

        public string InputText {
            get => _inputText;
            set => SetProperty(ref _inputText, value);
        }

        public ICommand ConfirmCommand { get; }
        public ICommand CancelCommand { get; }

        public InputDialogViewModel(string title, string message, string placeholder = "", string dialogHost = "MainRootDialog")
        {
            Title = title;
            Message = message;
            Placeholder = placeholder;
            _dialogHost = dialogHost;

            ConfirmCommand = new RelayCommand(OnConfirm);
            CancelCommand = new RelayCommand(OnCancel);
        }

        private void OnConfirm()
        {
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = true, Data = InputText?.Trim() });
            }
        }

        private void OnCancel()
        {
            if (DialogHost.IsDialogOpen(_dialogHost)) {
                DialogHost.Close(_dialogHost,
                    new MaterialDialogResult { Confirmed = false });
            }
        }
    }
}
