using Kinetic.Vision.Kinect.Face.Events.Engagement;
using System;

namespace Kinetic.Vision.Kinect.Face.Interfaces
{
    public interface IKineticEngagementManager
    {

        event EventHandler<BodyEngagedEventArgs> Engaged;
        event EventHandler<BodyDisengagedEventArgs> Disengaged;

        /// <summary>
        /// Per frame, have the set of engaged body hand pairs changed.
        /// </summary>
        /// <returns></returns>
        bool EngagedBodyHandPairsChanged();

        /// <summary>
        /// Called when the manager should start managing which BodyHandPairs are engaged.
        /// </summary>
        void StartManaging();

        /// <summary>
        /// Called when the manager should stop managing which BodyHandPairs are engaged.
        /// </summary>
        void StopManaging();
    }
}
