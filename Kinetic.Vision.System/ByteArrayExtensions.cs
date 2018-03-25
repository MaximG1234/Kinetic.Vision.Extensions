using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class ByteArrayExtensions
    {
        public static byte[] ExtractPixelsFromImage(this byte[] pixels, int top, int left, int width, int height, int horizontalResolution, int bytesPerPixel)
        {
            if (top > 0 && left > 0 && width > 0 && height > 0)
            {
                byte[] result = new byte[width * height * bytesPerPixel];

                int counter = 0;
                for (int y = top; y < top + height; y++)
                {
                    for (int x = left; x < left + width; x++)
                    {
                        int colorPixelIndex = ((y * horizontalResolution) + x) * bytesPerPixel;
                        result[counter++] = pixels[colorPixelIndex];
                        result[counter++] = pixels[colorPixelIndex + 1];
                        result[counter++] = pixels[colorPixelIndex + 2];
                        result[counter++] = pixels[colorPixelIndex + 3];
                    }
                }

                return result;
            }
            else
            {
                return null;
            }
        }
    }
}
