using Kinetic.Vision.Kinect.Face.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinetic.Vision.Kinect.Face.Events.Bodies
{
    public sealed class OxfordDataUpdatedEventArgs
    {
        public OxfordDataUpdatedEventArgs(ulong trackingId, double? age, GenderEnum gender)
        {
            this.TrackingId = trackingId;
            this.Age = age;
            this.Gender = gender;
        }

        public ulong TrackingId { get; private set; }

        public double? Age { get; private set; }

        public GenderEnum Gender { get; private set; }
        
    }
}
