using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using CommunityToolkit.Mvvm.DependencyInjection;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 为合集菜单项生成带标识的Header内容
    /// 输入值：壁纸对象和合集对象
    /// 输出值：UI元素（包含标识图标和合集名称）
    /// </summary>
    public class CollectionMenuHeaderConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Log.Information($"=== CollectionMenuHeaderConverter.Convert called ===");
            if (values == null || values.Length < 2)
            {
                Log.Error($"CollectionMenuHeaderConverter: values is null or too short, length: {values?.Length}");
                return string.Empty;
            }

            // 记录调试信息
            for (int i = 0; i < values.Length; i++)
            {
                var value = values[i];
                string typeName = "null";
                string stringValue = "null";

                if (value != null)
                {
                    typeName = value.GetType().Name;
                    if (value == System.Windows.DependencyProperty.UnsetValue)
                    {
                        typeName = "DependencyProperty.UnsetValue";
                        stringValue = "UnsetValue";
                    }
                    else
                    {
                        stringValue = value.ToString() ?? "null";
                    }
                }
                Log.Information($"  values[{i}] type: {typeName}, value: {stringValue}");
            }

            // 获取壁纸对象和合集对象
            object? wallpaperObj = values[0];
            object? collectionObj = values[1];

            // 处理DependencyProperty.UnsetValue
            if (wallpaperObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning($"CollectionMenuHeaderConverter: wallpaperObj is UnsetValue");
                wallpaperObj = null;
            }

            if (collectionObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning($"CollectionMenuHeaderConverter: collectionObj is UnsetValue");
                collectionObj = null;
            }

            if (collectionObj is not WallpaperCollection collection)
            {
                Log.Warning($"CollectionMenuHeaderConverter: collectionObj is not WallpaperCollection");
                return string.Empty;
            }

            string collectionName = collection.Name ?? string.Empty;

            // 如果壁纸对象无效，只返回合集名称
            if (wallpaperObj is not WallpaperItem wallpaper)
            {
                Log.Warning($"CollectionMenuHeaderConverter: wallpaperObj is not WallpaperItem");
                return CreateHeaderContent(false, collectionName);
            }

            try
            {
                // 通过IoC容器获取DatabaseManager
                var dbManager = Ioc.Default.GetService<DatabaseManager>();
                if (dbManager == null)
                {
                    Log.Error("CollectionMenuHeaderConverter: DatabaseManager is null");
                    return CreateHeaderContent(false, collectionName);
                }

                bool isInCollection = dbManager.IsInCollection(collection.Id, wallpaper.FolderPath);
                Log.Information($"CollectionMenuHeaderConverter: Wallpaper {wallpaper.Project?.Title} in collection {collection.Name}: {isInCollection}");

                return CreateHeaderContent(isInCollection, collectionName);
            }
            catch (Exception ex)
            {
                Log.Error($"CollectionMenuHeaderConverter: Error checking collection: {ex.Message}");
                return CreateHeaderContent(false, collectionName);
            }
        }

        /// <summary>
        /// 创建菜单项Header内容
        /// </summary>
        /// <param name="showCheckmark">是否显示✓标识</param>
        /// <param name="collectionName">合集名称</param>
        /// <returns>Grid包含图标和文本</returns>
        private object CreateHeaderContent(bool showCheckmark, string collectionName)
        {
            // 使用Grid替代StackPanel，便于精确控制对齐
            // 固定三列结构，确保文字起始位置对齐
            var grid = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            // 始终使用三列结构，确保文字对齐
            // 第一列：图标列（固定宽度，显示或隐藏图标）
            // 第二列：间距列（固定6像素）
            // 第三列：文本列（自动宽度）
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(15, GridUnitType.Pixel) }); // 固定宽度图标列
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6, GridUnitType.Pixel) });   // 固定间距列
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });                         // 文本列

            // 添加标识图标（如果显示）
            if (showCheckmark)
            {
                var checkmarkTextBlock = new TextBlock
                {
                    Text = "✓",
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = System.Windows.Media.Brushes.Green,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 1) // 微调垂直位置
                };
                Grid.SetColumn(checkmarkTextBlock, 0);
                grid.Children.Add(checkmarkTextBlock);
            }

            // 添加合集名称（始终在第三列）
            var nameTextBlock = new TextBlock
            {
                Text = collectionName,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                TextAlignment = TextAlignment.Left,
                FontSize = 13,
                LineHeight = 20,
                LineStackingStrategy = System.Windows.LineStackingStrategy.BlockLineHeight
            };
            Grid.SetColumn(nameTextBlock, 2);
            grid.Children.Add(nameTextBlock);

            return grid;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}