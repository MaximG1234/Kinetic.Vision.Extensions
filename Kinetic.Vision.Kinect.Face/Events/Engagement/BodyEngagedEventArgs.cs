using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face.Events.Engagement
{
    public sealed class BodyEngagedEventArgs
    {
        public BodyEngagedEventArgs(ulong trackingId)
        {
            this.TrackingId = trackingId;
        }

        public ulong TrackingId { get; private set; }
    }
}
