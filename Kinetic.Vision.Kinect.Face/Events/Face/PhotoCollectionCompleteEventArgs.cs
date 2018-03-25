using System;
using System.Collections.ObjectModel;

namespace Kinetic.Vision.Kinect.Face.Events.Face
{
    public class PhotoCollectionCompleteEventArgs : EventArgs
    {
        public PhotoCollectionCompleteEventArgs(ReadOnlyCollection<FacePhoto> facePhotos)
        {
            FacePhotos = facePhotos;
        }

        public ReadOnlyCollection<FacePhoto> FacePhotos { get; private set; }
    }
}
