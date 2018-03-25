using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Drawing
{
    public static class DrawingUtilities
    {
        public static ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
        

        public static EncoderParameters GetEncoderParameters(long quality)
        {
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            EncoderParameters encoderParameters = new EncoderParameters(1);
            EncoderParameter myEncoderParameter = new EncoderParameter(myEncoder, quality);
            encoderParameters.Param[0] = myEncoderParameter;

            return encoderParameters;
        }
    }
}
