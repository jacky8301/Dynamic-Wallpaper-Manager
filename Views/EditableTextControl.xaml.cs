using System.Windows;
using System.Windows.Input;

namespace WallpaperEngine.Views {
    public partial class EditableTextControl : System.Windows.Controls.UserControl {
        public EditableTextControl()
        {
            InitializeComponent();
        }
        // 定义依赖属性
        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(EditableTextControl), new PropertyMetadata(""));

        // 进入编辑模式
        private void EnterEditMode()
        {
            DisplayTextBlock.Visibility = Visibility.Collapsed;
            EditTextBox.Visibility = Visibility.Visible;
            EditTextBox.Focus(); // 设置焦点
            EditTextBox.SelectAll(); // 选中文本
        }

        // 退出编辑模式
        private void LeaveEditMode()
        {
            DisplayTextBlock.Visibility = Visibility.Visible;
            EditTextBox.Visibility = Visibility.Collapsed;
        }

        // 文本块鼠标按下事件（示例：双击进入编辑）
        private void DisplayTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2) // 双击
            {
                EnterEditMode();
            }
        }

        // 文本框失去焦点事件
        private void EditTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            LeaveEditMode();
        }

        // 文本框按键事件
        private void EditTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                LeaveEditMode(); // 按Enter键保存
            } else if (e.Key == Key.Escape) {
                EditTextBox.Text = Text; // 恢复原值
                LeaveEditMode(); // 按Escape键取消
            }
        }
    }
}

