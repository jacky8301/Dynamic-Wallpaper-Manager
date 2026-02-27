using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Serilog;

namespace WallpaperEngine.Converters
{
    /// <summary>
    /// 辅助类，用于在MenuItem的ItemContainerStyle中设置复杂的CommandParameter
    /// </summary>
    public static class MenuItemCommandParameterHelper
    {
        public static readonly DependencyProperty CommandParameterProperty =
            DependencyProperty.RegisterAttached(
                "CommandParameter",
                typeof(MultiBinding),
                typeof(MenuItemCommandParameterHelper),
                new PropertyMetadata(null, OnCommandParameterChanged));

        public static MultiBinding GetCommandParameter(MenuItem menuItem)
        {
            return (MultiBinding)menuItem.GetValue(CommandParameterProperty);
        }

        public static void SetCommandParameter(MenuItem menuItem, MultiBinding value)
        {
            menuItem.SetValue(CommandParameterProperty, value);
        }

        private static void OnCommandParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Log.Information($"=== MenuItemCommandParameterHelper.OnCommandParameterChanged called ===");
            Log.Information($"  d type: {d?.GetType().Name ?? "null"}");
            Log.Information($"  e.OldValue type: {e.OldValue?.GetType().Name ?? "null"}, e.NewValue type: {e.NewValue?.GetType().Name ?? "null"}");
            if (d is MenuItem menuItem)
            {
                Log.Information($"  MenuItem Header: {menuItem.Header}");
                if (e.NewValue is MultiBinding binding)
                {
                    Log.Information($"MenuItemCommandParameterHelper: Setting binding for MenuItem");
                    Log.Information($"  Binding has {binding.Bindings.Count} bindings");
                    for (int i = 0; i < binding.Bindings.Count; i++)
                    {
                        Log.Information($"    Binding[{i}]: {binding.Bindings[i].ToString()}");
                    }
                    menuItem.SetBinding(MenuItem.CommandParameterProperty, binding);
                    Log.Information($"MenuItemCommandParameterHelper: Binding set for MenuItem");
                }
                else if (e.NewValue == null)
                {
                    // Clear binding if set to null
                    Log.Information($"MenuItemCommandParameterHelper: Clearing binding");
                    menuItem.ClearValue(MenuItem.CommandParameterProperty);
                }
                else
                {
                    Log.Warning($"MenuItemCommandParameterHelper: e.NewValue is not MultiBinding, type: {e.NewValue.GetType().Name}");
                }
                // Ensure binding is applied even if menu item loads later
                menuItem.Loaded += MenuItem_Loaded;
            }
            else
            {
                Log.Warning($"MenuItemCommandParameterHelper: d is not MenuItem, type: {d?.GetType().Name}");
            }
        }

        private static void MenuItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                Log.Information($"=== MenuItemCommandParameterHelper.MenuItem_Loaded ===");
                Log.Information($"  MenuItem Header: {menuItem.Header}");
                var binding = GetCommandParameter(menuItem);
                if (binding != null)
                {
                    Log.Information($"MenuItemCommandParameterHelper: Re-applying binding on Loaded");
                    menuItem.SetBinding(MenuItem.CommandParameterProperty, binding);
                }
                else
                {
                    Log.Warning($"MenuItemCommandParameterHelper: No binding found for MenuItem");
                }
            }
        }
    }
}
