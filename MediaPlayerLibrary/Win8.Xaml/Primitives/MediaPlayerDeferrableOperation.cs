using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// Provides data for a deferrable event.
    /// </summary>
    public class MediaPlayerDeferrableEventArgs : EventArgs
    {
        internal MediaPlayerDeferrableEventArgs(MediaPlayerDeferrableOperation deferrableOperation)
        {
            DeferrableOperation = deferrableOperation;
        }

        /// <summary>
        /// Gets the deferrable operation.
        /// </summary>
        public MediaPlayerDeferrableOperation DeferrableOperation { get; private set; }
    }

    /// <summary>
    /// Provides info about a deferrable operation.
    /// </summary>
    public class MediaPlayerDeferrableOperation
    {
        readonly CancellationTokenSource cts;
        readonly List<MediaPlayerDeferral> deferrals;

        internal MediaPlayerDeferrableOperation(CancellationTokenSource cancellationTokenSource)
        {
            cts = cancellationTokenSource;
            deferrals = new List<MediaPlayerDeferral>();
        }

        internal bool DeferralsExist
        {
            get
            {
                return deferrals.Any();
            }
        }

        internal Task<bool[]> Task
        {
            get
            {
#if SILVERLIGHT && !WINDOWS_PHONE
                return System.Threading.Tasks.TaskEx.WhenAll(deferrals.Select(d => d.Task));
#else
                return System.Threading.Tasks.Task.WhenAll(deferrals.Select(d => d.Task));
#endif
            }
        }

        internal void Cancel()
        {
            cts.Cancel();
        }

        /// <summary>
        /// Requests that the deferrable operation be delayed.
        /// </summary>
        /// <returns>The deferral.</returns>
        public MediaPlayerDeferral GetDeferral()
        {
            var result = new MediaPlayerDeferral(cts.Token);
            deferrals.Add(result);
            return result;
        }
    }

    /// <summary>
    /// Manages a delayed app deferrable operation.
    /// </summary>
    public class MediaPlayerDeferral
    {
        readonly TaskCompletionSource<bool> tcs;

        internal MediaPlayerDeferral(CancellationToken cancellationToken)
        {
            tcs = new TaskCompletionSource<bool>();
            CancellationToken = cancellationToken;
        }

        internal Task<bool> Task
        {
            get
            {
                return tcs.Task;
            }
        }

        /// <summary>
        /// Gets the CancellationToken associated with this class.
        /// </summary>
        public CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Notifies the MediaPlayer that the operation is complete.
        /// </summary>
        public void Complete()
        {
            tcs.TrySetResult(true);
        }

        /// <summary>
        /// Notifies the MediaPlayer that the operation should be cancelled.
        /// </summary>
        public void Cancel()
        {
            tcs.TrySetResult(false);
        }
    }
}
