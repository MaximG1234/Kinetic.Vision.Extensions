using Kinetic.Vision.Kinect.Events;
using Kinetic.Vision.Kinect.Face.Bodies;
using Kinetic.Vision.Kinect.Face.Events.Bodies;
using Kinetic.Vision.Kinect.Face.Events.Engagement;
using Kinetic.Vision.Kinect.Face.Interfaces;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Kinetic.Vision.Kinect.Face.Engagement
{
    public class SinglePersonEngagementManager : IKineticEngagementManager, IDisposable
    {
        private const int MaxHandPositionHistory = 12;
        private const float MaxEngagementDistance = 2.5f;

        public event EventHandler<BodyEngagedEventArgs> Engaged;
        public event EventHandler<BodyDisengagedEventArgs> Disengaged;

        private readonly List<HandEngagementPosition> _HandPositionHistory = new List<HandEngagementPosition>();
        
        private readonly KinectManager _KinectManager;
        private readonly BodyManager _BodyManager;

        private int _EngagedPeopleAllowed;
        private bool _IsPaused = false;
        private bool _IsEngaged = false;
        private ulong _CurrentTrackingId = 0;

        public SinglePersonEngagementManager(KinectManager kinectManager, BodyManager bodyManager)
        {
            _KinectManager = kinectManager;
            _KinectManager.FrameArrived += KinectManager_FrameArrived;

            _BodyManager = bodyManager;
            _BodyManager.BodyAdded += BodyManager_BodyAdded;
            _BodyManager.BodyRemoved += BodyManager_BodyRemoved;
            _BodyManager.BodyUpdated += BodyManager_BodyUpdated;
        }
 
        public void Dispose()
        {
            _KinectManager.FrameArrived -= KinectManager_FrameArrived;
            _BodyManager.BodyAdded -= BodyManager_BodyAdded;
            _BodyManager.BodyRemoved -= BodyManager_BodyRemoved;
        }

        private void BodyManager_BodyAdded(object sender, BodyAddedEventArgs e)
        {
              
        }
        

        private void BodyManager_BodyUpdated(object sender, BodyUpdatedEventArgs e)
        {
            if (!_IsPaused && !_IsEngaged)
            {
                Joint handTipLeft = e.Body.Joints[JointType.HandTipLeft];
                Joint handTipRight = e.Body.Joints[JointType.HandTipRight];
                Joint spineShoulder = e.Body.Joints[JointType.SpineShoulder];

                if (handTipLeft.IsTracked() && handTipRight.IsTracked() && spineShoulder.IsTracked())
                {
                    if (spineShoulder.Position.DistanceToCamera() < MaxEngagementDistance)
                    {
                        bool aboveShoulderLeft = handTipLeft.Position.Y - spineShoulder.Position.Y > 0;
                        bool aboveShoulderRight = handTipRight.Position.Y - spineShoulder.Position.Y > 0;

                        EnqueueHandPosition(e.Body.TrackingId, aboveShoulderLeft && aboveShoulderRight);

                        bool handsHovering = AreHandsHovering(e.Body.TrackingId);

                        if (handsHovering)
                        {
                            _IsEngaged = true;
                            _CurrentTrackingId = e.Body.TrackingId;
                            RaiseEngagedEventHandler(e.Body.TrackingId);
                        }
                    }
                }
            }
        }     
        
        private void BodyManager_BodyRemoved(object sender, BodyRemovedEventArgs e)
        {
            //Remove hand position history
            IEnumerable<int> indexesToRemove = _HandPositionHistory.Where(o => !_BodyManager.Any(p => p.Key == o.TrackingId)).Select(o => _HandPositionHistory.IndexOf(o)).Reverse();
            foreach (int index in indexesToRemove)
                _HandPositionHistory.RemoveAt(index);


            if (e.TrackingId == _CurrentTrackingId)
            {
                _CurrentTrackingId = 0;
                _IsEngaged = false;
            }

            foreach (var item in _BodyManager)
            {

            }
        }

        private void EnqueueHandPosition(ulong trackingId, bool handsAboveShoulder)
        {
            var currentPositions = _HandPositionHistory.Where(o => o.TrackingId == trackingId);

            if (currentPositions.Count() >= MaxHandPositionHistory)
                _HandPositionHistory.Remove(currentPositions.FirstOrDefault());

            _HandPositionHistory.Add(new HandEngagementPosition(trackingId, handsAboveShoulder));
        }

        private bool AreHandsHovering(ulong trackingId)
        {
            var capturedHistory = _HandPositionHistory.Where(o => o.TrackingId == trackingId);
            if (capturedHistory.Count() >= MaxHandPositionHistory) 
            {
                bool isHovering = capturedHistory.All(o => o.AboveShoulders);
                return isHovering;
            }
            else
            {
                return false;
            }
        }

   
        private void KinectManager_FrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            
        }

        public bool EngagedBodyHandPairsChanged()
        {
            return false;
        }

        public void StartManaging()
        {
            _IsPaused = false;
        }

        public void StopManaging()
        {
            _IsPaused = true;
        }

        public int EngagedPeopleAllowed
        {
            get
            {
                return _EngagedPeopleAllowed;
            }
            set
            {
                if (value > 2 || value < 0)
                {
                    throw new ArgumentOutOfRangeException("value", value, "This engagement manager requires 0 to 2 people to be set as the EngagedPeopleAllowed");
                }

                _EngagedPeopleAllowed = value;
            }
        }

        public bool IsEngaged
        {
            get
            {
                return _IsEngaged;
            }
        }

        private void RaiseEngagedEventHandler(ulong trackingId)
        {
            EventHandler<BodyEngagedEventArgs> handler = Engaged;
            if (handler != null)
            {
                handler(this, new BodyEngagedEventArgs(trackingId));
            }
        }


        private class HandEngagementPosition
        {
            public HandEngagementPosition(ulong trackingId, bool handsAboveShoulders)
            {
                this.TrackingId = trackingId;
                this.AboveShoulders = handsAboveShoulders;
            }

            public ulong TrackingId { get; private set; }
            public bool AboveShoulders { get; private set; }
        }
    }
}

