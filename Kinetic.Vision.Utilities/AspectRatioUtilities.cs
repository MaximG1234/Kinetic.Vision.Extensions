using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Utilities
{
    public static class AspectRatioUtilities
    {
        public static double SolveAspect(double? newWidth, double? newHeight, double originalWidth, double originalHeight)
        {
            return SolveAspect(newWidth, newHeight, originalWidth, originalHeight, false);
        }

        public static double SolveAspect(double? newWidth, double? newHeight, double originalWidth, double originalHeight, bool round)
        {
            if (newWidth != null)
            {
                return round ? Math.Round(newWidth.Value / (originalWidth / originalHeight)) : newWidth.Value / (originalWidth / originalHeight);
            }
            else if (newHeight != null)
            {
                return round ? Math.Round(newHeight.Value * (originalWidth / originalHeight)) : newHeight.Value * (originalWidth / originalHeight);
            }
            else
            {
                throw new ArgumentNullException("Both NewWidth and NewHeight cannot be null.");
            }
        }
    }
}
