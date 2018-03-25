using System.Drawing;

namespace Kinetic.Vision.Kinect.Face
{
    public class FacePhoto
    {
        public ulong TrackingId { get; set; }
        public Bitmap Image { get; set; }
        public int Distance { get; set; }

    }
}
