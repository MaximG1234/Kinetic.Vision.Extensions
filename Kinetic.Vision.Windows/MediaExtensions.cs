using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace System.Windows.Media.Imaging
{
    public static class MediaExtensions
    {
        public static Bitmap ToBitmap(this BitmapSource source)
        {
            Bitmap bmp = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp.UnlockBits(data);
            return bmp;
        }

    }

    public static class DrawingContextExtensions
    {
        private const double DefaultFontSize = 32;
        private readonly static Typeface DefaultFont = new Typeface("Calibri");

        public static void DrawText(this DrawingContext context, string text, Brush brush, Point origin)
        {
            DrawText(context, text, DefaultFont, DefaultFontSize, brush, origin);
        }

        public static void DrawText(this DrawingContext context, string text, double size, Brush brush, Point origin)
        {
            DrawText(context, text, DefaultFont, size, brush, origin);
        }

        public static void DrawText(this DrawingContext context, string text, Typeface typeFace, double size, Brush brush, Point origin)
        {
            FormattedText formattedText = new FormattedText(text, CultureInfo.CurrentCulture,
                                                                  System.Windows.FlowDirection.LeftToRight,
                                                                  typeFace, size, brush);

            context.DrawText(formattedText, origin);
        }


    }
}
