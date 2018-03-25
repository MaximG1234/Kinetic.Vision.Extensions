using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face
{
    public class FaceCapture : IDisposable
    {
        public FaceCapture(byte[] faceBytes, int width, int height)
        {
            this.Image = faceBytes.ToBitmap(width, height);
        }

        public Bitmap Image { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public int Top { get; set; }
        public int Left { get; set; }

        public int Quality { get; set; }
        public int Distance { get; set; }
        public float Pitch { get; set; }
        public float Yaw { get; set; }
        public float Roll { get; set; }

        public void Dispose()
        {
            if (this.Image != null)
            {
                this.Image.Dispose();
                this.Image = null;
            }
                
        }
    }
}
