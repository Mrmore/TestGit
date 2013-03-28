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
    public class MoveAnimation : AnimationBase
    {
        // Fields
        private DoubleAnimationUsingKeyFrames _Animation_X;
        private DoubleAnimationUsingKeyFrames _Animation_Y;
        private EasingDoubleKeyFrame _KeyFrame_x_from;
        private EasingDoubleKeyFrame _KeyFrame_x_to;
        private EasingDoubleKeyFrame _KeyFrame_y_from;
        private EasingDoubleKeyFrame _KeyFrame_y_to;
        private static Stack<MoveAnimation> AnimationPool = new Stack<MoveAnimation>();
        private double TargetX;
        private double TargetY;

        // Methods
        public MoveAnimation()
        {
            this.Init();
        }

        private void _Storyboard_Completed(object sender, object e)
        {
            base.AnimationTarget.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (double)this.TargetX);
            base.AnimationTarget.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (double)this.TargetY);
            if (base.AnimationCompleted != null)
            {
                base.AnimationCompleted(base.AnimationTarget);
            }
            AnimationPool.Push(this);
        }

        private void Animate(FrameworkElement cell, double x, double y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            base.AnimationTarget = cell;
            this.TargetX = x;
            this.TargetY = y;
            base.AnimationCompleted = completed;
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
            this._KeyFrame_x_from.Value = transform.TranslateX;
            this._KeyFrame_x_to.Value = x;
            this._KeyFrame_y_from.Value = transform.TranslateY;
            this._KeyFrame_y_to.Value = y;
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
            this._KeyFrame_x_to.KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0));
            this._KeyFrame_x_to.Value = 1.0;
            this._Animation_X.KeyFrames.Add(this._KeyFrame_x_to);
            Storyboard.SetTargetProperty(this._Animation_X, "(UIElement.RenderTransform).(CompositeTransform.TranslateX)");
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
            Storyboard.SetTargetProperty(this._Animation_Y, "(UIElement.RenderTransform).(CompositeTransform.TranslateY)");
            base._Storyboard.Children.Add(this._Animation_Y);
        }

        public void InstanceMoveBy(FrameworkElement cell, double x, double y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            CompositeTransform transform = cell.RenderTransform as CompositeTransform;
            double num2 = transform.TranslateX + x;
            double num4 = transform.TranslateY + y;
            this.InstanceMoveTo(cell, num2, num4, duration, completed);
        }

        public void InstanceMoveFromTo(FrameworkElement cell, double from_x, double from_y, double to_x, double to_y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            cell.RenderTransform.SetValue(CompositeTransform.TranslateXProperty, (double)from_x);
            cell.RenderTransform.SetValue(CompositeTransform.TranslateYProperty, (double)from_y);
            this.InstanceMoveTo(cell, to_x, to_y, duration, completed);
        }

        public void InstanceMoveTo(FrameworkElement cell, double x, double y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            this.Animate(cell, x, y, duration, completed);
        }

        public static MoveAnimation MoveBy(FrameworkElement cell, double x, double y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            MoveAnimation animation = null;
            if (AnimationPool.Count == 0)
            {
                animation = new MoveAnimation();
            }
            else
            {
                animation = AnimationPool.Pop();
            }
            animation.InstanceMoveBy(cell, x, y, duration, completed);
            return animation;
        }

        public static MoveAnimation MoveFromTo(FrameworkElement cell, double from_x, double from_y, double to_x, double to_y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            MoveAnimation animation = null;
            if (AnimationPool.Count == 0)
            {
                animation = new MoveAnimation();
            }
            else
            {
                animation = AnimationPool.Pop();
            }
            animation.InstanceMoveFromTo(cell, from_x, from_y, to_x, to_y, duration, completed);
            return animation;
        }

        public static MoveAnimation MoveTo(FrameworkElement cell, double x, double y, TimeSpan duration, Action<FrameworkElement> completed)
        {
            MoveAnimation animation = null;
            if (AnimationPool.Count == 0)
            {
                animation = new MoveAnimation();
            }
            else
            {
                animation = AnimationPool.Pop();
            }
            animation.InstanceMoveTo(cell, x, y, duration, completed);
            return animation;
        }

        public static MoveAnimation PickupAnimationNonPooling()
        {
            return new MoveAnimation();
        }
    }


}
