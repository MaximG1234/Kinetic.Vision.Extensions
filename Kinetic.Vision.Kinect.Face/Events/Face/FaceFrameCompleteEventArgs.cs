using System;
using Kinetic.Vision.Kinect.Face.Bodies;
using Microsoft.Kinect;
using System.Collections.Generic;
using Microsoft.Kinect.Face;

namespace Kinetic.Vision.Kinect.Face.Events.Face
{
    public class FaceFrameCompleteEventArgs
    {
        public FaceFrameCompleteEventArgs(ulong trackingId, TimeSpan frameTime)
        {
            this.TrackingId = trackingId;
            this.FrameTime = frameTime;
        }

        public ulong TrackingId { get; private set; }
        public TimeSpan FrameTime { get; private set; }
        
    }
}
