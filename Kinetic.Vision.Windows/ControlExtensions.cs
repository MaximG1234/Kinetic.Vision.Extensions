using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;

namespace System.Windows.Controls
{
    public static class ControlExtensions
    {
        public static DependencyObject FindParent(this DependencyObject child)
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            return parentObject;
        }

        public static T FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }


        public static void SetTimeout(this ContentControl control, int timeoutMS, Action action)
        {
            SetTimeout(control.Dispatcher, timeoutMS, action);
        }

        public static void SetTimeout(this Dispatcher dispatcher, int timeoutMS, Action action)
        {

            System.Threading.Timer timer = null;

            timer = new System.Threading.Timer(o =>
            {
                timer.Dispose();

                try
                {
                    dispatcher.Invoke(() =>
                    {
                        action();
                    });
                }
                catch (TaskCanceledException)
                {
                    //This can happen when application is closing.
                }
                catch (Exception)
                {
                    throw;
                }

            }, null, timeoutMS, System.Threading.Timeout.Infinite);

        }

    }
}