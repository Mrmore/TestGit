using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace RenrenWin8RadioUI.Helper.Animation
{
    public class ScaleAnimation : AnimationBase
    {
        // Fields
        private DoubleAnimationUsingKeyFrames _Animation_X;
        private DoubleAnimationUsingKeyFrames _Animation_Y;
        private EasingDoubleKeyFrame _KeyFrame_x_from;
        private EasingDoubleKeyFrame _KeyFrame_x_to;
        private EasingDoubleKeyFrame _KeyFrame_y_from;
        private EasingDoubleKeyFrame _KeyFrame_y_to;
        private static Stack<ScaleAnimation> AnimationPool = new Stack<ScaleAnimation>();
        private double TargetX;
        private double TargetY;

        // Methods
        public ScaleAnimation()
        {
            this.Init();
        }

        private void _Storyboard_Completed(object sender, object e)
        {
            base.AnimationTarget.RenderTransform.SetValue(CompositeTransform.ScaleXProperty, (double)this.TargetX);
            base.AnimationTarget.RenderTransform.SetValue(CompositeTransform.ScaleYProperty, (double)this.TargetY);
            if (base.AnimationCompleted != null)
            {
                base.AnimationCompleted(base.AnimationTarget);
            }
            AnimationPool.Push(this);
        }

        private void Animate(FrameworkElement cell, TimeSpan duration, double targetX, double targetY, Action<FrameworkElement> completed)
        {
            base.AnimationTarget = cell;
            base.AnimationCompleted = completed;
            this.TargetX = targetX;
            this.TargetY = targetY;
            if (base._Storyboard == null)
            {
                this.Init();
            }
            else
            {
                base._Storyboard.Stop();
            }
            this._KeyFrame_x_to.KeyTime = KeyTime.FromTimeSpan(duration);
            this._KeyFrame_y_to.KeyTime = KeyTime.FromTimeSpan(duration);
            CompositeTransform transform = cell.RenderTransform as CompositeTransform;
            this._KeyFrame_x_from.Value  = transform.ScaleX;
            this._KeyFrame_x_to.Value = targetX;
            this._KeyFrame_y_from.Value = transform.ScaleY;
            this._KeyFrame_y_to.Value = targetY;
            Storyboard.SetTarget(this._Animation_X, base.AnimationTarget);
            Storyboard.SetTarget(this._Animation_Y, base.AnimationTarget);
            base._Storyboard.Begin();
        }

        private void Init()
        {
            base._Storyboard = new Storyboard();
            Storyboard storyboard = base._Storyboard;

            storyboard.Completed -= _Storyboard_Completed;
            storyboard.Completed += _Storyboard_Completed;

            this._Animation_X = new DoubleAnimationUsingKeyFrames();
            this._KeyFrame_x_from = new EasingDoubleKeyFrame();
            this._KeyFrame_x_from.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0));
            this._KeyFrame_x_from.Value = 0.0;
            this._Animation_X.KeyFrames.Add(this._KeyFrame_x_from);
            this._KeyFrame_x_to = new EasingDoubleKeyFrame();
            this._KeyFrame_x_to.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0));
            this._KeyFrame_x_to.Value = 1.0;
            this._Animation_X.KeyFrames.Add(this._KeyFrame_x_to);
            Storyboard.SetTargetProperty(this._Animation_X, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
            base._Storyboard.Children.Add(this._Animation_X);
            this._Animation_Y = new DoubleAnimationUsingKeyFrames();
            this._KeyFrame_y_from = new EasingDoubleKeyFrame();
            this._KeyFrame_y_from.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0));
            this._KeyFrame_y_from.Value = 0.0;
            this._Animation_Y.KeyFrames.Add(this._KeyFrame_y_from);
            this._KeyFrame_y_to = new EasingDoubleKeyFrame();
            this._KeyFrame_y_to.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0));
            this._KeyFrame_y_to.Value = 1.0;
            this._Animation_Y.KeyFrames.Add(this._KeyFrame_y_to);
            Storyboard.SetTargetProperty(this._Animation_Y, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");
            base._Storyboard.Children.Add(this._Animation_Y);
        }

        public void InstanceScaleFromTo(FrameworkElement cell, double from_x, double from_y, double to_x, double to_y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            cell.RenderTransform.SetValue(CompositeTransform.ScaleXProperty, (double)from_x);
            cell.RenderTransform.SetValue(CompositeTransform.ScaleYProperty, (double)from_y);
            this.InstanceScaleTo(cell, to_x, to_y, duration, completed);
        }

        public void InstanceScaleTo(FrameworkElement cell, double targetX, double targetY, TimeSpan duration, Action<FrameworkElement> completed)
        {
            this.Animate(cell, duration, targetX, targetY, completed);
        }

        public static ScaleAnimation ScaleFromTo(FrameworkElement cell, double from_x, double from_y, double to_x, double to_y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            ScaleAnimation animation = null;
            if (AnimationPool.Count == 0)
            {
                animation = new ScaleAnimation();
            }
            else
            {
                animation = AnimationPool.Pop();
            }
            animation.InstanceScaleFromTo(cell, from_x, from_y, to_x, to_y, duration, completed);
            return animation;
        }

        public static ScaleAnimation ScaleTo(FrameworkElement cell, double targetX, double targetY, TimeSpan duration, Action<FrameworkElement> completed)
        {
            ScaleAnimation animation = null;
            if (AnimationPool.Count == 0)
            {
                animation = new ScaleAnimation();
            }
            else
            {
                animation = AnimationPool.Pop();
            }
            animation.InstanceScaleTo(cell, targetX, targetY, duration, completed);
            return animation;
        }
    }


}
