using System;
using System.Threading.Tasks;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Media;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
#endif

namespace Microsoft.PlayerFramework
{
    public partial class MediaPlayer
    {
        public event EventHandler Disconnected;
        public event EventHandler Reconnected;

#if SILVERLIGHT
        IMediaElement internalMediaElement;
#else
        MediaElement internalMediaElement;
#endif

        public MediaPlayerState GetState()
        {
            var result = new MediaPlayerState();
            result.Source = mediaElement.Source;
            result.Position = mediaElement.Position;
            result.IsPaused = mediaElement.CurrentState == MediaElementState.Paused;
            return result;
        }

        public void Disconnect()
        {
            internalMediaElement = MediaElementElement;
            MediaElementElement = null;
            internalMediaElement.Source = null;
            UpdateTimer.Tick -= UpdateTimer_Tick;
            if (Disconnected != null) Disconnected(this, EventArgs.Empty);
        }

        public async Task Reconnect(MediaPlayerState state)
        {
            if (internalMediaElement == null)
            {
#if SILVERLIGHT
                internalMediaElement = new MediaElementWrapper();
#else
                internalMediaElement = new MediaElement();
#endif
            }

            if (state != null)
            {
                // open media and wait
                mediaOpenTask = new TaskCompletionSource<bool>();
#if WINDOWS_PHONE
                internalMediaElement.AutoPlay = !state.IsPaused;
                internalMediaElement.CurrentStateChanged += internalMediaElement_CurrentStateChanged;
#else
                internalMediaElement.MediaOpened += internalMediaElement_MediaOpened;
#endif
                internalMediaElement.MediaFailed += internalMediaElement_MediaFailed;

                internalMediaElement.Source = state.Source;
                // TODO: surface failures through the MediaFailed event
                var result = await mediaOpenTask.Task;
#if WINDOWS_PHONE
                internalMediaElement.CurrentStateChanged -= internalMediaElement_CurrentStateChanged;
#else
                internalMediaElement.MediaOpened -= internalMediaElement_MediaOpened;
#endif
                internalMediaElement.MediaFailed -= internalMediaElement_MediaFailed;
                mediaOpenTask = null;

                internalMediaElement.Position = state.Position;
                if (!state.IsPaused)
                {
#if WINDOWS_PHONE
                    internalMediaElement.AutoPlay = false; // reset
#else
                    internalMediaElement.Play();
#endif
                }
            }

            MediaElementElement = internalMediaElement;
            internalMediaElement = null;
            UpdateTimer.Tick += UpdateTimer_Tick;
            if (Reconnected != null) Reconnected(this, EventArgs.Empty);
        }

#if WINDOWS_PHONE
        void internalMediaElement_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (internalMediaElement.CurrentState)
            {
                case MediaElementState.Buffering:
                case MediaElementState.Playing:
                case MediaElementState.Paused:
                    mediaOpenTask.TrySetResult(true);
                    break;
            }
        }
#else
        void internalMediaElement_MediaOpened(object sender, RoutedEventArgs e)
        {
            mediaOpenTask.SetResult(true);
        }
#endif

        TaskCompletionSource<bool> mediaOpenTask;
        void internalMediaElement_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
#if SILVERLIGHT
            mediaOpenTask.TrySetException(e.ErrorException);
#else
            mediaOpenTask.TrySetException(new Exception(e.ErrorMessage));
#endif
        }

    }

    public class MediaPlayerState
    {
        public bool IsPaused { get; set; }
        public Uri Source { get; set; }
        public TimeSpan Position { get; set; }
    }
}
