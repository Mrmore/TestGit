﻿using System;

namespace Microsoft.VideoAdvertising
{
    /// <summary>
    /// Provides information about an event related to a MAST trigger
    /// </summary>
    public sealed class TriggerEventArgs 
#if SILVERLIGHT
        : EventArgs
#endif
    {
        internal TriggerEventArgs(Trigger trigger)
        {
            Trigger = trigger;
        }

        public Trigger Trigger { get; private set; }
    }

    /// <summary>
    /// Provides information about a failure involving a specific MAST trigger
    /// </summary>
    public sealed class TriggerFailureEventArgs
#if SILVERLIGHT
        : EventArgs
#endif
    {
        internal TriggerFailureEventArgs(Trigger trigger, Exception exception)
        {
            Exception = exception;
            Trigger = trigger;
        }

        public Trigger Trigger { get; private set; }

        public Exception Exception { get; private set; }
    }
}
