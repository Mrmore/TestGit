using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.TimedText;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.Foundation;
#endif

namespace Microsoft.PlayerFramework.TimedText
{
#if MEF
    [System.ComponentModel.Composition.PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [System.ComponentModel.Composition.Export(typeof(IPlugin))]
#endif
    /// <summary>
    /// A player framework plugin capable of displaying timed text captions.
    /// </summary>
    public partial class CaptionsPlugin : PluginBase
    {
        TimedTextCaptions captionsPanel;
        Panel captionsContainer;
        DispatcherTimer timer;

        /// <summary>
        /// Creates a new instance of the CaptionsPlugin
        /// </summary>
        public CaptionsPlugin()
        {
            PollingInterval = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Gets or sets the amount of time to check the server for updated data. Only applies when MediaPlayer.IsLive = true
        /// </summary>
        public TimeSpan PollingInterval { get; set; }

        /// <summary>
        /// Gets whether the captions panel is visible. Returns true if any captions were found.
        /// </summary>
        public bool IsSourceLoaded { get; private set; }

        /// <summary>
        /// Gets or sets the style to be used for the TimedTextCaptions
        /// </summary>
        public Style TimedTextCaptionsStyle { get; set; }

        void MediaPlayer_SelectedCaptionChanged(object sender, RoutedPropertyChangedEventArgs<PlayerFramework.Caption> e)
        {
            if (e.OldValue != null)
            {
                e.OldValue.PayloadChanged -= caption_PayloadChanged;
            }
            MediaPlayer.IsCaptionsActive = (e.NewValue as Caption != null);
            UpdateCaption(e.NewValue as Caption);
            if (e.NewValue != null)
            {
                e.NewValue.PayloadChanged += caption_PayloadChanged;
            }
        }

        void MediaPlayer_PositionChanged(object sender, RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            if (MediaPlayer.SelectedCaption != null)
            {
                captionsPanel.UpdateCaptions(MediaPlayer.Position);
            }
        }

        void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            captionsPanel.NaturalVideoSize = new Size(MediaPlayer.NaturalVideoWidth, MediaPlayer.NaturalVideoHeight);
        }

        void MediaPlayer_IsLiveChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            if (MediaPlayer.IsLive)
            {
                InitializeTimer();
            }
            else
            {
                ShutdownTimer();
            }
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = PollingInterval;
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void ShutdownTimer()
        {
            if (timer != null)
            {
                timer.Tick -= timer_Tick;
                if (timer.IsEnabled) timer.Stop();
                timer = null;
            }
        }

        void timer_Tick(object sender, object e)
        {
            var caption = MediaPlayer.SelectedCaption as Caption;
            RefreshCaption(caption, false);
        }

        /// <inheritdoc /> 
        protected override bool OnActivate()
        {
            var mediaContainer = MediaPlayer.Containers.OfType<Panel>().FirstOrDefault(c => c.Name == MediaPlayerTemplateParts.MediaContainer);
            captionsContainer = mediaContainer.Children.OfType<Panel>().FirstOrDefault(c => c.Name == MediaPlayerTemplateParts.CaptionsContainer);
            if (captionsContainer != null)
            {
                //PoC: to use a custom marker manager
                //captionsPanel = new TimedTextCaptions(new MarkerManager<CaptionRegion>(MediaPlayer), m => new MarkerManager<TimedTextElement>(MediaPlayer) { Markers = m });
                captionsPanel = new TimedTextCaptions();
                captionsPanel.NaturalVideoSize = new Size(MediaPlayer.NaturalVideoWidth, MediaPlayer.NaturalVideoHeight);
                captionsPanel.Style = TimedTextCaptionsStyle;
                MediaPlayer.IsCaptionsActive = (MediaPlayer.SelectedCaption as Caption != null);
                captionsContainer.Children.Add(captionsPanel);
                UpdateCaption(MediaPlayer.SelectedCaption as Caption);

                MediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
                MediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
                MediaPlayer.SelectedCaptionChanged += MediaPlayer_SelectedCaptionChanged;
                MediaPlayer.IsLiveChanged += MediaPlayer_IsLiveChanged;
                if (MediaPlayer.IsLive) InitializeTimer();

                return true;
            }
            return false;
        }

        /// <inheritdoc /> 
        protected override void OnDeactivate()
        {
            MediaPlayer.MediaOpened -= MediaPlayer_MediaOpened;
            MediaPlayer.PositionChanged -= MediaPlayer_PositionChanged;
            MediaPlayer.SelectedCaptionChanged -= MediaPlayer_SelectedCaptionChanged;
            MediaPlayer.IsLiveChanged -= MediaPlayer_IsLiveChanged;
            MediaPlayer.IsCaptionsActive = false;
            captionsContainer.Children.Remove(captionsPanel);
            captionsContainer = null;
            captionsPanel.Clear();
            captionsPanel = null;
            IsSourceLoaded = false;
        }

        /// <summary>
        /// Updates the current caption track.
        /// Will cause the caption source to download and get parsed, and will will start playing.
        /// </summary>
        /// <param name="caption">The caption track to use.</param>
        public void UpdateCaption(Caption caption)
        {
            captionsPanel.Clear();
            RefreshCaption(caption, true);
        }

        void caption_PayloadChanged(object sender, EventArgs e)
        {
            RefreshCaption(sender as Caption, false);
        }

        private async void RefreshCaption(Caption caption, bool forceRefresh)
        {
            if (caption != null)
            {
                string result = null;
                if (caption.Payload is Uri)
                {
                    try
                    {
                        result = await ((Uri)caption.Payload).LoadToString();
                    }
                    catch
                    {
                        // TODO: expose event to log errors
                        return;
                    }
                }
                else if (caption.Payload is string)
                {
                    result = (string)caption.Payload;
                }

                if (result != null)
                {
                    if (Convert.ToInt32(result[0]) == 65279)
                    {
                        result = result.Substring(1, result.Length - 1);
                    }

                    allTasks = EnqueueTask(() => captionsPanel.ParseTtml(result, forceRefresh), allTasks);
                    await allTasks;
                    IsSourceLoaded = true;

                    // refresh the caption based on the current position. Fixes issue where caption is changed while paused.
                    if (IsLoaded) // make sure we didn't get unloaded by the time this completed.
                    {
                        captionsPanel.UpdateCaptions(MediaPlayer.Position);
                    }
                }
            }
        }

        Task allTasks;
        static async Task EnqueueTask(Func<Task> newTask, Task taskQueue)
        {
            if (taskQueue != null) await taskQueue;
            await newTask();
        }
    }
}
