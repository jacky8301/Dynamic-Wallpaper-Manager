using CommunityToolkit.Mvvm.DependencyInjection;
using System.Windows;
using WallpaperEngine.ViewModels;

namespace WallpaperEngine.Views
{
    /// <summary>
    /// 分类管理窗口，提供分类的增加、删除、重命名功能
    /// </summary>
    public partial class CategoryManagementWindow : Window
    {
        public CategoryManagementWindow()
        {
            InitializeComponent();

            // 设置数据上下文
            DataContext = Ioc.Default.GetService<CategoryManagementViewModel>();

            // 窗口加载完成后开始加载分类数据
            Loaded += async (s, e) =>
            {
                var vm = DataContext as CategoryManagementViewModel;
                if (vm != null)
                {
                    await vm.LoadCategoriesCommand.ExecuteAsync(null);
                }
            };
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}