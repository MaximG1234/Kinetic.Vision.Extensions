using System.Threading.Tasks;
using Kinetic.Vision.Kinect.Face.Enumerations;
using Kinetic.Vision.Kinect.Face.Events.Bodies;
using Kinetic.Vision.Kinect.Face.Events.Face;
using Kinetic.Vision.Kinect.Face.Image;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using Microsoft.ProjectOxford.Face;

namespace Kinetic.Vision.Kinect.Face.Bodies
{
    public sealed class TrackedBody : IDisposable, INotifyPropertyChanged
    {
        private static readonly List<FaceAttributeType> DefaultFaceAttributes = new List<FaceAttributeType>()
        {
                        FaceAttributeType.Age,
                        FaceAttributeType.Gender
        };

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
                                                                  | FaceFrameFeatures.PointsInInfraredSpace
                                                                  | FaceFrameFeatures.RightEyeClosed
                                                                  | FaceFrameFeatures.RotationOrientation;

        
        private const int MinimumSharpnessQuality = 550;
        private const int MaxAttributesHistory = 10;
        private const int MaxOxfordHistory = 3;
        private const int MaxOxfordFailure = 5;
        private const int MaxHighQualityFaceCaptures = 30;
        private const int MaxFaceQueueSize = 8;
        private const int MaxEngagementAngle = 20;

        private const ColorImageFormat ImageFormat = ColorImageFormat.Bgra;

        public event EventHandler<OxfordDataUpdatedEventArgs> OxfordDataUpdated;
        public event EventHandler<TrackingIdLostEventArgs> TrackingIdLost;
        public event EventHandler<FaceFrameEventArgs> FaceFrameArrived; // Do not do cpu intensive tasks with FaceFrameArrived. Causes everything to slow down.
        public event EventHandler<FaceFrameCompleteEventArgs> FaceFrameComplete;
        public event PropertyChangedEventHandler PropertyChanged;

      
        private readonly ConcurrentQueue<FaceCapture> _FaceCaptureQueue = new ConcurrentQueue<FaceCapture>();
        private readonly ConcurrentBag<FaceCapture> _HighQualityFaceCaptures = new ConcurrentBag<FaceCapture>();

        private readonly ConcurrentQueue<IReadOnlyDictionary<FaceProperty, DetectionResult>> _BodyAttributesHistory = new ConcurrentQueue<IReadOnlyDictionary<FaceProperty, DetectionResult>>();
        private readonly ConcurrentBag<Tuple<GenderEnum, double>> _OxfordDataHistory = new ConcurrentBag<Tuple<GenderEnum, double>>();

        private readonly FaceServiceClient _FaceServiceClient = new FaceServiceClient(Properties.Settings.Default.ProjectOxford_PrimaryKey);
        private readonly Stopwatch _StopWatch = new Stopwatch();
        private readonly FaceFrameReader _FaceFrameReader;
        private readonly FaceFrameSource _FaceFrameSource;

        private readonly KinectManager _KinectManager;
        private readonly FrameDescription _ColorFrameDesc;
        private readonly FrameDescription _DepthFrameDesc;
        private readonly DateTime _Created;
        private readonly ulong _FirstTrackedFrame;
        private readonly int _DisplayWidth;
        private readonly int _DisplayHeight;
        
        private IReadOnlyDictionary<JointType, Joint> _LastKnownJoints;
        private IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> _FacePointsInfrared;
        private IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> _FacePointsColor;
        private FaceFrameResult _CurrentFaceFrameResult;
        private ulong _LastTrackedFrame;
        private ulong _TrackingId;
        private int _Yaw;
        private int _Pitch;
        private int _Roll;
        private int _CurrentFailureCount;
        private RectF _ColorBoundingBox;
        private CancellationTokenSource _CancellationTokenSource;
        
        private double _DepthHFoV_Half_Rad_Tangent;
        private bool _IsOnScreen = false;
        private bool _IsFacingScreen = false;
        
        private int _NumFramesContinuousLooking = 0;
        private float _ShoulderAngle = 90;
        private float _ShoulderWidth = 0;
        private float _HipWidth = 0;
        private float _TorsoHeight = 0;

        public TrackedBody(KinectManager kinectManager, Body body, ulong currentFrame, int displayWidth, int displayHeight)
        {
            //Console.WriteLine("Tracked body invoked.");
            _KinectManager = kinectManager;
            _KinectManager.FrameArrived += KinectManager_FrameArrived;
            
            _Created = DateTime.UtcNow;
            _LastKnownJoints = body.Joints;
            
            this.TrackingId = body.TrackingId;
            this.LastTrackedFrame = currentFrame;

            _FirstTrackedFrame = currentFrame;

            _DisplayWidth = displayWidth;
            _DisplayHeight = displayHeight;

            _ColorFrameDesc = _KinectManager.KinectSensor.ColorFrameSource.CreateFrameDescription(ImageFormat);
            _DepthFrameDesc = _KinectManager.KinectSensor.DepthFrameSource.FrameDescription;
            
            _FaceFrameSource = new FaceFrameSource(_KinectManager.KinectSensor, 0, DefaultFaceFeatures);
            _FaceFrameReader = _FaceFrameSource.OpenReader();
            _FaceFrameReader.FrameArrived += OnFaceFrameArrived;
            _FaceFrameReader.FaceFrameSource.TrackingIdLost += OnTrackingIdLost;
            _FaceFrameReader.FaceFrameSource.TrackingId = this.TrackingId;
            _FaceFrameReader.IsPaused = true;

            _DepthHFoV_Half_Rad_Tangent = Math.Tan(_DepthFrameDesc.HorizontalFieldOfView / 2 / 180 * Math.PI);
        }

        
        public void Dispose()
        {
            if (_FaceFrameReader != null)
            {
                if (!_FaceFrameReader.IsPaused)
                    this.EndFaceTracking();

                _FaceFrameReader.FrameArrived -= OnFaceFrameArrived;
                _FaceFrameReader.FaceFrameSource.TrackingIdLost -= OnTrackingIdLost;
                _FaceFrameReader.FaceFrameSource.TrackingIdLost -= OnTrackingIdLost;
                _FaceFrameReader.FaceFrameSource.TrackingId = 0;                
            }

            if (_FaceFrameSource != null)
                _FaceFrameSource.Dispose();

            _KinectManager.FrameArrived -= KinectManager_FrameArrived;

            foreach (var item in _FaceCaptureQueue)
                item.Dispose();

            foreach (var item in _HighQualityFaceCaptures)
                item.Dispose();

        }
        
        public void BeginFaceTracking()
        {
            if (_FaceFrameReader.IsPaused)
            {
                _FaceFrameReader.IsPaused = false;
                _CancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => RunBiometricsThread(_CancellationTokenSource.Token));
            }   
        }

        public void EndFaceTracking()
        {
            _FaceFrameReader.IsPaused = true;
            _FaceFrameReader.FaceFrameSource.TrackingId = 0;
            _CancellationTokenSource.Cancel();
        }

        private void OnTrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            RaiseBodyTrackingLostEvent(sender, e);
        }
 
        private void OnFaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            if (e.FrameReference != null)
            {
                using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
                {
                    if (faceFrame != null)
                        _CurrentFaceFrameResult = faceFrame.FaceFrameResult;
                }
            }
        }

        private void KinectManager_FrameArrived(object sender, Kinect.Events.KinectFrameArrivedEventArgs e)
        {
            if (_CurrentFaceFrameResult != null)
            {
                _StopWatch.Reset();
                _StopWatch.Start();

                if (_CurrentFaceFrameResult.ValidateFaceBoxAndPoints(_DisplayWidth, _DisplayHeight))
                {
                    AddBodyAttributeHistory(_CurrentFaceFrameResult);
                    this.ColorBoundingBox = _CurrentFaceFrameResult.FaceBoundingBoxInColorSpace.Offset(0.40f, 0.50f, 0.70f, e.KinectSensor.ColorFrameSource.FrameDescription.Width, 
                                                                                                                            e.KinectSensor.ColorFrameSource.FrameDescription.Height);
                    
                    if (!this.ColorBoundingBox.IsEmpty())
                    {
                        try
                        {
                            byte[] colorFace = e.ColorPixels.ExtractPixelsFromImage(Convert.ToInt32(_ColorBoundingBox.Y), Convert.ToInt32(_ColorBoundingBox.X),
                                                                                    Convert.ToInt32(_ColorBoundingBox.Width), Convert.ToInt32(_ColorBoundingBox.Height),
                                                                                    _ColorFrameDesc.Width,
                                                                                    Convert.ToInt32(_ColorFrameDesc.BytesPerPixel));
                            

                            if (_CurrentFaceFrameResult.FacePointsInColorSpace != null)
                                this.FacePointsColor = _CurrentFaceFrameResult.FacePointsInColorSpace;

                            if (_CurrentFaceFrameResult.FacePointsInInfraredSpace != null)
                                this.FacePointsInfrared = _CurrentFaceFrameResult.FacePointsInInfraredSpace;

                            if (colorFace != null)
                            {
                                AddToFaceCaptureQueue(_CurrentFaceFrameResult, this.ColorBoundingBox, colorFace);
                                RaiseFaceFrameArrivedEvent(this, this.ColorBoundingBox, colorFace, this.TrackingId);
                            }

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("An error likely occured extracting image because it was outside bounds. Just ignore for now: {0}", ex.Message);
                        }

                    }
                }

                _StopWatch.Stop();
                RaiseFaceFrameCompleteEvent(this.TrackingId, _StopWatch.Elapsed);
            }
        }

        private void AddToFaceCaptureQueue(FaceFrameResult frameResult, RectF colorBoundingBox, byte[] face)
        {
            Joint head = _LastKnownJoints[JointType.Head];
            if (head.IsTracked() && _HighQualityFaceCaptures.Count < MaxHighQualityFaceCaptures && _FaceCaptureQueue.Count < MaxFaceQueueSize) 
            {
                int pitch;
                int yaw;
                int roll;

                frameResult.FaceRotationQuaternion.ExtractFaceRotationInDegrees(out pitch, out yaw, out roll);

                _Yaw = yaw;
                _Roll = roll;
                _Pitch = pitch;

                FaceCapture capture = new FaceCapture(face, Convert.ToInt32(colorBoundingBox.Width), Convert.ToInt32(colorBoundingBox.Height))
                {
                    Distance = Convert.ToInt32(head.Position.DistanceToCamera() * 1000),
                    Pitch = pitch,
                    Yaw = yaw,
                    Roll = roll,
                    Left = Convert.ToInt32(colorBoundingBox.X),
                    Top = Convert.ToInt32(colorBoundingBox.Y)
                };
                
                //Console.WriteLine("Face Capture Created! Current Face Capture Count : {0}", _HighQualityFaceCaptures.Count);
                _FaceCaptureQueue.Enqueue(capture);
                
            }
        }

        /// <summary>
        /// Adds to the history collection keeping an up-to-date buffer of body state datapoints.
        /// Used for moving average values of attributes.
        /// </summary>
        /// <param name="frameResult">Push in a frame result</param>
        private void AddBodyAttributeHistory(FaceFrameResult frameResult)
        {
            if (_BodyAttributesHistory.Count >= MaxAttributesHistory)
            {
                IReadOnlyDictionary<FaceProperty, DetectionResult> attributes;
                _BodyAttributesHistory.TryDequeue(out attributes);
            }

            _BodyAttributesHistory.Enqueue(frameResult.FaceProperties);
        }

        private void AddOxfordDataHistory(Tuple<GenderEnum, double> data)
        {
            _OxfordDataHistory.Add(data);
            RaiseOxfordDataUpdatedEvent();
            this.NotifyPropertyChanged(PropertyChanged, "Age");
            this.NotifyPropertyChanged(PropertyChanged, "Gender");
            this.NotifyPropertyChanged(PropertyChanged, "AgeString");
            this.NotifyPropertyChanged(PropertyChanged, "GenderString");
        }

        private void AddHighQualityFaceCapture(FaceCapture capture)
        {
            if (_HighQualityFaceCaptures.Count < MaxHighQualityFaceCaptures)
            {
                _HighQualityFaceCaptures.Add(capture);
                this.NotifyPropertyChanged(PropertyChanged, "ColorPhoto");
            }
        }

        //Warning! This method causes side-effects...
        private async Task<Tuple<GenderEnum, double>> ObtainFaceDemographics(Bitmap faceBitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                faceBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                stream.Position = 0;

                try
                {
                    //Console.WriteLine("Processing Oxford Request beginning");
                    var faces = await _FaceServiceClient.DetectAsync(stream, false, true, DefaultFaceAttributes);
                    
                    if (faces != null && faces.Count() > 0)
                    {
                        var currentFace = faces[0];
                        GenderEnum gender = currentFace.FaceAttributes.Gender.ToLower() == "male" ? GenderEnum.Male : GenderEnum.Female;
                        return Tuple.Create(gender, currentFace.FaceAttributes.Age);
                    }
                }
                catch (Exception ex)
                {
                    _CurrentFailureCount++;
#if DEBUG
                    Console.WriteLine("Processing Oxford Request failed with : {0}", ex.Message);
#endif
                    await Task.Delay(3500);
                }                
            }

            return null;
        }

        private async void RunBiometricsThread(CancellationToken cancellationToken)
        {
            //Console.WriteLine("Biometrics Thread Started");

            while (!cancellationToken.IsCancellationRequested) 
            {
                FaceCapture capture;
                if (_FaceCaptureQueue.TryDequeue(out capture))
                {
                    int sharpness = SharpnessAnalyser.Analyse(capture.Image);
                    if (sharpness > MinimumSharpnessQuality)
                    {
                        AddHighQualityFaceCapture(capture);

                        if (_OxfordDataHistory.Count < MaxOxfordHistory && _CurrentFailureCount < MaxOxfordFailure)
                        {
                            Tuple<GenderEnum, double> result = await ObtainFaceDemographics(capture.Image);
                            if (result != null)
                                AddOxfordDataHistory(result);
                        }
                    }
                }
            }

            //Console.WriteLine("Biometrics Thread Exited");
        }

        public int NumFramesContinuousLooking
        {
            get
            {
                return _NumFramesContinuousLooking;
            }
        }

        public DateTime Created
        {
            get
            {
                return _Created;
            }
        }
        public bool IsOnScreen
        {
            get
            {
                return _IsOnScreen;
            }
        }
        public bool IsFacingScreen
        {
            get
            {
                return _IsFacingScreen;
            }
        }
        public float ShoulderAngle
        {
            get
            {
                return _ShoulderAngle;
            }
        }
        public float ShoulderWidth
        {
            get
            {
                return _ShoulderWidth;
            }
        }
        public float HipWidth
        {
            get
            {
                return _HipWidth;
            }
        }
        public float TorsoHeight
        {
            get
            {
                return _TorsoHeight;
            }
        }
        public int Yaw 
        { 
            get
            {
                return _Yaw;
            }
        }

        public int Pitch
        {
            get
            {
                return _Pitch;
            }
        }

        public int Roll
        {
            get
            {
                return _Roll;
            }
        }

        public ulong TrackingId
        {
            get
            {
                return _TrackingId;
            }
            private set
            {
                ulong newValue = value;
                if (newValue != _TrackingId)
                {
                    _TrackingId = value;
                    this.NotifyPropertyChanged(PropertyChanged);
                }
            }
        }
        
        public ulong LastTrackedFrame
        {
            get
            {
                return _LastTrackedFrame;
            }
            set
            {
                ulong newValue = value;
                if (newValue != _LastTrackedFrame)
                {
                    _LastTrackedFrame = value;
                    this.NotifyPropertyChanged(PropertyChanged);
                }
            }
        }
        

        public ulong TotalTrackedFrames
        {
            get
            {
                return _LastTrackedFrame - _FirstTrackedFrame;
            } 
        }

        public RectF ColorBoundingBox
        {
            get
            {
                return _ColorBoundingBox;
            }
            private set
            {
                RectF newBox = value;
                if (newBox != _ColorBoundingBox)
                {
                    _ColorBoundingBox = value;
                    this.NotifyPropertyChanged(PropertyChanged);
                }
            }
        }

        public IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> FacePointsInfrared
        {
            get
            {
                return _FacePointsInfrared;
            }
            private set
            {
                IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> newPoints = value;
                if (newPoints != _FacePointsInfrared)
                {
                    _FacePointsInfrared = newPoints;
                    this.NotifyPropertyChanged(PropertyChanged);
                }
            }
        }

        public IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> FacePointsColor
        {
            get
            {
                return _FacePointsColor;
            }
            private set
            {
                IReadOnlyDictionary<FacePointType, Microsoft.Kinect.PointF> newPoints = value;
                if (newPoints != _FacePointsColor)
                {
                    _FacePointsColor = newPoints;
                    this.NotifyPropertyChanged(PropertyChanged);
                }
            }
        }


        public double? Age
        {
            get
            {
                if (_OxfordDataHistory.Count > 0)
                    return _OxfordDataHistory.Average(o => o.Item2);
                else
                    return null;
            }
        }

        public GenderEnum Gender
        {
            get
            {
                if (_OxfordDataHistory.Count > 0)
                    return (GenderEnum)_OxfordDataHistory.Average(o => Convert.ToDouble(o.Item1));
                else
                    return GenderEnum.Unknown;
            }
        }

        public byte[] ColorPhoto
        {
            get
            {
                FaceCapture capture = _HighQualityFaceCaptures.OrderBy(o => o.Quality).OrderByDescending(o => o.Distance).FirstOrDefault();
                if (capture != null)
                    return capture.Image.ToBytes();

                return null;
            }
        }

        public string AgeString
        {
            get
            {
                if (_OxfordDataHistory.Count > 0)
                    return string.Format("{0} years old.", this.Age);
                else
                    return "Unknown";
            }
        }

        public string GenderString
        {
            get
            {
                return string.Format("{0}", this.Gender);
            }
        }

        public IReadOnlyDictionary<JointType, Joint> Joints
        {
            get
            {
                return _LastKnownJoints;
            }
        }

        public DetectionResult Happy
        {
            get
            {
                return GetDetectionState(FaceProperty.Happy);
            }
        }

        public DetectionResult Engaged
        {
            get
            {
                return GetDetectionState(FaceProperty.Engaged);
            }
        }

        public DetectionResult LeftEyeClosed
        {
            get
            {
                return GetDetectionState(FaceProperty.LeftEyeClosed);
            }
        }

        public DetectionResult RightEyeClosed
        {
            get
            {
                return GetDetectionState(FaceProperty.RightEyeClosed);
            }
        }

        public DetectionResult LookingAway
        {
            get
            {
                return GetDetectionState(FaceProperty.LookingAway);
            }
        }

        public DetectionResult WearingGlasses
        {
            get
            {
                return GetDetectionState(FaceProperty.WearingGlasses);
            }
        }

        public DetectionResult MouthMoved
        {
            get
            {
                return GetDetectionState(FaceProperty.MouthMoved);
            }
        }

        public DetectionResult MouthOpen
        {
            get
            {
                return GetDetectionState(FaceProperty.MouthOpen);
            }
        }

        public DetectionResult GetDetectionState(FaceProperty faceProperty)
        {
            if (_BodyAttributesHistory.Count > 0)
                return (DetectionResult)_BodyAttributesHistory.Select(o => o[faceProperty]).Average(o => Convert.ToDouble(o));
            else
                return DetectionResult.Unknown;
        }
        //Stopwatch _stopwatch = new Stopwatch();
        public void UpdateJoints(IReadOnlyDictionary<JointType, Joint> joints)
        {

            _LastKnownJoints = joints;
            Joint SpineShoulderJoint = joints[JointType.SpineShoulder];
            Joint ShoulderLeftJoint = joints[JointType.ShoulderLeft];
            Joint ShoulderRightJoint = joints[JointType.ShoulderRight];
            Joint HipLeftJoint = joints[JointType.HipLeft];
            Joint HipRightJoint = joints[JointType.HipRight];
            Joint SpineBaseJoint = joints[JointType.SpineBase];


            if (ShoulderLeftJoint.IsTracked() && ShoulderRightJoint.IsTracked() && HipLeftJoint.IsTracked() && HipRightJoint.IsTracked() && SpineShoulderJoint.IsTracked() && SpineBaseJoint.IsTracked())
            {
                

                _ShoulderAngle = (float)(180 /Math.PI * Math.Atan((ShoulderLeftJoint.Position.Z - ShoulderRightJoint.Position.Z) / (ShoulderLeftJoint.Position.X - ShoulderRightJoint.Position.X)) );
                if(Math.Abs(_ShoulderAngle) < MaxEngagementAngle)
                {
                    _IsFacingScreen = true;
                    _ShoulderWidth = (float)Math.Sqrt(
                                (ShoulderLeftJoint.Position.X - ShoulderRightJoint.Position.X) * (ShoulderLeftJoint.Position.X - ShoulderRightJoint.Position.X)
                               + (ShoulderLeftJoint.Position.Y - ShoulderRightJoint.Position.Y) * (ShoulderLeftJoint.Position.Y - ShoulderRightJoint.Position.Y)
                               + (ShoulderLeftJoint.Position.Z - ShoulderRightJoint.Position.Z) * (ShoulderLeftJoint.Position.Z - ShoulderRightJoint.Position.Z)
                               );
                    _HipWidth = (float)Math.Sqrt(
                                    (HipLeftJoint.Position.X - HipRightJoint.Position.X) * (HipLeftJoint.Position.X - HipRightJoint.Position.X)
                                   + (HipLeftJoint.Position.Y - HipRightJoint.Position.Y) * (HipLeftJoint.Position.Y - HipRightJoint.Position.Y)
                                   + (HipLeftJoint.Position.Z - HipRightJoint.Position.Z) * (HipLeftJoint.Position.Z - HipRightJoint.Position.Z)
                                   );
                    _TorsoHeight = (float)Math.Sqrt(
                                    (SpineShoulderJoint.Position.X - SpineBaseJoint.Position.X) * (SpineShoulderJoint.Position.X - SpineBaseJoint.Position.X)
                                   + (SpineShoulderJoint.Position.Y - SpineBaseJoint.Position.Y) * (SpineShoulderJoint.Position.Y - SpineBaseJoint.Position.Y)
                                   + (SpineShoulderJoint.Position.Z - SpineBaseJoint.Position.Z) * (SpineShoulderJoint.Position.Z - SpineBaseJoint.Position.Z)
                                   );
                }
                else
                {
                    _IsFacingScreen = false;
                }

                double x = SpineShoulderJoint.Position.Z * _DepthHFoV_Half_Rad_Tangent;
                if(SpineShoulderJoint.Position.X > x/1.78/2 || SpineShoulderJoint.Position.X < -x / 1.78 / 2)
                {
                    _IsOnScreen = false;
                }
                else
                {
                    _IsOnScreen = true;
                }
            }
            else
            {
                _IsOnScreen = false;
                _IsFacingScreen = false;
            }

        }

        private void RaiseOxfordDataUpdatedEvent()
        {
            EventHandler<OxfordDataUpdatedEventArgs> handler = OxfordDataUpdated;
            if (handler != null)
                handler(this, new OxfordDataUpdatedEventArgs(this.TrackingId, this.Age, this.Gender));
        }

        private void RaiseBodyTrackingLostEvent(object sender, TrackingIdLostEventArgs args)
        {
            EventHandler<TrackingIdLostEventArgs> handler = TrackingIdLost;
            if (handler != null)
                handler(sender, args);
        }

        private void RaiseFaceFrameArrivedEvent(object sender, RectF boundingBox, byte[] face, ulong trackingId)
        {
            EventHandler<FaceFrameEventArgs> handler = FaceFrameArrived;
            if (handler != null)
            {
                handler(sender, new FaceFrameEventArgs()
                {
                    ColorBoundingBox = boundingBox,
                    Face = face,
                    TrackingId = trackingId,
                    TrackedBody = this
                });
            }
        }

        private void RaiseFaceFrameCompleteEvent(ulong trackingId, TimeSpan frameTime)
        {
            EventHandler<FaceFrameCompleteEventArgs> handler = FaceFrameComplete;
            if (handler != null)
            {
                handler(this, new FaceFrameCompleteEventArgs(trackingId, frameTime));
            }
        }
        
    }
}
