using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;

namespace WallpaperEngine.Views {
    public partial class MyToolBar : System.Windows.Controls.UserControl {
        public MyToolBar()
        {
            InitializeComponent();
        }

        // 保留原有的KeyDown事件处理
        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter) {
                // 处理搜索逻辑
                var textBox = sender as TextBox;
                if (textBox != null) {
                    // 触发搜索命令或直接处理搜索
                    if (DataContext is IScanToolBarViewModel viewModel) {
                        viewModel.PerformSearch();
                    }
                }
            }
        }


        // 定义接口，便于单元测试和依赖注入
        public interface IScanToolBarViewModel {
            void PerformSearch();
            // 其他必要的接口成员
        }
    }
}
