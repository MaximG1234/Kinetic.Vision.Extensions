using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace System.Windows
{
    public static class FrameworkElementExtensions
    {
        public static void Show(this FrameworkElement element)
        {
            if (element.Visibility != Visibility.Visible)
                element.Visibility = Visibility.Visible;

            if (element.Opacity != 1)
                element.Opacity = 1;
        }

        public static void Hide(this FrameworkElement element)
        {
            if (element.Visibility != Visibility.Hidden)
                element.Visibility = Visibility.Hidden;
        }

        public static void Collapse(this FrameworkElement element)
        {
            if (element.Visibility != Visibility.Collapsed)
                element.Visibility = Visibility.Collapsed;
        }

        public static void ToggleVisibility(this FrameworkElement element)
        {
            if (element.Visibility == Visibility.Visible)
                FrameworkElementExtensions.Hide(element);
            else if (element.Visibility == Visibility.Collapsed || element.Visibility == Visibility.Hidden)
                FrameworkElementExtensions.Show(element);
        }

        public static void FadeElementOut(this FrameworkElement element)
        {
            FadeElementOut(element, 1000, null);
        }

        public static void FadeElementOut(this FrameworkElement element, Action onComplete)
        {
            FadeElementOut(element, 1000, onComplete);
        }

        public static void FadeElementOut(this FrameworkElement element, int durationMS, Action onComplete)
        {
            if (element.Visibility != Visibility.Hidden)
            {
                DoubleAnimation fadingAnimation = new DoubleAnimation();
                fadingAnimation.From = 1;
                fadingAnimation.To = 0;
                fadingAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(durationMS));
                //fadingAnimation.AutoReverse = true;

                fadingAnimation.Completed += (s, e) =>
                {
                    if (element.Visibility == Visibility.Visible)
                        element.Visibility = Visibility.Collapsed;

                    if (onComplete != null)
                        onComplete();
                };


                element.BeginAnimation(TextBlock.OpacityProperty, fadingAnimation);
            }
            else
            {
                if (onComplete != null)
                    onComplete();
            }
        }

        public static void FadeElementIn(this FrameworkElement element)
        {
            FadeElementIn(element, 1000, null);
        }

        public static void FadeElementIn(this FrameworkElement element, Action onComplete)
        {
            FadeElementIn(element, 1000, onComplete);
        }

        public static void FadeElementIn(this FrameworkElement element, int durationMS, Action onComplete)
        {
            if (element.Visibility == Visibility.Hidden || element.Visibility == Visibility.Collapsed)
            {
                if (element.Visibility != Visibility.Visible)
                {
                    element.Opacity = 0;
                    element.Visibility = Visibility.Visible;
                }

                DoubleAnimation fadingAnimation = new DoubleAnimation();
                fadingAnimation.From = 0;
                fadingAnimation.To = 1;
                fadingAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(durationMS));
                //fadingAnimation.AutoReverse = true;
                fadingAnimation.Completed += (s, e) =>
                {
                    if (onComplete != null)
                        onComplete();
                };
                
                element.BeginAnimation(TextBlock.OpacityProperty, fadingAnimation);
            }
            else
            {
                if (onComplete != null)
                    onComplete();
            }
        }

        public static void FadeElementTo(this FrameworkElement element, int durationMS, double opacity)
        {
            FadeElementTo(element, durationMS, opacity, null);
        }

        public static void FadeElementTo(this FrameworkElement element, int durationMS, double opacity, Action onComplete)
        {
            if (element.Visibility != Visibility.Visible)
                element.Visibility = Visibility.Visible;

            DoubleAnimation fadingAnimation = new DoubleAnimation();
            fadingAnimation.From = element.Opacity;
            fadingAnimation.To = opacity;
            fadingAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(durationMS));
            //fadingAnimation.AutoReverse = true;
            fadingAnimation.Completed += (s, e) =>
            {
                if (onComplete != null)
                    onComplete();
            };

            element.BeginAnimation(TextBlock.OpacityProperty, fadingAnimation);
        }

        public static void BounceEasingTop(this FrameworkElement element, double to)
        {
            BounceEasing(element, Canvas.TopProperty, to);
        }

        public static void BounceEasingLeft(this FrameworkElement element, double to)
        {
            BounceEasing(element, Canvas.LeftProperty, to);
        }

        public static void BounceEasing(this FrameworkElement element, DependencyProperty easingProperty, double to)
        {
            DoubleAnimation easingAnimation = new DoubleAnimation()
            {
                From = 0,
                To = to,
                Duration = TimeSpan.FromMilliseconds(400),
                //EasingFunction = new BounceEase() 
                //{ 
                //    Bounces = 2, 
                //    EasingMode = EasingMode.EaseIn, 
                //    Bounciness = 2 
                //}
            };

            easingAnimation.EasingFunction = new PowerEase() { EasingMode = EasingMode.EaseIn, Power = 5 };

            element.BeginAnimation(easingProperty, easingAnimation);
           
        }

        public static double SolveAspectHeight(this FrameworkElement element, double newWidth)
        {
            return Kinetic.Vision.Utilities.AspectRatioUtilities.SolveAspect(newWidth, null, element.ActualWidth, element.ActualHeight);
        }

        public static double SolveAspectWidth(this FrameworkElement element, double newHeight)
        {
            return Kinetic.Vision.Utilities.AspectRatioUtilities.SolveAspect(null, newHeight, element.ActualWidth, element.ActualHeight);
        }
    }
}
