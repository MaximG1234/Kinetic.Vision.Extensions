using Kinetic.Vision.Kinect.Face.Bodies;
using Kinetic.Vision.Kinect.Face.Events.Face;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kinetic.Vision.Kinect.Face
{
    [Obsolete]
    public class FaceManager 
    {
        public event EventHandler<FaceFrameEventArgs> FaceFrameArrived;

        private const ColorImageFormat ImageFormat = ColorImageFormat.Bgra;
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

        private readonly List<FaceFrameSource> _FaceFrameSources = new List<FaceFrameSource>();
        private readonly List<FaceFrameReader> _FaceFrameReaders = new List<FaceFrameReader>();
        private readonly List<ulong> _TrackedIds = new List<ulong>();
        private readonly FaceFrameFeatures _FaceFrameFeatures;
        private readonly KinectManager _KinectManager;
        private readonly BodyManager _BodyManager;
        private readonly FrameDescription _ColorFrameDesc;
        private readonly byte[] _ColorPixels;

        public FaceManager(KinectManager kinectManager, BodyManager bodyManager) : this(kinectManager, bodyManager, DefaultFaceFeatures)
        {
        }

        public FaceManager(KinectManager kinectManager, BodyManager bodyManager, FaceFrameFeatures faceFrameFeatures)
        {
            _KinectManager = kinectManager;

            _BodyManager = bodyManager;            
            _BodyManager.EngagedBodyUpdated += BodyManager_EngagedBodyUpdated;
            _BodyManager.BodyRemoved += BodyManager_BodyRemoved;
            
            _FaceFrameFeatures = faceFrameFeatures;

            _ColorFrameDesc = kinectManager.KinectSensor.ColorFrameSource.CreateFrameDescription(ImageFormat);
            _ColorPixels = new byte[_ColorFrameDesc.Width * _ColorFrameDesc.Height * _ColorFrameDesc.BytesPerPixel];

            CreateFaceTrackers(_KinectManager.KinectSensor.BodyFrameSource.BodyCount);
        }

        ~FaceManager()
        {
            _BodyManager.EngagedBodyUpdated -= BodyManager_EngagedBodyUpdated;
        }

        private void BodyManager_EngagedBodyUpdated(object sender, Events.Bodies.EngagedBodyUpdatedEventArgs e)
        {
            if (_BodyManager.IsBodyEngaged(e.Body.TrackingId))
            {
                if (!_TrackedIds.Contains(e.Body.TrackingId))
                {
                    _TrackedIds.Add(e.Body.TrackingId);
                    
                    StartFaceTracking(e.Body.TrackingId);
                }

            }
        }
        private void BodyManager_BodyRemoved(object sender, Events.Bodies.BodyRemovedEventArgs e)
        {
            if (_TrackedIds.Contains(e.TrackingId))
            {
                PauseFaceTracking(e.TrackingId);
            }
        }

        private void PauseFaceTracking(ulong trackingId)
        {
            FaceFrameReader faceFrameReader = _FaceFrameReaders.Where(o => o.FaceFrameSource.TrackingId == trackingId).FirstOrDefault();
            if (faceFrameReader != null)
            {
                faceFrameReader.FaceFrameSource.TrackingId = 0;
                faceFrameReader.IsPaused = true;
            }
                
        }

        private void StartFaceTracking(ulong trackingId)
        {
            FaceFrameReader faceFrameReader = _FaceFrameReaders.Where(o => o.FaceFrameSource.TrackingId == 0).FirstOrDefault();
            if (faceFrameReader != null)
            {
                faceFrameReader.FaceFrameSource.TrackingId = trackingId;
                faceFrameReader.IsPaused = false;
            }
        }

        private void CreateFaceTrackers(int total)
        {
            for (int i = 0; i < total; i++)
            {
                var faceFrameSource = new FaceFrameSource(_KinectManager.KinectSensor, 0, _FaceFrameFeatures);
                var faceFrameReader = faceFrameSource.OpenReader();

                faceFrameReader.FrameArrived += OnFaceFrameArrived;
                faceFrameSource.TrackingIdLost += OnTrackingIdLost;

                _FaceFrameReaders.Add(faceFrameReader);
                _FaceFrameSources.Add(faceFrameSource);
            }            
        }

        private void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            if (e.FrameReference != null)
            {
                using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
                {
                    if (faceFrame != null)
                    {
                        FaceFrameResult frameResult = faceFrame.FaceFrameResult;

                        if (frameResult != null)
                        {
                            using (ColorFrame colorFrame = faceFrame.ColorFrameReference.AcquireFrame())
                            {
                                if (colorFrame != null)
                                {
                                    RectF currentColorBoundingBox = frameResult.FaceBoundingBoxInColorSpace.Offset(0.40f, 0.50f, 0.70f, colorFrame.FrameDescription.Width, colorFrame.FrameDescription.Height);

                                    if (colorFrame.RawColorImageFormat == ImageFormat)
                                        colorFrame.CopyRawFrameDataToArray(_ColorPixels);
                                    else
                                        colorFrame.CopyConvertedFrameDataToArray(_ColorPixels, ImageFormat);

                                    byte[] colorFace = _ColorPixels.ExtractPixelsFromImage(Convert.ToInt32(currentColorBoundingBox.Y), Convert.ToInt32(currentColorBoundingBox.X), 
                                                                                           Convert.ToInt32(currentColorBoundingBox.Width), Convert.ToInt32(currentColorBoundingBox.Height),
                                                                                           _ColorFrameDesc.Width,
                                                                                           Convert.ToInt32(_ColorFrameDesc.BytesPerPixel));

                                    RaiseFaceFrameArrivedEvent(colorFace, currentColorBoundingBox, faceFrame.TrackingId);
                                }
                            }
                        }
                    }
                }
            }

            
        }
 
        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            _BodyManager.RemoveEngagedBody(e.TrackingId);
            //_BodyManager.RemoveEngagedBody(e.TrackingId);
        }

        protected void RaiseFaceFrameArrivedEvent(byte[] face, RectF colorBoundingBox, ulong trackingId)
        {
            EventHandler<FaceFrameEventArgs> handler = FaceFrameArrived;
            if (handler != null)
            {
                handler(this, new FaceFrameEventArgs()
                {
                    Face = face,
                    TrackingId = trackingId,
                    ColorBoundingBox = colorBoundingBox
                });
            }
                
        }
    }
}
