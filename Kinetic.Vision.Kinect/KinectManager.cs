using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;
using Kinetic.Vision.Kinect.Events;
using Kinetic.Vision.Kinect.Exceptions;

namespace Kinetic.Vision.Kinect
{
    public unsafe class KinectManager : IDisposable
    {
        public const ColorImageFormat ImageFormat = ColorImageFormat.Bgra;

        public event EventHandler<IsAvailableChangedEventArgs> IsAvailableChanged;
        public event EventHandler<KinectFrameArrivedEventArgs> FrameArrived;
        public event EventHandler<KinectFrameCompleteEventArgs> FrameComplete;


        private static byte[] ColorPixels;
        private static byte[] BodyIndexPixels;
        private static ushort[] DepthPixels;
        private static Body[] Bodies;

        private readonly KinectFrameArrivedEventArgs _KinectFrameArrivedEventArgs = new KinectFrameArrivedEventArgs();
        private readonly FrameSourceTypes _FrameSourceTypes;
        private readonly KinectSensor _KinectSensor;
        private readonly MultiSourceFrameReader _MultiSourceFrameReader;

        private readonly FrameDescription _ColorFrameDescription;
        private readonly FrameDescription _DepthFrameDescription;
        private readonly FrameDescription _BodyIndexFrameDescription;

        private readonly Stopwatch _Stopwatch = new Stopwatch();

        private ulong _FrameNumber = 0;
        private bool _IsRunning = false;

        public KinectManager()
            : this(FrameSourceTypes.Body |
                   FrameSourceTypes.BodyIndex |
                   FrameSourceTypes.Color |
                   FrameSourceTypes.Depth)
        {

        }

        public KinectManager(FrameSourceTypes frameSourceTypes)
        {
            _FrameSourceTypes = frameSourceTypes;

            _KinectSensor = KinectSensor.GetDefault();
            _KinectSensor.IsAvailableChanged += Kinect_IsAvailableChanged;

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Color))
                _ColorFrameDescription = _KinectSensor.ColorFrameSource.CreateFrameDescription(ImageFormat);

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Depth))
                _DepthFrameDescription = _KinectSensor.DepthFrameSource.FrameDescription;

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.BodyIndex))
                _BodyIndexFrameDescription = _KinectSensor.BodyIndexFrameSource.FrameDescription;

            _MultiSourceFrameReader = _KinectSensor.OpenMultiSourceFrameReader(_FrameSourceTypes);

            _MultiSourceFrameReader.MultiSourceFrameArrived += FrameReader_MultiSourceFrameArrived;

            InitializeArrays(_KinectSensor.ColorFrameSource.CreateFrameDescription(ImageFormat),
                               _KinectSensor.DepthFrameSource.FrameDescription,
                               _KinectSensor.BodyIndexFrameSource.FrameDescription,
                               _KinectSensor.BodyFrameSource.BodyCount);
        }


        public void Start()
        {
            _KinectSensor.Open();

            //Block to wait for Kinect to wake up
            System.Threading.Thread.CurrentThread.Join(1500);

            if (!_KinectSensor.IsOpen || !_KinectSensor.IsAvailable)
                throw new KinectUnavailableException();

            _IsRunning = true;
        }

        public void Stop()
        {
            _KinectSensor.Close();
            _IsRunning = false;
        }

        public KinectSensor KinectSensor
        {
            get
            {
                return _KinectSensor;
            }
        }

        public FrameDescription ColorFrameDescription
        {
            get
            {
                return _ColorFrameDescription;
            }
        }

        public FrameDescription DepthFrameDescription
        {
            get
            {
                return _DepthFrameDescription;
            }
        }

        public FrameDescription BodyIndexFrameDescription
        {
            get
            {
                return _BodyIndexFrameDescription;
            }
        }

        public bool IsRunning
        {
            get
            {
                return _IsRunning;
            }
            set
            {
                _IsRunning = value;
            }
        }

        public ulong FrameNumber
        {
            get
            {
                return _FrameNumber;
            }
        }

        public void Dispose()
        {
            _KinectSensor.IsAvailableChanged -= Kinect_IsAvailableChanged;
            _MultiSourceFrameReader.MultiSourceFrameArrived -= FrameReader_MultiSourceFrameArrived;

            if (_KinectSensor.IsOpen)
                _KinectSensor.Close();
        }

        private void InitializeArrays(FrameDescription colorFrameDescription, FrameDescription depthFrameDescription, FrameDescription bodyIndexDescription, int bodyCount)
        {
            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Color))
                ColorPixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel];

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Depth))
                DepthPixels = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Body))
                Bodies = new Body[bodyCount];

            if (_FrameSourceTypes.HasFlag(FrameSourceTypes.BodyIndex))
                BodyIndexPixels = new byte[bodyIndexDescription.Width * bodyIndexDescription.Height * bodyIndexDescription.BytesPerPixel];
        }

        #region Frame Event Handler 

        private unsafe void FrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {

            _Stopwatch.Restart();
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame != null)
            {
                ColorFrame colorFrame = null;
                DepthFrame depthFrame = null;
                BodyFrame bodyFrame = null;
                BodyIndexFrame bodyIndexFrame = null;

                try
                {
                    bool allRequiredDataReceived = true;

                    if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Color))
                    {
                        colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                        if (colorFrame != null)
                        {
                            fixed (byte* colorBytesPointer = ColorPixels)
                            {
                                IntPtr colorPtr = (IntPtr)colorBytesPointer;
                                uint size = (uint)(_ColorFrameDescription.Width * _ColorFrameDescription.Height * _ColorFrameDescription.BytesPerPixel);
                                if (colorFrame.RawColorImageFormat == ImageFormat)
                                    colorFrame.CopyRawFrameDataToIntPtr(colorPtr, size);
                                else
                                    colorFrame.CopyConvertedFrameDataToIntPtr(colorPtr, size, ImageFormat);
                            }
                        }
                        else
                        {
                            allRequiredDataReceived = false;
                        }
                    }
                        

                    if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Depth) && allRequiredDataReceived)
                    {
                        depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                        if (depthFrame != null)
                        {
                            fixed (ushort* depthBytesPointer = DepthPixels)
                            {
                                IntPtr depthPtr = (IntPtr)depthBytesPointer;
                                depthFrame.CopyFrameDataToIntPtr(depthPtr, (uint)(_DepthFrameDescription.Width * _DepthFrameDescription.Height * _DepthFrameDescription.BytesPerPixel));
                            }
                        }
                        else
                        {
                            allRequiredDataReceived = false;
                        }

                    }


                    if (_FrameSourceTypes.HasFlag(FrameSourceTypes.Body) && allRequiredDataReceived)
                    {
                        bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();
                        if (bodyFrame != null)
                        {
                            bodyFrame.GetAndRefreshBodyData(Bodies);
                        }
                        else
                        {
                            allRequiredDataReceived = false;
                        }
                    }
                        

                    if (_FrameSourceTypes.HasFlag(FrameSourceTypes.BodyIndex) && allRequiredDataReceived)
                    {
                        bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();
                        if (bodyIndexFrame != null)
                        {
                            fixed (byte* bodyIndexBytesPointer = BodyIndexPixels)
                            {
                                IntPtr bodyIndexPtr = (IntPtr)bodyIndexBytesPointer;
                                bodyIndexFrame.CopyFrameDataToIntPtr(bodyIndexPtr, (uint)(_BodyIndexFrameDescription.Width * _BodyIndexFrameDescription.Height * _BodyIndexFrameDescription.BytesPerPixel));
                            }
                        }
                        else
                        {
                            allRequiredDataReceived = false;
                        }
                    }
                    
                    if (allRequiredDataReceived)
                    {
                        _KinectFrameArrivedEventArgs.ColorPixels = ColorPixels;
                        _KinectFrameArrivedEventArgs.DepthPixels = DepthPixels;
                        _KinectFrameArrivedEventArgs.Bodies = Bodies;
                        _KinectFrameArrivedEventArgs.BodyIndexPixels = BodyIndexPixels;
                        _KinectFrameArrivedEventArgs.KinectSensor = multiSourceFrame.KinectSensor;
                        _KinectFrameArrivedEventArgs.FrameNumber = _FrameNumber;

                        EventHandler<KinectFrameArrivedEventArgs> handler = FrameArrived;
                        if (handler != null)
                            handler(this, _KinectFrameArrivedEventArgs);
                    }
                    
                }
                finally
                {
                    if (colorFrame != null)
                        colorFrame.Dispose();

                    if (depthFrame != null)
                        depthFrame.Dispose();

                    if (bodyFrame != null)
                        bodyFrame.Dispose();

                    if (bodyIndexFrame != null)
                        bodyIndexFrame.Dispose();
                }

            }

            _Stopwatch.Stop();
            RaiseKinectFrameComplete(_Stopwatch.Elapsed);
            _FrameNumber++;
        }

        #endregion

        #region Kinect Events

        private void Kinect_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            EventHandler<IsAvailableChangedEventArgs> handler = IsAvailableChanged;
            if (handler != null)
                handler(this, e);
        }

        private void RaiseKinectFrameComplete(TimeSpan frameTime)
        {
            EventHandler<KinectFrameCompleteEventArgs> handler = FrameComplete;
            if (handler != null)
            {
                handler(this, new KinectFrameCompleteEventArgs(frameTime));
            }
        }

        #endregion




    }
}
