using System.Globalization;
using System.Windows;
using System.Windows.Data;
using WallpaperEngine.Models;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 将壁纸对象和合集ID组合成数组，用于传递给AddToSpecificCollection命令
    /// </summary>
    public class AddToCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Log.Information("=== AddToCollectionConverter.Convert called ===");
            if (values == null)
            {
                Log.Error("AddToCollectionConverter: values is null");
                return new object?[2] { null, null };
            }

            Log.Information("  values.Length = {ValuesLength}", values.Length);
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
                Log.Information("  values[{Index}] type: {TypeName}, value: {StringValue}", i, typeName, stringValue);
            }
            Log.Information("  targetType: {TargetTypeName}, parameter: {Parameter}", targetType?.Name ?? "null", parameter?.ToString() ?? "null");

            if (values.Length < 2)
            {
                Log.Error("AddToCollectionConverter: values too short, length: {Length}", values.Length);
                return new object?[2] { null, null };
            }

            // 简化逻辑：values[0]应该是壁纸对象，values[1]应该是合集ID
            // 但我们需要处理可能的类型不匹配

            object? wallpaperObj = values[0];
            object? collectionIdObj = values[1];

            // 处理DependencyProperty.UnsetValue
            if (wallpaperObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning("AddToCollectionConverter: wallpaperObj is UnsetValue");
                wallpaperObj = null;
            }

            if (collectionIdObj == System.Windows.DependencyProperty.UnsetValue)
            {
                Log.Warning("AddToCollectionConverter: collectionIdObj is UnsetValue");
                collectionIdObj = null;
            }

            // 确保collectionIdObj是字符串
            if (collectionIdObj != null && !(collectionIdObj is string))
            {
                Log.Warning("AddToCollectionConverter: collectionIdObj is not string, converting. Type: {TypeName}", collectionIdObj.GetType().Name);
                collectionIdObj = collectionIdObj.ToString();
            }

            Log.Information("AddToCollectionConverter: Returning array - wallpaper type: {WallpaperType}, collectionId: {CollectionId}", wallpaperObj?.GetType().Name ?? "null", collectionIdObj?.ToString() ?? "null");

            // 总是返回长度为2的数组，即使元素可能为null
            return new object?[] { wallpaperObj, collectionIdObj };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
