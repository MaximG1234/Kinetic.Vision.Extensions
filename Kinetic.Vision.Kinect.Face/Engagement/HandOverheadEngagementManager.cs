
using Kinetic.Vision.Kinect.Face.Events.Engagement;
using Kinetic.Vision.Kinect.Face.Interfaces;
using Microsoft.Kinect;
using Microsoft.Kinect.Input;
using System;
using System.Collections.Generic;

namespace Kinetic.Vision.Kinect.Face.Engagement
{
    public class HandOverheadEngagementManager : IKineticEngagementManager
    {
        public event EventHandler<BodyDisengagedEventArgs> Disengaged;
        public event EventHandler<BodyEngagedEventArgs> Engaged;

        private bool _Stopped = true;
        private BodyFrameReader _BodyReader;
        private Body[] _Bodies;
        private bool _EngagementPeopleHaveChanged;
        private List<BodyHandPair> _HandsToEngage;
        private int _EngagedPeopleAllowed;

        public HandOverheadEngagementManager(KinectSensor kinectSensor, int engagedPeopleAllowed) 
        {
            _EngagedPeopleAllowed = engagedPeopleAllowed;
            _BodyReader = kinectSensor.BodyFrameSource.OpenReader();
            _BodyReader.FrameArrived += this.BodyReader_FrameArrived;
            _Bodies = new Body[_BodyReader.BodyFrameSource.BodyCount];
            _HandsToEngage = new List<BodyHandPair>();
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

        public bool EngagedBodyHandPairsChanged()
        {
            return _EngagementPeopleHaveChanged;
        }

        public IReadOnlyList<BodyHandPair> KinectManualEngagedHands
        {
            get
            {
                return KinectCoreWindow.KinectManualEngagedHands;
            }
        }

        public void StartManaging()
        {
            _Stopped = false;
            _BodyReader.IsPaused = false;
        }

        public void StopManaging()
        {
            _Stopped = true;
            _BodyReader.IsPaused = true;
        }

        private void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs args)
        {
            bool gotData = false;

            using (var frame = args.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_Bodies);
                    gotData = true;
                }
            }

            if (gotData && !_Stopped)
            {
                this.TrackEngagedPlayersViaHandOverHead();
            }
        }

        private static bool IsHandOverShoulder(JointType jointType, Body body)
        {
            return (body.Joints[jointType].Position.Y >
                    body.Joints[JointType.SpineShoulder].Position.Y);
        }

        private static bool IsHandBelowHip(JointType jointType, Body body)
        {
            return (body.Joints[jointType].Position.Y <
                    body.Joints[JointType.SpineBase].Position.Y);
        }

        private void TrackEngagedPlayersViaHandOverHead()
        {
            _EngagementPeopleHaveChanged = false;
            var currentlyEngagedHands = KinectCoreWindow.KinectManualEngagedHands;
            _HandsToEngage.Clear();

            // check to see if anybody who is currently engaged should be disengaged
            foreach (var bodyHandPair in currentlyEngagedHands)
            {
                var bodyTrackingId = bodyHandPair.BodyTrackingId;
                foreach (var body in _Bodies)
                {
                    if (body.TrackingId == bodyTrackingId)
                    {
                        // check for disengagement
                        JointType engagedHandJoint = (bodyHandPair.HandType == HandType.LEFT) ? JointType.HandLeft : JointType.HandRight;
                        bool toBeDisengaged = HandOverheadEngagementManager.IsHandBelowHip(engagedHandJoint, body);

                        if (toBeDisengaged)
                        {
                            _EngagementPeopleHaveChanged = true;
                            RaiseDisengagedEventHandler(body.TrackingId);
                        }
                        else
                        {
                            _HandsToEngage.Add(bodyHandPair);
                        }
                    }
                }
            }

            // check to see if anybody should be engaged, if not already engaged
            foreach (var body in _Bodies)
            {
                if (_HandsToEngage.Count < _EngagedPeopleAllowed)
                {
                    bool alreadyEngaged = false;
                    foreach (var bodyHandPair in _HandsToEngage)
                    {
                        alreadyEngaged = (body.TrackingId == bodyHandPair.BodyTrackingId);
                    }

                    if (!alreadyEngaged)
                    {
                        // check for engagement
                        if (HandOverheadEngagementManager.IsHandOverShoulder(JointType.HandLeft, body))
                        {
                            // engage the left hand
                            _HandsToEngage.Add(new BodyHandPair(body.TrackingId, HandType.LEFT));
                            _EngagementPeopleHaveChanged = true;

                            RaiseEngagedEventHandler(body.TrackingId);
                        }
                        else if (HandOverheadEngagementManager.IsHandOverShoulder(JointType.HandRight, body))
                        {
                            // engage the right hand
                            _HandsToEngage.Add(new BodyHandPair(body.TrackingId, HandType.RIGHT));
                            _EngagementPeopleHaveChanged = true;

                            RaiseEngagedEventHandler(body.TrackingId);
                        }
                    }
                }
            }

            if (_EngagementPeopleHaveChanged)
            {
                BodyHandPair firstPersonToEngage = null;
                BodyHandPair secondPersonToEngage = null;

                //Debug.Assert(_HandsToEngage.Count <= 2, "handsToEngage should be <= 2");
                
                switch (_HandsToEngage.Count)
                {
                    case 0:
                        break;
                    case 1:
                        firstPersonToEngage = _HandsToEngage[0];
                        break;
                    case 2:
                        firstPersonToEngage = _HandsToEngage[0];
                        secondPersonToEngage = _HandsToEngage[1];
                        break;
                }

                switch (this.EngagedPeopleAllowed)
                {
                    case 1:
                        KinectCoreWindow.SetKinectOnePersonManualEngagement(firstPersonToEngage);
                        break;
                    case 2:
                        KinectCoreWindow.SetKinectTwoPersonManualEngagement(firstPersonToEngage, secondPersonToEngage);
                        break;
                }
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

        private void RaiseEngagedEventHandler(ulong trackingId)
        {
            EventHandler<BodyEngagedEventArgs> handler = Engaged;
            if (handler != null)
            {
                handler(this, new BodyEngagedEventArgs(trackingId));
            }
        }

    }
}
