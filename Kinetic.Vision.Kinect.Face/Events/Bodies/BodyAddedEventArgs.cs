using Microsoft.Kinect;

namespace Kinetic.Vision.Kinect.Face.Events.Bodies
{
    public sealed class BodyAddedEventArgs
    {
        public BodyAddedEventArgs(Body body)
        {
            Body = body;
        }

        public Body Body { get; private set; }
    }
}
