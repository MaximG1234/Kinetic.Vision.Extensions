using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace System.Windows
{
    public static class WindowExtensions
    {
        public static bool IsInfinityOrEmpty(this System.Windows.Point point)
        {
            return point.IsInfinity() || (point.X == 0 && point.Y == 0);
        }

        public static bool IsInfinity(this System.Windows.Point point)
        {
            return double.IsInfinity(point.X) && double.IsInfinity(point.Y);
        }

        public static bool ContainsInfinity(this System.Windows.Point point)
        {
            return double.IsInfinity(point.X) || double.IsInfinity(point.Y);
        }

        public static string GetLocalPath(this Window window)
        {
            return Kinetic.Vision.Utilities.PathUtilities.GetLocalPath();
        }

        public static void MoveToSecondaryDisplay(this Window window)
        {
            if (window.WindowStartupLocation != WindowStartupLocation.Manual)
                window.WindowStartupLocation = WindowStartupLocation.Manual;

            if (Screen.AllScreens.Length > 0)
            {
                Screen screen = Screen.AllScreens[1];

                System.Drawing.Rectangle screenPosition = screen.WorkingArea;
                window.Top = screenPosition.Top;
                window.Left = screenPosition.Left;
            }


        }
    }
}
