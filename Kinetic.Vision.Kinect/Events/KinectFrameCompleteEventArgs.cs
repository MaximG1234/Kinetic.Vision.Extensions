using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Events
{
    public class KinectFrameCompleteEventArgs
    {
        public KinectFrameCompleteEventArgs(TimeSpan frameTime)
        {
            this.FrameTime = frameTime;
        }

        public TimeSpan FrameTime { get; private set; }
    }
}
