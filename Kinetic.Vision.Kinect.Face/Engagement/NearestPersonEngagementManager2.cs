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
    public class NearestPersonEngagementManager2 : IKineticEngagementManager, IDisposable
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

        public NearestPersonEngagementManager2(KinectManager kinectManager, BodyManager bodyManager)
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

        //
        private double FramesEngaged = 0;
        private double BodyDistance, EngagedBodyDistance;
        private ulong EngagedBodyKey = ulong.MaxValue;
        //private int EngagedBodyIndex = int.MaxValue;
        private const int MinFramesEngaged = 20;
        private const float MinPositionDifference = 0.3f;
        private const int MaxBodyAngleForEngagement = 40;
        private void KinectManager_FrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            //Find body closest to centre of FoV
            double ClosestBodyDistance = 3*3;
            ulong ClosestBodyKey = ulong.MaxValue;
            bool EngagedBodyStillPresent = false;
            int i = 0;
            foreach (var body in _BodyManager)
            {

                if (body.Value.IsOnScreen)
                {
                    // Ideal position is at X=0, Z=1.5
                    BodyDistance = body.Value.Joints[JointType.SpineShoulder].Position.X * body.Value.Joints[JointType.SpineShoulder].Position.X
                                    + (body.Value.Joints[JointType.SpineShoulder].Position.Z - 1.5) * (body.Value.Joints[JointType.SpineShoulder].Position.Z - 1.5);

                    if (BodyDistance < ClosestBodyDistance)
                    {
                        ClosestBodyDistance = BodyDistance;
                        ClosestBodyKey = body.Value.TrackingId;
                    }
                }

                // This IF must always be tested!!
                if (body.Key == EngagedBodyKey) // Identifies the currently engaged body - to allow for it to be disengaged
                {
                    EngagedBodyStillPresent = true;
                }
                i++;
            }
            if (!EngagedBodyStillPresent)
            {
                EngagedBodyKey = ulong.MaxValue;
            }
            if (ClosestBodyKey == ulong.MaxValue || ClosestBodyKey == EngagedBodyKey)
            {
                //If no bodies fall within the max range
                // OR
                //If the closest body is the previously engaged body
                FramesEngaged = 0;
            }
            else
            {
                //If the closest body is NOT the previously engaged body
                //Then check if closest body is engaged (hand over spinemid) and if previously engaged body is a certain distance further than closest body

                Joint handLeft = _BodyManager[ClosestBodyKey].Joints[JointType.HandLeft];
                Joint handRight = _BodyManager[ClosestBodyKey].Joints[JointType.HandRight];
                Joint spineMid = _BodyManager[ClosestBodyKey].Joints[JointType.SpineMid];
                //Joint shoulderLeft = _BodyManager[ClosestBodyKey].Joints[JointType.ShoulderLeft];
                //Joint shoulderRight = _BodyManager[ClosestBodyKey].Joints[JointType.ShoulderRight];

                
                if (handLeft.IsTracked() && handRight.IsTracked() && spineMid.IsTracked())// && shoulderLeft.IsTracked() && shoulderRight.IsTracked())
                {
                    // If hand above hip and body facing sensor
                    if ( (handLeft.Position.Y > spineMid.Position.Y || handRight.Position.Y > spineMid.Position.Y) && _BodyManager[ClosestBodyKey].IsFacingScreen)
                    {
                        // If there is a currently engaged body, then compare distances.
                        if (EngagedBodyKey < ulong.MaxValue)
                        {
                            if (_BodyManager[EngagedBodyKey].IsOnScreen)
                            {
                                BodyDistance = Math.Sqrt(_BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.X * _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.X +
                                                         _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.Z * _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.Z);

                                EngagedBodyDistance = Math.Sqrt(_BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.X * _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.X +
                                                                _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.Z * _BodyManager[ClosestBodyKey].Joints[JointType.SpineShoulder].Position.Z);

                                if (EngagedBodyDistance - BodyDistance > MinPositionDifference)
                                {
                                    FramesEngaged++;
                                }
                                else
                                {
                                    FramesEngaged = 0;
                                }
                            }
                            else
                            {
                                FramesEngaged++;
                            }

                        }
                        else
                        {
                            FramesEngaged++;
                        }

                        if (FramesEngaged > MinFramesEngaged)
                        {
                            if (EngagedBodyKey < ulong.MaxValue)
                            {
                                RaiseDisengagedEventHandler(_BodyManager[EngagedBodyKey].TrackingId);
                            }
                            RaiseEngagedEventHandler(_BodyManager[ClosestBodyKey].TrackingId);
                            EngagedBodyKey = _BodyManager[ClosestBodyKey].TrackingId;
                            FramesEngaged = 0;
                        }
                    }
                }
            }
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

        private void RaiseDisengagedEventHandler(ulong trackingId)
        {
            EventHandler<BodyDisengagedEventArgs> handler = Disengaged;
            if (handler != null)
            {
                handler(this, new BodyDisengagedEventArgs(trackingId));
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

