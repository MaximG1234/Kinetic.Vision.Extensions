using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face.Image
{
    public static class SharpnessAnalyser
    {
        public static int Analyse(Bitmap image)
        {
            using (Image<Bgr, Byte> img = new Image<Bgr, Byte>(image))
            {
                using (Image<Gray, Byte> greyimg = img.Convert<Gray, Byte>())
                {
                    using (Image<Gray, Byte> cannied = greyimg.Canny(new Gray(200), new Gray(200)))
                    {
                        int[] count = cannied.CountNonzero();

                        return count.First();
                    }
                }
            }
        }


    }
}
