using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Events
{
    public sealed class KinectFrameArrivedEventArgs : EventArgs
    {
        public ulong FrameNumber { get; set; }
        public KinectSensor KinectSensor { get; set; }
        public byte[] ColorPixels { get; set; }
        public ushort[] DepthPixels { get; set; }
        public Body[] Bodies { get; set; }
        public byte[] BodyIndexPixels { get; set; }
    }
}
