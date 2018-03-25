using Kinetic.Vision.Kinect.Face.Bodies;
using Microsoft.Kinect;

namespace Kinetic.Vision.Kinect.Face.Events.Face
{
    public class FaceFrameEventArgs
    {
        public ulong TrackingId { get; set; }
        public byte[] Face { get; set; }
        public RectF ColorBoundingBox { get; set; }
        public TrackedBody TrackedBody { get; set; }
    }
}
