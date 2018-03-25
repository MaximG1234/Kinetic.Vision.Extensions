using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kinetic.Vision.Utilities;

namespace Microsoft.Kinect
{
    public static class KinectExtensions
    {
        private const short DefaultMaxTrackedDistanceMM = 4000;
        private const float DefaultMinHandConfidence = 0.4f;

        /// <summary>
        /// Face rotation display angle increment in degrees
        /// </summary>
        private const double FaceRotationIncrementInDegrees = 0.5;
        
        /// <summary>
        /// Converts rotation quaternion to Euler angles 
        /// And then maps them to a specified range of values to control the refresh rate
        /// </summary>
        /// <param name="rotQuaternion">face rotation quaternion</param>
        /// <param name="pitch">rotation about the X-axis</param>
        /// <param name="yaw">rotation about the Y-axis</param>
        /// <param name="roll">rotation about the Z-axis</param>
        public static void ExtractFaceRotationInDegrees(this Vector4 rotQuaternion, out int pitch, out int yaw, out int roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            // convert face rotation quaternion to Euler angles in degrees
            double yawD, pitchD, rollD;
            pitchD = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yawD = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            rollD = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;

            // clamp the values to a multiple of the specified increment to control the refresh rate
            double increment = FaceRotationIncrementInDegrees;
            pitch = (int)(Math.Floor((pitchD + ((increment / 2.0) * (pitchD > 0 ? 1.0 : -1.0))) / increment) * increment);
            yaw = (int)(Math.Floor((yawD + ((increment / 2.0) * (yawD > 0 ? 1.0 : -1.0))) / increment) * increment);
            roll = (int)(Math.Floor((rollD + ((increment / 2.0) * (rollD > 0 ? 1.0 : -1.0))) / increment) * increment);
        }

        public static bool IsInfinity(this ColorSpacePoint point)
        {
            return float.IsInfinity(point.X) || float.IsInfinity(point.Y);
        }

        public static bool IsEmpty(this RectF rect)
        {
            return rect.X == 0 && rect.Y == 0 && rect.Width == 0 && rect.Height == 0;
        }

        public static bool IsEmpty(this CameraSpacePoint point)
        {
            return point.X == 0 && point.Y == 0 && point.Z == 0;
        }

        public static double DistanceToCamera(this CameraSpacePoint point)
        {
            return KinectExtensions.DistanceTo(point, new CameraSpacePoint() { X = 0, Y = 0, Z = 0 });
        }

        public static double DistanceTo(this CameraSpacePoint point1, CameraSpacePoint point2)
        {
            return Math.Sqrt((point1.X - point2.X) * (point1.X - point2.X) +
                             (point1.Y - point2.Y) * (point1.Y - point2.Y) +
                             (point1.Z - point2.Z) * (point1.Z - point2.Z));
        }

        public static bool IsTracked(this Joint joint)
        {
            return joint.TrackingState != TrackingState.NotTracked;
        }

        public static PointF ToColorEuclid(this ColorSpacePoint point, float z, int width, int height)
        {
            if (!point.IsInfinity())
            {
                return new PointF()
                {
                    X = Convert.ToSingle(EuclidUtilities.ToColorEuclidX(Convert.ToInt32(point.X), z, width)),
                    Y = Convert.ToSingle(EuclidUtilities.ToColorEuclidY(Convert.ToInt32(point.Y), z, height))
                };
            }
            else
            {
                return new PointF();
            }
        }

        public static ColorSpacePoint ToColorSpacePoint(this CameraSpacePoint point, CoordinateMapper mapper)
        {
            return mapper.MapCameraPointToColorSpace(point);
        }

        public static double DistanceToCamera(this Body value)
        {
            Joint headJoint = value.Joints[JointType.Head];
            if (headJoint.IsTracked())
            {
                return headJoint.Position.DistanceToCamera();
            }
            else
            {
                return short.MaxValue;
            }
        }
        
        public static List<Body> GetTrackedBodies(this Body[] bodies)
        {
            return KinectExtensions.GetTrackedBodies(bodies, DefaultMaxTrackedDistanceMM);
        }

        public static List<Body> GetTrackedBodies(this Body[] bodies, short maxDistanceMM)
        {
            //First filter to the bodies which we want to engage
            List<Body> result = new List<Body>();
            for (int i = 0; i < bodies.Length; i++)
            {
                Body currentBody = bodies[i];
                if (currentBody.IsTracked)
                {
                    Joint headJoint = currentBody.Joints[JointType.Head];

                    if (headJoint.IsTracked() && headJoint.Position.DistanceToCamera() <= maxDistanceMM)
                        result.Add(currentBody);
                }
            }

            //Now do insertion sort to really quickly sort by closest 
            for (int i = 1; i < result.Count; i++)
            {
                Body temp = result[i];
                int j = i;
                while (j > 0 && result[j - 1].DistanceToCamera() > temp.DistanceToCamera())
                {
                    result[j] = result[j - 1];
                    j--;
                }
                result[j] = temp;
            }
            
            return result;
        }
    }
}
