using System;
using System.Globalization;
using System.Windows.Data;
using CommunityToolkit.Mvvm.DependencyInjection;
using WallpaperEngine.Data;
using WallpaperEngine.Models;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 检查壁纸是否已在合集中的多值转换器
    /// 输入值：壁纸对象和合集ID
    /// 输出值：布尔值（true表示壁纸已在合集中）
    /// </summary>
    public class IsInCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Log.Information($"=== IsInCollectionConverter.Convert called ===");
            if (values == null || values.Length < 2)
            {
                Log.Error($"IsInCollectionConverter: values is null or too short, length: {values?.Length}");
                return false;
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

            // 获取壁纸对象和合集ID
            object? wallpaperObj = values[0];
            object? collectionIdObj = values[1];

            // 处理DependencyProperty.UnsetValue
            if (wallpaperObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning($"IsInCollectionConverter: wallpaperObj is UnsetValue");
                wallpaperObj = null;
            }

            if (collectionIdObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning($"IsInCollectionConverter: collectionIdObj is UnsetValue");
                collectionIdObj = null;
            }

            if (wallpaperObj is not WallpaperItem wallpaper || collectionIdObj is not string collectionId)
            {
                Log.Warning($"IsInCollectionConverter: Invalid parameter types. WallpaperItem: {wallpaperObj is WallpaperItem}, string: {collectionIdObj is string}");
                return false;
            }

            try
            {
                // 通过IoC容器获取DatabaseManager
                var dbManager = Ioc.Default.GetService<DatabaseManager>();
                if (dbManager == null)
                {
                    Log.Error("IsInCollectionConverter: DatabaseManager is null");
                    return false;
                }

                bool isInCollection = dbManager.IsInCollection(collectionId, wallpaper.FolderPath);
                Log.Information($"IsInCollectionConverter: Wallpaper {wallpaper.Project?.Title} in collection {collectionId}: {isInCollection}");
                return isInCollection;
            }
            catch (Exception ex)
            {
                Log.Error($"IsInCollectionConverter: Error checking collection: {ex.Message}");
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}