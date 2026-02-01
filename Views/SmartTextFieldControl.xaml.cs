using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using WallpaperEngine.ViewModels;


namespace WallpaperEngine.Views {

    public partial class SmartTextFieldControl : System.Windows.Controls.UserControl {
        public SmartTextFieldControl()
        {
            InitializeComponent();
            //DataContext = new SmartTextFieldViewModel();
        }
        //#region 依赖属性

        //public static readonly DependencyProperty LabelProperty =
        //    DependencyProperty.Register(nameof(Label), typeof(string), typeof(SmartTextFieldControl),
        //        new PropertyMetadata(string.Empty, OnLabelPropertyChanged));

        //public static readonly DependencyProperty TextProperty =
        //    DependencyProperty.Register(nameof(Text), typeof(string), typeof(SmartTextFieldControl),
        //        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextPropertyChanged));

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(SmartTextFieldControl),
                new PropertyMetadata(false, OnIsEditModePropertyChanged));

        //public string Label {
        //    get => (string)GetValue(LabelProperty);
        //    set => SetValue(LabelProperty, value);
        //}

        //public string Text {
        //    get => (string)GetValue(TextProperty);
        //    set => SetValue(TextProperty, value);
        //}

        public bool IsEditMode {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        //#endregion

        //#region 属性变更处理

        //private static void OnLabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is SmartTextFieldControl control && control.DataContext is SmartTextFieldViewModel viewModel) {
        //        viewModel.Label = e.NewValue as string;
        //    }
        //}

        //private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        //{
        //    if (d is SmartTextFieldControl control && control.DataContext is SmartTextFieldViewModel viewModel) {
        //        viewModel.Content = e.NewValue as string;
        //    }
        //}

        private static void OnIsEditModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartTextFieldControl control && control.DataContext is SmartTextFieldViewModel viewModel) {
                viewModel.IsEditMode = (bool)e.NewValue;
            }
        }

        //#endregion

        //#region 事件处理

        //protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        //{
        //    base.OnPropertyChanged(e);

        //    // 同步ViewModel的变化到依赖属性
        //    if (DataContext is SmartTextFieldViewModel viewModel) {
        //        if (e.Property == TextProperty && viewModel.Content != Text) {
        //            viewModel.Content = Text;
        //        } else if (e.Property == IsEditModeProperty && viewModel.IsEditMode != IsEditMode) {
        //            viewModel.IsEditMode = IsEditMode;
        //        }
        //    }
        //}

        //#endregion
    }
}

