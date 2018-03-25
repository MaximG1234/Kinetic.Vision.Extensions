using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Utilities
{
    public static class EuclidUtilities
    {
        public static double ToColorEuclidX(int u, float z, int w)
        {
            return z * 0.901985 * (2.0 * u / w - 1);
        }

        public static double ToColorEuclidY(int v, float z, int h)
        {
            return z * 0.507329 * (1 - 2.0 * v / h);
        }

        public static int ToColorPixelU(float x, float z, int w)
        {
            return (int)((x / (z * 0.901985) + 1) / 2 * w);
        }
        public static int ToColorPixelV(float y, float z, int h)
        {
            return (int)((1 - y / (z * 0.507329)) / 2 * h);
        }

        public static float ToDepthEuclidX(int u, float z, int w)
        {
            return (float)(z * 0.70804 * (2.0 * u / w - 1));
        }
        public static float ToDepthEuclidY(int v, float z, int h)
        {
            return (float)(z * 0.57735 * (1 - 2.0 * v / h));
        }
        public static int ToDepthPixelU(float x, float z, int w)
        {
            return (int)((x / (z * 0.70804) + 1) / 2 * w);
        }
        public static int ToDepthPixelV(float y, float z, int h)
        {
            return (int)((1 - y / (z * 0.57735)) / 2 * h);
        }


    }
}
