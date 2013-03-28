using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace RenRenWin8Client.Helper
{
    public static class ExtensionMethods
    {
        static Regex regex = new Regex(@"([\u0e01-\u0e5f]{2,})", RegexOptions.Multiline);
        public static string DeleteDuplicatedThaiChars(this string self)
        {
            if (regex.IsMatch(self))
            {
                while (regex.IsMatch(self))
                {
                    var match = regex.Match(self);
                    self = self.Remove(match.Index, match.Length - 1);
                }
            }
            return self;
        }

        public static FrameworkElement FindVisualChild(this FrameworkElement root, string name)
        {
            if (root != null)
            {
                FrameworkElement temp = root.FindName(name) as FrameworkElement;
                if (temp != null)
                    return temp;

                foreach (FrameworkElement element in root.GetVisualDescendents())
                {
                    temp = element.FindName(name) as FrameworkElement;
                    if (temp != null)
                        return temp;
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        public static IEnumerable<FrameworkElement> GetVisualDescendents(this FrameworkElement root)
        {
            Queue<IEnumerable<FrameworkElement>> toDo = new Queue<IEnumerable<FrameworkElement>>();

            toDo.Enqueue(root.GetVisualChildren());
            while (toDo.Count > 0)
            {
                IEnumerable<FrameworkElement> children = toDo.Dequeue();
                foreach (FrameworkElement child in children)
                {
                    yield return child;
                    toDo.Enqueue(child.GetVisualChildren());
                }
            }
        }

        public static IEnumerable<FrameworkElement> GetVisualChildren(this FrameworkElement root)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
                yield return VisualTreeHelper.GetChild(root, i) as FrameworkElement;
        }

        /// <summary>
        /// Finding the ScrollViewer or ScrollBar (All Con)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <returns></returns>
        public static T FindChildOfType<T>(DependencyObject root) where T : class
        {
            var queue = new Queue<DependencyObject>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                DependencyObject current = queue.Dequeue();
                for (int i = VisualTreeHelper.GetChildrenCount(current) - 1; 0 <= i; i--)
                {
                    var child = VisualTreeHelper.GetChild(current, i);
                    var typedChild = child as T;
                    if (typedChild != null)
                    {
                        return typedChild;
                    }
                    queue.Enqueue(child);
                }
            }
            return null;
        }

        public static T FindFirstElementInVisualTree<T>(DependencyObject parentElement) where T : DependencyObject
        {
            try
            {
                var count = VisualTreeHelper.GetChildrenCount(parentElement);
                if (count == 0)
                    return null;

                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(parentElement, i);

                    if (child != null && child is T)
                    {
                        return (T)child;
                    }
                    else
                    {
                        var result = FindFirstElementInVisualTree<T>(child);
                        if (result != null)
                            return result;
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
