using Microsoft.Kinect;
using System;

namespace Kinetic.Vision.Kinect.Face.Events.Bodies
{
    public sealed class BodyUpdatedEventArgs : EventArgs
    {
        public BodyUpdatedEventArgs(KinectSensor sensor, Body body, ulong frameNumber)
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
