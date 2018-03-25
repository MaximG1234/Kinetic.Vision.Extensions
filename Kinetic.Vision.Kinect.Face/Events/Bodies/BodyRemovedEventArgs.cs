using Kinetic.Vision.Kinect.Face.Bodies;
using System;

namespace Kinetic.Vision.Kinect.Face.Events.Bodies
{
    public sealed class BodyRemovedEventArgs : EventArgs
    {
        public BodyRemovedEventArgs(TrackedBody trackedBody, bool didEngage)
        {
            this.TrackingId = trackedBody.TrackingId;
            this.TrackedBody = trackedBody;
            this.DidEngage = didEngage;
        }

        public ulong TrackingId { get; private set; }

        public TrackedBody TrackedBody { get; private set; }

        public bool DidEngage { get; private set; }

    }
}
