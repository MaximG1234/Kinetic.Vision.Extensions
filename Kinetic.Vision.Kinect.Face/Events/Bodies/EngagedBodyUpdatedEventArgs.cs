using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face.Events.Bodies
{
    public sealed class EngagedBodyUpdatedEventArgs : EventArgs
    {
        public EngagedBodyUpdatedEventArgs(KinectSensor sensor, Body body, ulong frameNumber)
        {
            Sensor = sensor;
            Body = body;
            FrameNumber = frameNumber;
        }

        public Body Body { get; private set; }
        public KinectSensor Sensor { get; private set; }
        public ulong FrameNumber { get; private set; }
    }
}
