using System;

namespace Kinetic.Vision.Kinect.Face.Events.Face
{
    public class FacePhotoBodyLostEventArgs : EventArgs
    {
        public ulong TrackingId { get; set; }

        public int CurrentSubjects { get; set; }
    }
}
