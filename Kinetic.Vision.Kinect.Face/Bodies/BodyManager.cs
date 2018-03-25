using Kinetic.Vision.Kinect.Events;
using Kinetic.Vision.Kinect.Face.Engagement;
using Kinetic.Vision.Kinect.Face.Events.Bodies;
using Kinetic.Vision.Kinect.Face.Events.Engagement;
using Kinetic.Vision.Kinect.Face.Events.Face;
using Kinetic.Vision.Kinect.Face.Exceptions;
using Kinetic.Vision.Kinect.Face.Interfaces;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace Kinetic.Vision.Kinect.Face.Bodies
{
    public class BodyManager : Dictionary<ulong, TrackedBody>, IReadOnlyDictionary<ulong, TrackedBody>, INotifyCollectionChanged, INotifyPropertyChanged, IDisposable
    {
        private const ColorImageFormat ImageFormat = ColorImageFormat.Bgra;
        private const bool DefaultFaceTrackingEnabled = false;
        private const ulong DefaultMaxMissedFrames = 46;

        private readonly int MaximumTrackedBodies = 3;

        public event PropertyChangedEventHandler PropertyChanged;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event EventHandler<BodyEngagedEventArgs> Engaged;
        public event EventHandler<BodyDisengagedEventArgs> Disengaged;
        public event EventHandler<BodyAddedEventArgs> BodyAdded;
        public event EventHandler<BodyRemovedEventArgs> BodyRemoved;
        public event EventHandler<BodyUpdatedEventArgs> BodyUpdated;
        public event EventHandler<EngagedBodyUpdatedEventArgs> EngagedBodyUpdated;
        public event EventHandler<FaceFrameEventArgs> FaceFrameArrived;
        public event EventHandler<FaceFrameCompleteEventArgs> FaceFrameComplete;
        public event EventHandler<OxfordDataUpdatedEventArgs> OxfordDataUpdated;

        private readonly List<ulong> _EngagedBodyIds = new List<ulong>();
        private readonly FrameDescription _ColorFrameDescription;
        private readonly IKineticEngagementManager _EngagementManager;
        private readonly KinectManager _KinectManager;
        private readonly ulong _MaxMissedFrames;
        private readonly bool _FaceTrackingEnabled;

        public BodyManager(KinectManager kinectManager) : this(kinectManager, DefaultFaceTrackingEnabled, DefaultMaxMissedFrames) { }

        public BodyManager(KinectManager kinectManager, bool faceTrackingEnabled) : this(kinectManager, faceTrackingEnabled, DefaultMaxMissedFrames) { }

        public BodyManager(KinectManager kinectManager, bool faceTrackingEnabled, ulong maxMissedFrames)
        {
            _KinectManager = kinectManager;
            _KinectManager.FrameArrived += KinectManager_FrameArrived;

            _ColorFrameDescription = kinectManager.KinectSensor.ColorFrameSource.CreateFrameDescription(ImageFormat);

            _FaceTrackingEnabled = faceTrackingEnabled;
            _MaxMissedFrames = maxMissedFrames;

            _EngagementManager = new NearestPersonEngagementManager2(_KinectManager, this);
            _EngagementManager.Engaged += EngagementManager_Engaged;
            _EngagementManager.Disengaged += EngagementManager_Disengaged;
        }
        
        public void Dispose()
        {
            _KinectManager.FrameArrived -= KinectManager_FrameArrived;
            _EngagementManager.Engaged -= EngagementManager_Engaged;
            _EngagementManager.Disengaged -= EngagementManager_Disengaged;

            foreach (var item in this)
            {
                item.Value.FaceFrameArrived -= TrackedBody_FaceFrameArrived;
                item.Value.OxfordDataUpdated -= TrackedBody_OxfordDataUpdated;
                item.Value.Dispose();
            }
        }

        public void AddEngagedBody(ulong trackingId)
        {
            _EngagedBodyIds.Add(trackingId);
        }

        public void RemoveEngagedBody(ulong trackingId)
        {
            _EngagedBodyIds.Remove(trackingId);
        }

        public void ClearEngagedBodies()
        {
            _EngagedBodyIds.Clear();
        }

        public IReadOnlyCollection<TrackedBody> EngagedBodies
        {
            get
            {
                return this.Where(o => _EngagedBodyIds.Exists(m => m == o.Key)).Select(o => o.Value)
                           .ToList()
                           .AsReadOnly();
            }
        }

        public IReadOnlyCollection<ulong> EngagedBodyIds
        {
            get
            {
                return _EngagedBodyIds.AsReadOnly();
            }
        }

        public bool IsBodyEngaged(ulong trackingId)
        {
            //Don't use linq here, this has gotta be fast.
            for (int i = 0; i < _EngagedBodyIds.Count; i++)
            {
                if (_EngagedBodyIds[i] == trackingId)
                    return true;
            }

            return false;
        }

        private void EngagementManager_Engaged(object sender, Events.Engagement.BodyEngagedEventArgs e)
        {
            if (!this.IsBodyEngaged(e.TrackingId) && this.EngagedBodies.Count == 0)
            {
                this.AddEngagedBody(e.TrackingId);
                RaiseEngagedEventHandler(sender, e);
            }
        }


        private void EngagementManager_Disengaged(object sender, BodyDisengagedEventArgs e)
        {
            if (this.IsBodyEngaged(e.TrackingId))
            {
                this.RemoveEngagedBody(e.TrackingId);
                RaiseDisengagedEventHandler(sender, e);
            }
        }

        private void KinectManager_FrameArrived(object sender, KinectFrameArrivedEventArgs e)
        {
            UpdateBodies(_KinectManager.KinectSensor, e.Bodies, e.FrameNumber);
        }
        
        private void UpdateBodies(KinectSensor sensor, Body[] bodies, ulong frameNumber)
        {
            
            //Update existing or add new bodies
            foreach (Body body in bodies.GetTrackedBodies())
            {
                TrackedBody trackedBody;
                if (this.TryGetValue(body.TrackingId, out trackedBody))
                {
                    trackedBody.LastTrackedFrame = frameNumber;
                    trackedBody.UpdateJoints(body.Joints);

                    RaiseBodyUpdatedEvent(sensor, body, frameNumber);

                    if (this.IsBodyEngaged(body.TrackingId))
                        RaiseEngagedBodyUpdatedEvent(sensor, body, frameNumber);
                }
                else if (this.Count < MaximumTrackedBodies)
                {
                    var newTrackedBody = new TrackedBody(_KinectManager, body, frameNumber, _ColorFrameDescription.Width, _ColorFrameDescription.Height);

                    newTrackedBody.FaceFrameArrived += TrackedBody_FaceFrameArrived;
                    newTrackedBody.FaceFrameComplete += TrackedBody_FaceFrameComplete;
                    newTrackedBody.OxfordDataUpdated += TrackedBody_OxfordDataUpdated;

                    if (_FaceTrackingEnabled)
                        newTrackedBody.BeginFaceTracking();
                     
                        
                    this.Add(body.TrackingId, newTrackedBody);

                    RaiseBodyAddedEvent(body);
                    RaiseCollectionChangedEvent(NotifyCollectionChangedAction.Add, body);
                }
            }

            //Calculate which bodies to remove
            var trackersToRemove = new List<ulong>();
            foreach (KeyValuePair<ulong, TrackedBody> item in this)
            {
                ulong missedFrames = frameNumber - item.Value.LastTrackedFrame;
                if (missedFrames > _MaxMissedFrames)
                {
                    trackersToRemove.Add(item.Key);
                }
            }

            //Remove the bodies
            foreach (ulong trackingId in trackersToRemove)
            {
                TrackedBody removedTrackedBody;
                if (this.TryGetValue(trackingId, out removedTrackedBody))
                {
                    if (_FaceTrackingEnabled)
                        removedTrackedBody.EndFaceTracking();

                    removedTrackedBody.FaceFrameArrived -= TrackedBody_FaceFrameArrived;
                    removedTrackedBody.FaceFrameComplete -= TrackedBody_FaceFrameComplete;
                    removedTrackedBody.OxfordDataUpdated -= TrackedBody_OxfordDataUpdated;

                    bool didEngage = this.IsBodyEngaged(trackingId);

                    this.Remove(trackingId);
                    this.RemoveEngagedBody(trackingId);

                    RaiseBodyRemovedEvent(removedTrackedBody, didEngage);
                    RaiseCollectionChangedEvent(NotifyCollectionChangedAction.Remove, removedTrackedBody);
                    removedTrackedBody.Dispose();
                }
                else
                {
                    throw new BodyNotFoundException();
                }               
                    
            }

            //_Stopwatch.Stop();
            //Console.WriteLine(_Stopwatch.ElapsedTicks);
        }
        
        private void TrackedBody_FaceFrameArrived(object sender, FaceFrameEventArgs e)
        {
            RaiseFaceFrameArrivedEvent(sender, e);
        }

        private void TrackedBody_FaceFrameComplete(object sender, FaceFrameCompleteEventArgs e)
        {
            RaiseFaceFrameCompleteFrame(sender, e);
        }

        private void TrackedBody_OxfordDataUpdated(object sender, OxfordDataUpdatedEventArgs e)
        {
            RaiseOxfordDataUpdatedEvent(sender, e);
        }

        #region Raise Event Methods
        private void RaiseOxfordDataUpdatedEvent(object sender, OxfordDataUpdatedEventArgs e)
        {
            EventHandler<OxfordDataUpdatedEventArgs> handler = OxfordDataUpdated;
            if (handler != null)
                handler(sender, e);
        }

        private void RaiseEngagedBodyUpdatedEvent(KinectSensor sensor, Body body, ulong frameNumber)
        {
            EventHandler<EngagedBodyUpdatedEventArgs> handler = EngagedBodyUpdated;
            if (handler != null)
                handler(this, new EngagedBodyUpdatedEventArgs(sensor, body, frameNumber));
        }

        private void RaiseBodyUpdatedEvent(KinectSensor sensor, Body body, ulong frameNumber)
        {
            EventHandler<BodyUpdatedEventArgs> handler = BodyUpdated;
            if (handler != null)
                handler(this, new BodyUpdatedEventArgs(sensor, body, frameNumber));
        }

        private void RaiseBodyAddedEvent(Body body)
        {
            EventHandler<BodyAddedEventArgs> handler = BodyAdded;
            if (handler != null)
                handler(this, new BodyAddedEventArgs(body));
        }

        private void RaiseBodyRemovedEvent(TrackedBody trackedBody, bool DidEngage)
        {
            EventHandler<BodyRemovedEventArgs> handler = BodyRemoved;
            if (handler != null)
                handler(this, new BodyRemovedEventArgs(trackedBody, DidEngage));
        }
        
        private void RaiseCollectionChangedEvent(NotifyCollectionChangedAction changedAction, object changedItem)
        {
            NotifyCollectionChangedEventHandler handler = CollectionChanged;
            if (handler != null)
                handler(this, new NotifyCollectionChangedEventArgs(changedAction, changedItem));
        }

        private void RaiseFaceFrameArrivedEvent(object sender, FaceFrameEventArgs faceFrame)
        {
            EventHandler<FaceFrameEventArgs> handler = FaceFrameArrived;
            if (handler != null)
                handler(sender, faceFrame);
        }

        private void RaiseEngagedEventHandler(object sender, BodyEngagedEventArgs eventArgs)
        {
            EventHandler<BodyEngagedEventArgs> handler = Engaged;
            if (handler != null)
            {
                handler(sender, eventArgs);
            }
        }

        private void RaiseDisengagedEventHandler(object sender, BodyDisengagedEventArgs eventArgs)
        {
            EventHandler<BodyDisengagedEventArgs> handler = Disengaged;
            if (handler != null)
            {
                handler(sender, eventArgs);
            }
        }

        private void RaiseFaceFrameCompleteFrame(object sender, FaceFrameCompleteEventArgs eventArgs)
        {
            EventHandler<FaceFrameCompleteEventArgs> handler = FaceFrameComplete;
            if (handler != null)
            {
                handler(sender, eventArgs);
            }
        }
        #endregion
    }
}
