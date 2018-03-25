using System;

namespace Kinetic.Vision.Kinect.Face.Events.Face
{
    public class PhotoCollectedEventArgs : EventArgs
    {
        public PhotoCollectedEventArgs(FacePhoto facePhoto)
        {
            FacePhoto = facePhoto;
        }

        public FacePhoto FacePhoto { get; private set; }
    }
}
