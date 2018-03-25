using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kinetic.Vision.Kinect.Face.Bodies;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Kinetic.Vision.Kinect.Face.Exceptions;
using Kinetic.Vision.Kinect.Face.Events.Face;

namespace Kinetic.Vision.Kinect.Face
{
    public class FacePhotoTaker : IDisposable
    {
        private const int TotalSamples = 40;

        private const ColorImageFormat ColorImageFormat = Microsoft.Kinect.ColorImageFormat.Bgra;
        private const FaceFrameFeatures TrackedFaceFeatures = FaceFrameFeatures.BoundingBoxInColorSpace
                                                            | FaceFrameFeatures.RotationOrientation
                                                            | FaceFrameFeatures.LookingAway
                                                            | FaceFrameFeatures.Happy
                                                            | FaceFrameFeatures.PointsInColorSpace
                                                            | FaceFrameFeatures.FaceEngagement;

        public event EventHandler<PhotoCollectionCompleteEventArgs> CollectionComplete;
        public event EventHandler<PhotoCollectedEventArgs> PhotoCollected;
        public event EventHandler<FacePhotoBodyLostEventArgs> BodyLost;

        private readonly Dictionary<ulong, FaceFrameSource> _FaceFrameSources = new Dictionary<ulong, FaceFrameSource>();
        private readonly Dictionary<ulong, FaceFrameReader> _FaceFrameReaders = new Dictionary<ulong, FaceFrameReader>();
        private readonly List<FacePhoto> _FacePhotos = new List<FacePhoto>();

        private readonly KinectSensor _KinectSensor;
        private readonly BodyManager _BodyManager;
        private readonly FrameDescription _ColorFrameDescription;
        private readonly int _TotalSamples;
        private readonly byte[] _ColorPixels;
        private readonly bool _OnlyCaptureTrackedBodies;

        private bool _Continue;

        public FacePhotoTaker(KinectSensor kinectSensor, BodyManager bodyManager, bool onlyCaptureTrackedBodies) : this(TotalSamples, kinectSensor, bodyManager, onlyCaptureTrackedBodies) { }
        public FacePhotoTaker(int totalSamples, KinectSensor kinectSensor, BodyManager bodyManager, bool onlyCaptureTrackedBodies)
        {
            _TotalSamples = totalSamples;
            _KinectSensor = kinectSensor;
            _BodyManager = bodyManager;

            _ColorFrameDescription = kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat);
            _ColorPixels = new byte[_ColorFrameDescription.Width * _ColorFrameDescription.Height * _ColorFrameDescription.BytesPerPixel];

            _OnlyCaptureTrackedBodies = onlyCaptureTrackedBodies;
        }

        public void Dispose()
        {
            ClearFaceFrameCaptureObjects();
        }

        private void FaceFrameReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            if (_Continue)
            {
                if (e.FrameReference != null)
                {
                    using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
                    {
                        using (ColorFrame colorFrame = faceFrame.ColorFrameReference.AcquireFrame())
                        {
                            if (faceFrame != null && colorFrame != null)
                            {
                                FaceFrameResult frameResult = faceFrame.FaceFrameResult;

                                if (frameResult != null)
                                {
                                    if (!AllBodySamplesCollected())
                                    {
                                        if (!BodySamplesComplete(frameResult.TrackingId))
                                        {
                                            var currentTrackedBody = _BodyManager.Where(o => o.Key == frameResult.TrackingId).FirstOrDefault();
                                            if (currentTrackedBody.Value != null)
                                            {
                                                
                                                //Joint head = currentTrackedBody.Value.Body.Joints[JointType.Head];

                                                //if (head.TrackingState != TrackingState.NotTracked)
                                                //{

                                                //    if (colorFrame.RawColorImageFormat == ColorImageFormat)
                                                //        colorFrame.CopyRawFrameDataToArray(_ColorPixels);
                                                //    else
                                                //        colorFrame.CopyConvertedFrameDataToArray(_ColorPixels, ColorImageFormat);

                                                //    RectF currentColorBoundingBox = frameResult.FaceBoundingBoxInColorSpace.Offset(0.40f, 0.50f, 0.70f);

                                                //    if (currentColorBoundingBox.X > 0 && currentColorBoundingBox.Y > 0 && currentColorBoundingBox.Height > 0 && currentColorBoundingBox.Width > 0)
                                                //    {
                                                //        byte[] colorFace = _ColorPixels.ExtractPixelsFromImage(Convert.ToInt32(currentColorBoundingBox.Y), Convert.ToInt32(currentColorBoundingBox.X),
                                                //                                                          Convert.ToInt32(currentColorBoundingBox.Width), Convert.ToInt32(currentColorBoundingBox.Height),
                                                //                                                          _ColorFrameDescription.Width, Convert.ToInt32(_ColorFrameDescription.BytesPerPixel));

                                                //        Bitmap faceBitmap = colorFace.ToBitmap(Convert.ToInt32(currentColorBoundingBox.Width), Convert.ToInt32(currentColorBoundingBox.Height));

                                                //        FacePhoto photo = new FacePhoto()
                                                //        {
                                                //            Distance = Convert.ToInt32(head.Position.DistanceToCamera() * 1000),
                                                //            TrackingId = frameResult.TrackingId,
                                                //            Image = faceBitmap
                                                //        };

                                                //        _FacePhotos.Add(photo);

                                                //        RaisePhotoCollected(photo);
                                                //    }
                                                //}
                                            }
                                        }
                                    }
                                    else
                                    {
                                        Stop();
                                        RaiseCollectionComplete(_FacePhotos.AsReadOnly());
                                    }
                                }
                            }
                        }
                    }
                }
            }
           
        }

        private bool BodySamplesComplete(ulong trackingId) 
        {
            return _FacePhotos.Where(o => o.TrackingId == trackingId).Count() >= _TotalSamples;
        }

        private bool AllBodySamplesCollected()
        {
            bool result = true;

            var sampleAggregates = _BodyManager.Select(o => new 
            { 
                TrackingId = o.Key,
                SamplesCollected = _FacePhotos.Where(p => p.TrackingId == o.Key).Count() 
            });

            foreach (var item in sampleAggregates)
            {
                if (item.SamplesCollected < _TotalSamples)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        private void FaceFrameSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            FaceFrameReader reader;
            if (_FaceFrameReaders.TryGetValue(e.TrackingId, out reader))
            {
                FaceFrameSource source;
                if (_FaceFrameSources.TryGetValue(e.TrackingId, out source))
                {
                    reader.IsPaused = true;
                    reader.FrameArrived -= FaceFrameReader_FrameArrived;
                    reader.Dispose();

                    source.TrackingIdLost -= FaceFrameSource_TrackingIdLost;
                    source.Dispose();

                    _FaceFrameReaders.Remove(e.TrackingId);
                    _FaceFrameSources.Remove(e.TrackingId);

                    if (_BodyManager.Count == 0)
                        _Continue = false;

                    RaiseBodyLost(e.TrackingId, _FaceFrameSources.Count);

                    Console.WriteLine("Tracker for bodyId {0} destroyed.", e.TrackingId);
                }
                else
                {
                    throw new BodyPhotoTakerInvalidStateException();
                }
            }
            else
            {
                throw new BodyPhotoTakerInvalidStateException();
            }
        }
        
        public void Start()
        {
            _Continue = true;

            if ((_OnlyCaptureTrackedBodies && _BodyManager.EngagedBodies.Count > 0) || (!_OnlyCaptureTrackedBodies && _BodyManager.Count > 0))
            {
                if (_OnlyCaptureTrackedBodies)
                {
                    foreach (var trackedBody in _BodyManager.EngagedBodies)
                    {
                        BeginFaceTracking(trackedBody.TrackingId);
                    }
                }
                else
                {
                    foreach (var trackedBody in _BodyManager)
                    {
                        BeginFaceTracking(trackedBody.Key);
                    }
                }
            }
        }

        private void BeginFaceTracking(ulong trackingId)
        {
            if (!_FaceFrameReaders.ContainsKey(trackingId))
            {
                FaceFrameSource frameSource = new FaceFrameSource(_KinectSensor, trackingId, TrackedFaceFeatures);
                frameSource.TrackingIdLost += FaceFrameSource_TrackingIdLost;

                FaceFrameReader frameReader = frameSource.OpenReader();
                frameReader.FrameArrived += FaceFrameReader_FrameArrived;

                _FaceFrameSources.Add(trackingId, frameSource);
                _FaceFrameReaders.Add(trackingId, frameReader);

                //Console.WriteLine("Created face frame tracker for bodyId {0}.", trackedBody.Value.Body.TrackingId);
            }
        }

        public void Stop()
        {
            _Continue = false;
        }

        public void ClearCapturedPhotos()
        {
            _FacePhotos.Clear();
        }

        private void ClearFaceFrameCaptureObjects()
        {
            foreach (var item in _FaceFrameReaders)
            {
                if (item.Value != null)
                {
                    item.Value.FrameArrived -= FaceFrameReader_FrameArrived;
                    item.Value.Dispose();
                }
            }

            foreach (var item in _FaceFrameSources)
            {
                if (item.Value != null)
                {
                    item.Value.TrackingIdLost -= FaceFrameSource_TrackingIdLost;
                    item.Value.Dispose();
                }
            }

            _FaceFrameReaders.Clear();
            _FaceFrameSources.Clear();
        }

        private void RaiseBodyLost(ulong trackingId, int currentSubjects)
        {
            EventHandler<FacePhotoBodyLostEventArgs> handler = BodyLost;
            if (handler != null)
            {
                handler(this, new FacePhotoBodyLostEventArgs() { TrackingId = trackingId, 
                                                        CurrentSubjects = currentSubjects });
            }
        }

        private void RaisePhotoCollected(FacePhoto photo)
        {
            EventHandler<PhotoCollectedEventArgs> handler = PhotoCollected;
            if (handler != null)
            {
                handler(this, new PhotoCollectedEventArgs(photo));
            }
        }

        private void RaiseCollectionComplete(ReadOnlyCollection<FacePhoto> facePhotos)
        {
            EventHandler<PhotoCollectionCompleteEventArgs> handler = CollectionComplete;
            if (handler != null)
            {
                handler(this, new PhotoCollectionCompleteEventArgs(facePhotos));
            }
                
        }

    }
}
