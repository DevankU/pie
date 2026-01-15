using System.Windows;
using System.Windows.Media;

namespace Pie.Helpers
{
    public static class VisualHelper
    {
        public static T? FindVisualParent<T>(this DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }
    }
}