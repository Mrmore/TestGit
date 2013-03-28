using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Animation;

namespace RenrenWin8RadioUI.Helper.Animation
{
    public abstract class AnimationBase
    {
        // Fields
        protected Storyboard _Storyboard;
        protected Action<FrameworkElement> AnimationCompleted;
        protected FrameworkElement AnimationTarget;

        // Methods
        public virtual void Stop()
        {
            this._Storyboard.Stop();
        }
    }
}
