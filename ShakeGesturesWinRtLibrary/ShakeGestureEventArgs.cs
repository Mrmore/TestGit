using System;
using System.Net;
using System.Windows;
using System.Windows.Input;

namespace ShakeGestures
{
    public class ShakeGestureEventArgs : EventArgs
    {
        private ShakeType _shakeType;

        public ShakeGestureEventArgs(ShakeType shakeType)
        {
            _shakeType = shakeType;
        }

        public ShakeType ShakeType
        {
            get
            {
                return _shakeType;
            }
        }
    }
}
