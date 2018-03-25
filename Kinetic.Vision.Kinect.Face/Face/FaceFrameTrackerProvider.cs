using System.Collections.Concurrent;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face.Face
{
    public class FaceFrameTrackerProvider : IDisposable
    {
        private const FaceFrameFeatures DefaultFaceFeatures = FaceFrameFeatures.BoundingBoxInColorSpace
            //| FaceFrameFeatures.BoundingBoxInInfraredSpace
                                                                | FaceFrameFeatures.FaceEngagement
                                                                | FaceFrameFeatures.Glasses
                                                                | FaceFrameFeatures.Happy
                                                                | FaceFrameFeatures.LeftEyeClosed
                                                                | FaceFrameFeatures.LookingAway
                                                                | FaceFrameFeatures.MouthMoved
                                                                | FaceFrameFeatures.MouthOpen
                                                                | FaceFrameFeatures.PointsInColorSpace
            //| FaceFrameFeatures.PointsInInfraredSpace
                                                                | FaceFrameFeatures.RightEyeClosed
                                                                | FaceFrameFeatures.RotationOrientation;

        private readonly ConcurrentBag<FaceFrameReader> _FaceFrameReaders = new ConcurrentBag<FaceFrameReader>();

        private KinectSensor _KinectSensor;

        public FaceFrameTrackerProvider(KinectSensor kinectSensor)
        {
            _KinectSensor = kinectSensor;

            for (int i = 0; i < _KinectSensor.BodyFrameSource.BodyCount + 40; i++)
            {
                var faceFrameSource = new FaceFrameSource(_KinectSensor, 0, DefaultFaceFeatures);
                var faceFrameReader = faceFrameSource.OpenReader();
                faceFrameReader.IsPaused = true;
                _FaceFrameReaders.Add(faceFrameReader);
            }
        }

        public void Dispose()
        {
            foreach (var item in _FaceFrameReaders)
            {
                item.IsPaused = true;
                item.FaceFrameSource.Dispose();
                item.FaceFrameSource.TrackingId = 0;
                item.Dispose();
            }
        }
        static int counter = 0;

        public FaceFrameReader GetNextReader(ulong trackingId)
        {
            Console.WriteLine(counter);
            FaceFrameReader reader = _FaceFrameReaders.Where(o => o.IsPaused && o.FaceFrameSource.TrackingId == 0).FirstOrDefault();
            counter++;
            return reader;
        }

    }
}
