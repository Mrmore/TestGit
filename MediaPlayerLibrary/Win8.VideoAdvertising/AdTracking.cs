using System;
using System.Net.Http;

namespace Microsoft.VideoAdvertising
{
    public sealed class AdTracking
    {
        static AdTracking current;
        public static AdTracking Current
        {
            get
            {
                if (current == null) current = new AdTracking();
                return current;
            }
        }

        public event EventHandler<TrackingFailureEventArgs> TrackingFailed;

#if !SILVERLIGHT
        [Windows.Foundation.Metadata.DefaultOverload()]
#endif
        public void FireTracking(string trackingUrl)
        {
            try
            {
                var uri = new Uri(trackingUrl);
                FireTracking(uri);
            }
            catch (Exception ex)
            {
                if (TrackingFailed != null) TrackingFailed(this, new TrackingFailureEventArgs(trackingUrl, ex));
            }
        }

        public async void FireTracking(Uri trackingUri)
        {
            if (trackingUri != null)
            {
                try
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(trackingUri);
#endif
                    await Extensions.DownloadStreamAsync(trackingUri);
                }
                catch (Exception ex)
                {
                    if (TrackingFailed != null) TrackingFailed(this, new TrackingFailureEventArgs(trackingUri.OriginalString, ex));
                }
            }
        }
    }

    /// <summary>
    /// Provides additional information about a tracking failure event.
    /// </summary>
    public sealed class TrackingFailureEventArgs
#if SILVERLIGHT
        : EventArgs
#endif
    {
        public TrackingFailureEventArgs(string url, Exception error)
        {
            Error = error;
            Url = url;
        }

        /// <summary>
        /// The tracking url that failed.
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>
        /// The Url that was unable to be tracked.
        /// </summary>
        public string Url { get; private set; }
    }
}
