using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace System.Drawing
{
    public static class DrawingExtensions
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static byte[] ToByteArray(this string base64String)
        {
            Byte[] bitmapData = Convert.FromBase64String(FixBase64ForImage(base64String));
            return bitmapData;
        }

        public static Bitmap ToBitmap(this string base64String)
        {
            Byte[] bitmapData = Convert.FromBase64String(FixBase64ForImage(base64String));
            System.IO.MemoryStream streamBitmap = new System.IO.MemoryStream(bitmapData);
            Bitmap bitImage = new Bitmap((Bitmap)Image.FromStream(streamBitmap));
            return bitImage;
        }

        private static string FixBase64ForImage(string Image)
        {
            System.Text.StringBuilder sbText = new System.Text.StringBuilder(Image, Image.Length);
            sbText.Replace("\r\n", String.Empty); sbText.Replace(" ", String.Empty);
            return sbText.ToString();
        }

        public static byte[] ToBytes(this System.Drawing.Image image)
        {
            return ToBytes(image, ImageFormat.Png);
        }

        public static byte[] ToBytes(this System.Drawing.Image image, System.Drawing.Imaging.ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, format);
                return ms.ToArray();
            }
        }

        public static byte[] ToBytes(this System.Drawing.Image image, ImageCodecInfo codecInfo, EncoderParameters encoderParams)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                image.Save(ms, codecInfo, encoderParams);
                return ms.ToArray();
            }
        }

        public static BitmapSource ToBitmapSource(this byte[] imageBytes)
        {
            return ToImage(imageBytes).ToBitmapSource();
        }

        public static Image ToImage(this byte[] imageBytes)
        {
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                Image returnImage = Image.FromStream(ms);
                return returnImage;
            }
        }

        public static Bitmap ToBitmap(this byte[] imageBytes, int width, int height)
        {
            if (imageBytes != null)
                return ToBitmap(imageBytes, width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            else
                return null;
        }

        public static Bitmap ToBitmap(this byte[] imageBytes, int width, int height, PixelFormat format)
        {
            Bitmap bmap = new Bitmap(width, height, format);

            BitmapData bmapData = bmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, bmap.PixelFormat);

            IntPtr ptr = bmapData.Scan0;
            Marshal.Copy(imageBytes, 0, ptr, imageBytes.Length);

            bmap.UnlockBits(bmapData);

            return bmap;
        }

        public static BitmapImage ToImageSource(this Image source)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                source.Save(stream, ImageFormat.Png);

                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();
                return result;
            }
        }

        public static BitmapSource ToBitmapSource(this Image bitmapImage)
        {
            Bitmap bitmap = bitmapImage as Bitmap;
            if (bitmap != null)
                return ToBitmapSource(bitmap);
            else
                return null;
        }

        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();
            BitmapSource retval;
            
            try
            {

                retval = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(hBitmap,
                                                                                      IntPtr.Zero,
                                                                                      Int32Rect.Empty,
                                                                                      BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return retval;
        }

        public static Bitmap Merge(this Bitmap background, Bitmap foreground)
        {
            Bitmap result = new Bitmap(background.Width, background.Height, PixelFormat.Format32bppArgb);
            using (Graphics canvas = Graphics.FromImage(result))
            {
                canvas.InterpolationMode = InterpolationMode.HighQualityBicubic;
                canvas.DrawImage(background, 0, 0);
                canvas.DrawImage(foreground, new Rectangle(0, 0, foreground.Width, foreground.Height), new Rectangle(0, 0, foreground.Width, foreground.Height), GraphicsUnit.Pixel);
                canvas.Save();
            }

            return result;
        }


        public static System.Drawing.Image ResizeImage(this System.Drawing.Image image, Size size, bool preserveAspectRatio = true)
        {
            int newWidth;
            int newHeight;
            if (preserveAspectRatio)
            {
                int originalWidth = image.Width;
                int originalHeight = image.Height;
                float percentWidth = (float)size.Width / (float)originalWidth;
                float percentHeight = (float)size.Height / (float)originalHeight;
                float percent = percentHeight < percentWidth ? percentHeight : percentWidth;
                newWidth = (int)(originalWidth * percent);
                newHeight = (int)(originalHeight * percent);
            }
            else
            {
                newWidth = size.Width;
                newHeight = size.Height;
            }
            System.Drawing.Image newImage = new Bitmap(newWidth, newHeight);
            using (System.Drawing.Graphics graphicsHandle = System.Drawing.Graphics.FromImage(newImage))
            {
                graphicsHandle.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphicsHandle.DrawImage(image, 0, 0, newWidth, newHeight);
            }
            return newImage;
        }


        /// <summary>
        /// Resizes an image based on the width and height provided. Note: Preserves aspect ratio
        /// </summary>
        /// <param name="imgPhoto">Image that contains original</param>
        /// <param name="width">New Width</param>
        /// <param name="height">New Height</param>
        /// <returns>Image resized to new height and width</returns>
        public static System.Drawing.Image ResizeImage(this System.Drawing.Image imgPhoto, int width, int height)
        {
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)width / (float)sourceWidth);
            nPercentH = ((float)height / (float)sourceHeight);
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = System.Convert.ToInt16((width - (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = System.Convert.ToInt16((height - (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            Bitmap bmPhoto = new Bitmap(width, height,
                              PixelFormat.Format24bppRgb);

            bmPhoto.SetResolution(imgPhoto.HorizontalResolution, imgPhoto.VerticalResolution);

            using (Graphics grPhoto = Graphics.FromImage(bmPhoto))
            {
                grPhoto.Clear(Color.White);
                grPhoto.InterpolationMode = InterpolationMode.HighQualityBicubic;

                grPhoto.DrawImage(imgPhoto,
                    new Rectangle(destX, destY, destWidth, destHeight),
                    new Rectangle(sourceX, sourceY, sourceWidth, sourceHeight),
                    GraphicsUnit.Pixel);
            }
            return bmPhoto;
        }

    }

}
