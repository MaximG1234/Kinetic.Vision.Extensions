using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace Microsoft.Kinect.Face
{
    public static class KinectFaceExtensions
    {
        public static bool ValidateFaceBoxAndPoints(this FaceFrameResult faceResult, int displayWidth, int displayHeight)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= displayWidth &&
                                  faceBox.Bottom <= displayHeight;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < displayWidth &&
                                                        pointF.Y < displayHeight;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
        }


        public static RectF Offset(this RectI rect, float widthOffsetPercent, float topOffsetPercent, float bottomOffsetPercent, float maxWidth, float maxHeight)
        {
            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            float widthDifference = ((width / 100f) * widthOffsetPercent) * 100;

            float offsetTop = rect.Top - ((height / 100f) * topOffsetPercent) * 100;
            float offsetLeft = rect.Left - (widthDifference / 2);
            float offsetWidth = width + widthDifference;
            float offsetHeight = height + (((height / 100f) * bottomOffsetPercent) * 100);

            return new RectF()
            {
                Y = offsetTop > maxHeight ? maxHeight : offsetTop,
                X = offsetLeft > maxWidth ? maxWidth : offsetLeft,
                Height = offsetTop + offsetHeight > maxHeight ? maxHeight - offsetTop : offsetHeight,
                Width = offsetLeft + offsetWidth > maxWidth ? maxWidth - offsetLeft : offsetWidth
            };
        }
    }
}
