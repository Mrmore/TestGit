﻿#define CODE_ANALYSIS

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
#if MEF
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
#endif
#if SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using System.Windows.Data;
using System.Linq.Expressions;
using System.Windows.Input;
using System.Resources;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using System.Reflection;
using Windows.Media.PlayTo;
using Windows.Media.Protection;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Data;
using Windows.ApplicationModel.Resources;
using Windows.UI.Core;
using Windows.ApplicationModel.Resources.Core;
using Windows.ApplicationModel;
using Windows.Storage.Streams;
using Windows.Media;
#endif

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// Represents a media player used to play video or audio and optionally allow the user interact.
    /// This is the primary class in the Microsoft Media Platform Player Framework.
    /// This player offers a super-set of the MediaElement API.
    /// Optional plugins can be automatically detected to help extend or modify the default behavior.
    /// </summary>
    public partial class MediaPlayer : Control, IMediaSource, IDisposable
    {
        /// <summary>
        /// Instantiates a new instance of the MediaPlayer class.
        /// </summary>
        public MediaPlayer()
        {
            this.DefaultStyleKey = typeof(MediaPlayer);
            cts = new CancellationTokenSource();
            AllowMediaStartingDeferrals = true;
            AutoLoadPlugins = true;
            SetValueWithoutCallback(VisualMarkersProperty, new ObservableCollection<VisualMarker>());
            SetValueWithoutCallback(AvailableCaptionsProperty, new List<Caption>());
            SetValueWithoutCallback(AvailableAudioStreamsProperty, new List<AudioStream>());
            SetValueWithoutCallback(TimeFormatConverterProperty, new StringFormatConverter() { StringFormat = DefaultTimeFormat });
            LoadPlugins(new ObservableCollection<IPlugin>());
            UpdateTimer.Interval = UpdateInterval;
            UpdateTimer.Tick += UpdateTimer_Tick;
            InitializeTemplateDefinitions();
        }

        partial void InitializeTemplateDefinitions();

        /// <summary>
        /// Provides a cancellation token for all async operations that should be cancelled during Dispose
        /// </summary>
        private CancellationTokenSource cts;

        /// <summary>
        /// The timer used to update the position and other frequently changing properties.
        /// </summary>
        private readonly DispatcherTimer UpdateTimer = new DispatcherTimer();

        /// <summary>
        /// Used to seek on MediaOpened to a new position.
        /// </summary>
        private TimeSpan? mediaRestorePosition;

        /// <summary>
        /// Indicates the playback rate that should be set after scrubbing.
        /// </summary>
        private double rateAfterScrub;

        /// <summary>
        /// Remembers the scrub start position to be relayed in future scrub events.
        /// </summary>
        private TimeSpan startScrubPosition;

#if !SILVERLIGHT
        private MediaExtensionManager mediaExtensionManager;
#endif

        private bool isMediaLoaded;
        /// <summary>
        /// Indicates whether or not the media is actually set on the underlying MediaElement.
        /// </summary>
        protected bool IsMediaLoaded
        {
            get { return isMediaLoaded; }
            private set
            {
                isMediaLoaded = value;
                if (isMediaLoaded)
                {
                    SetValue(PlayerStateProperty, PlayerState.Loaded);
                }
                else
                {
                    SetValue(PlayerStateProperty, PlayerState.Unloaded);
                }
            }
        }

        /// <summary>
        /// Indicates the state has changed but filters out changes when the state changes to buffering. 
        /// Buffering is a special case that makes it hard to determine if the video is actually playing, loading or paused.
        /// </summary>
        event RoutedEventHandler CurrentStateChangedBufferingIgnored;

        Action pendingLoadAction;
        /// <summary>
        /// Holds the action to set the source so we can delay things
        /// </summary>
        protected Action PendingLoadAction
        {
            get { return pendingLoadAction; }
            set
            {
                pendingLoadAction = value;
                if (pendingLoadAction != null)
                {
                    SetValue(PlayerStateProperty, PlayerState.Pending);
                }
            }
        }

        /// <inheritdoc /> 
#if SILVERLIGHT
        public override void OnApplyTemplate()
#elif MEF
        protected async override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            if (IsTemplateApplied)
            {
                UninitializeTemplateChildren();
                DestroyTemplateChildren();
            }

            base.OnApplyTemplate();
            SetDefaultVisualStates();
            GetTemplateChildren();
            InitializeTemplateChildren();

            if (!IsTemplateApplied)
            {
#if SILVERLIGHT || !MEF
                InitializePlugins();
#else
                await InitializePlugins();
#endif
                IsTemplateApplied = true;
                if (Initialized != null) Initialized(this, new RoutedEventArgs());
            }
        }

        #region Plugins

#if SILVERLIGHT || !MEF
        void InitializePlugins()
#else
        async Task InitializePlugins()
#endif
        {
            // initialize any plugins already in the collection
            foreach (var plugin in Plugins.ToList())
            {
                plugin.Load();
            }

            // load more plugins via MEF
            if (AutoLoadPlugins)
            {
                var PluginsManager = new PluginsFactory();
#if SILVERLIGHT || !MEF
                PluginsManager.ImportPlugins();
#else
                await PluginsManager.ImportPlugins();
#endif
                if (PluginsManager.Plugins != null)
                {
                    // we want to load the plugins ourselves instead of in this event. This allows us to add them all before loading the first one.
                    Plugins.CollectionChanged -= Plugins_CollectionChanged;
                    try
                    {
                        foreach (var plugin in PluginsManager.Plugins)
                        {
                            Plugins.Add(plugin);
                        }
                        foreach (var plugin in PluginsManager.Plugins)
                        {
                            plugin.MediaPlayer = this;
                            plugin.Load();
                        }
                    }
                    finally // turn on the event again
                    {
                        Plugins.CollectionChanged += Plugins_CollectionChanged;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether plugins should be automatically discovered. MEF (Managed Extensibility Framework) is used to discover plugins.
        /// Set to false to optimize if you are not using any plugins or if you want to manually set which plugins are connected.
        /// You can programmatically connect plugins by adding to the Plugins collection. Default is true.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Correctly named architectural pattern")]
        [Category(Categories.Advanced)]
        public bool AutoLoadPlugins { get; set; }

        ObservableCollection<IPlugin> plugins;
        /// <summary>
        /// Gets the collection of connected plugins. You can dynamically add a plugin to the collection at any time and it will be appropriately wired when added and unwired when removed.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Correctly named architectural pattern")]
        [Category(Categories.Advanced)]
        public ObservableCollection<IPlugin> Plugins
        {
            get { return plugins; }
        }

        void LoadPlugins(ObservableCollection<IPlugin> value)
        {
            if (Plugins != null)
            {
                Plugins.CollectionChanged -= Plugins_CollectionChanged;
                foreach (var plugin in Plugins)
                {
                    if (IsTemplateApplied) plugin.Unload();
                    plugin.MediaPlayer = null;
                }
            }

            plugins = value;

            if (Plugins != null)
            {
                Plugins.CollectionChanged += Plugins_CollectionChanged;
                foreach (var plugin in Plugins)
                {
                    plugin.MediaPlayer = this;
                    if (IsTemplateApplied) plugin.Load();
                }
            }
        }

        void Plugins_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var plugin in e.NewItems.Cast<IPlugin>())
                {
                    plugin.MediaPlayer = this;
                    if (IsTemplateApplied) plugin.Load();
                }
            }

            if (e.OldItems != null)
            {
                foreach (var plugin in e.OldItems.Cast<IPlugin>())
                {
                    if (IsTemplateApplied) plugin.Unload();
                    plugin.MediaPlayer = null;
                }
            }
        }

        #endregion

        #region Methods

#if SILVERLIGHT
        /// <summary>
        /// Sends a request to generate a log which will then be raised through the LogReady event.
        /// </summary>
        public void RequestLog()
        {
            _RequestLog();
        }

        /// <summary>
        /// This sets the source of the MediaElement to a subclass of System.Windows.Media.MediaStreamSource.
        /// </summary>
        /// <param name="mediaStreamSource">A subclass of System.Windows.Media.MediaStreamSource.</param>
        public void SetSource(MediaStreamSource mediaStreamSource)
        {
            RegisterApplyTemplateAction(async () =>
            {
                if (AutoLoad || mediaStreamSource == null)
                {
                    if (mediaStreamSource == null || await OnMediaLoadingAsync())
                    {
                        if (IsMediaLoaded)
                        {
                            OnMediaClosed(new RoutedEventArgs());
                        }
                        _SetSource(mediaStreamSource);
                        IsMediaLoaded = mediaStreamSource != null;
                    }
                }
                else
                {
                    PendingLoadAction = () => SetSource(mediaStreamSource);
                }
            });
        }

        /// <summary>
        /// Sets the MediaElement.Source property using the supplied stream.
        /// </summary>
        /// <param name="stream">A stream that contains a valid media source.</param>
        public void SetSource(Stream stream)
        {
            RegisterApplyTemplateAction(async () =>
            {
                if (AutoLoad || stream == null)
                {
                    if (stream == null || await OnMediaLoadingAsync())
                    {
                        if (IsMediaLoaded)
                        {
                            OnMediaClosed(new RoutedEventArgs());
                        }
                        _SetSource(stream);
                        IsMediaLoaded = stream != null;
                    }
                }
                else
                {
                    PendingLoadAction = () => SetSource(stream);
                }
            });
        }

        /// <summary>
        /// Changes the display mode (or Stretch property) when called to the next in the list and starts at the beginning if at the end of the list.
        /// The order is Uniform, UniformToFill, Fill, None.
        /// </summary>
        public virtual void CycleDisplayMode()
        {
            switch (Stretch)
            {
                case Stretch.Uniform:
                    Stretch = Stretch.UniformToFill;
                    break;
                case Stretch.UniformToFill:
                    Stretch = Stretch.Fill;
                    break;
                case Stretch.Fill:
                    Stretch = Stretch.None;
                    break;
                case Stretch.None:
                    Stretch = Stretch.Uniform;
                    break;
            }
        }
#else

        /// <summary>
        /// Applies an audio effect to playback. Takes effect for the next source set on the MediaElement.
        /// </summary>
        /// <param name="effectID">The identifier for the desired effect.</param>
        /// <param name="effectOptional">True if the effect should not block playback in cases where the effect cannot be used at run time. False if playback should be blocked in cases where the effect cannot be used at run time.</param>
        /// <param name="effectConfiguration">A property set that transmits property values to specific effects as selected by effectID.</param>
        public void AddAudioEffect(string effectID, bool effectOptional, IPropertySet effectConfiguration)
        {
            _AddAudioEffect(effectID, effectOptional, effectConfiguration);
        }

        /// <summary>
        /// Applies a video effect to playback. Takes effect for the next source set on the MediaElement.
        /// </summary>
        /// <param name="effectID">The identifier for the desired effect.</param>
        /// <param name="effectOptional">True if the effect should not block playback in cases where the effect cannot be used at run time. False if playback should be blocked in cases where the effect cannot be used at run time.</param>
        /// <param name="effectConfiguration">A property set that transmits property values to specific effects as selected by effectID.</param>
        public void AddVideoEffect(string effectID, bool effectOptional, IPropertySet effectConfiguration)
        {
            _AddVideoEffect(effectID, effectOptional, effectConfiguration);
        }

        /// <summary>
        /// Removes all effects for the next source set for this MediaElement.
        /// </summary>
        public void RemoveAllEffects()
        {
            _RemoveAllEffects();
        }

        /// <summary>
        /// Returns an enumeration value that describes the likelihood that the current MediaElement and its client configuration can play that media source.
        /// </summary>
        /// <param name="type">A string that describes the desired type as a MIME string.</param>
        /// <returns>A value of the enumeration that describes the likelihood that the source can be played by the current media engine.</returns>
        public MediaCanPlayResponse CanPlayType(string type)
        {
            return _CanPlayType(type);
        }

        /// <summary>
        /// Gets the language for a given audio stream.
        /// </summary>
        /// <param name="index">The index of the audio stream.</param>
        /// <returns>The language for the audio stream.</returns>
        public string GetAudioStreamLanguage(int? index)
        {
            return _GetAudioStreamLanguage(index);
        }

        /// <summary>
        /// Sets the Source property using the supplied stream.
        /// </summary>
        /// <param name="stream">The stream that contains the media to load.</param>
        /// <param name="mimeType">The MIME type of the media resource, expressed as the string form typically seen in HTTP headers and requests.</param>
        public void SetSource(IRandomAccessStream stream, string mimeType)
        {
            RegisterApplyTemplateAction(async () =>
            {
                if (AutoLoad || stream == null)
                {
                    if (stream == null || await OnMediaLoadingAsync())
                    {
                        if (IsMediaLoaded)
                        {
                            OnMediaClosed(new RoutedEventArgs());
                        }
                        _SetSource(stream, mimeType);
                        IsMediaLoaded = stream != null;
                    }
                }
                else
                {
                    PendingLoadAction = () => SetSource(stream, mimeType);
                }
            });
        }

#endif

        /// <summary>
        /// Decreases the PlaybackRate. When called, PlaybackRate will halve until it reaches 1. Once it reaches 1, it will flip to negative numbers causing the player to rewind.
        /// </summary>
        public virtual void DecreasePlaybackRate()
        {
            if (PlaybackRate > -1 && PlaybackRate <= 1) PlaybackRate = -1;
            else if (PlaybackRate > 1) PlaybackRate /= 2;
            else PlaybackRate *= 2;
        }

        /// <summary>
        /// Increases the PlaybackRate. When called, PlaybackRate will double until it reaches -1. Once it reaches -1, it will flip to positive numbers causing the player to fast forward.
        /// </summary>
        public virtual void IncreasePlaybackRate()
        {
            if (PlaybackRate >= -1 && PlaybackRate < 1) PlaybackRate = 1;
            else if (PlaybackRate < -1) PlaybackRate /= 2;
            else PlaybackRate *= 2;
        }

        /// <summary>
        /// Supports Instant Replay by subtracting the amount of time specified by the ReplayOffset property from the current Position.
        /// </summary>
        public virtual void Replay()
        {
            TimeSpan newPosition = Position.Subtract(ReplayOffset);
            if (newPosition < StartTime)
            {
                newPosition = StartTime;
            }

            Position = newPosition;

            if (CurrentState == MediaElementState.Paused)
            {
                Play();
            }
        }

        /// <summary>
        /// Stops and closes the current media source. Fires MediaClosed.
        /// </summary>
        public virtual void Close()
        {
            Source = null;
        }

        /// <summary>
        /// Retries the media after an error by reopening the media and seeking to the last position
        /// </summary>
        public void Retry()
        {
            if (Position != StartTime)
            {
                mediaRestorePosition = Position;
            }
            PlaybackRate = DefaultPlaybackRate;
            OnRetry();
        }

        /// <summary>
        /// Called when retrying to load a video stream.
        /// Override to handle Stream sources.
        /// </summary>
        protected virtual void OnRetry()
        {
            var failedSource = Source;
            SetSource(null as Uri);
            SetSource(failedSource);
        }

        /// <summary>
        /// Plays the media or resets the PlaybackRate if already playing.
        /// </summary>
        public void PlayResume()
        {
            if (PlaybackRate != DefaultPlaybackRate)
            {
                PlaybackRate = DefaultPlaybackRate;
            }
            Play();
        }

        /// <summary>
        /// Stops and resets media to be played from the beginning.
        /// </summary>
        public void Stop()
        {
            _Stop();
        }

        /// <summary>
        /// Pauses media at the current position.
        /// </summary>
        public void Pause()
        {
            _Pause();
        }

        /// <summary>
        /// Plays media from the current position.
        /// </summary>
        public async void Play()
        {
            if (PlayerState == PlayerState.Started)
            {
                _Play();
            }
            else if (await OnMediaStartingAsync())
            {
                _Play();
                UpdateTimer.Start();
                OnUpdate();   // simulate the timer tick ASAP so everyone can update things.
            }
        }

        /// <summary>
        /// Invokes the captions selection dialog.
        /// </summary>
        public void InvokeCaptionSelection()
        {
            OnInvokeCaptionSelection(new RoutedEventArgs());
        }

        /// <summary>
        /// Invokes the audio stream selection dialog.
        /// </summary>
        public void InvokeAudioSelection()
        {
            OnInvokeAudioSelection(new RoutedEventArgs());
        }

        /// <summary>
        /// Seeks to the live position during live playback.
        /// </summary>
        public void SeekToLive()
        {
            var eventArgs = new RoutedEventArgs();
            OnSeekToLive(eventArgs);
            if (LivePosition.HasValue)
            {
                Seek(LivePosition.Value);
            }
        }

        internal void Seek(TimeSpan position)
        {
            var previousPosition = Position;
            _Position = position;
            OnUpdate();
            OnSeeked(new SeekRoutedEventArgs(previousPosition, position));
        }

        internal void SkipAhead(TimeSpan position)
        {
            var skippingEventArgs = new SkipRoutedEventArgs(position);
            if (SkippingAhead != null) SkippingAhead(this, skippingEventArgs);
            if (!skippingEventArgs.Canceled)
            {
                var previousPosition = Position;
                _Position = position;
                OnUpdate();
                OnSeeked(new SeekRoutedEventArgs(previousPosition, position));
            }
        }

        internal void SkipBack(TimeSpan position)
        {
            var skippingEventArgs = new SkipRoutedEventArgs(position);
            if (SkippingBack != null) SkippingBack(this, skippingEventArgs);
            if (!skippingEventArgs.Canceled)
            {
                var previousPosition = Position;
                _Position = position;
                OnUpdate();
                OnSeeked(new SeekRoutedEventArgs(previousPosition, position));
            }
        }

        internal void CompleteScrub(TimeSpan position, ref bool canceled)
        {
            var args = new ScrubProgressRoutedEventArgs(startScrubPosition, position);
            args.Canceled = canceled;
            OnScrubbingCompleted(args);
            canceled = args.Canceled;
            if (!canceled)
            {
                _Position = position;
                OnUpdate();
                PlaybackRate = rateAfterScrub;
            }
            SetValue(IsScrubbingProperty, false);
        }

        internal void StartScrub(TimeSpan position, out bool canceled)
        {
            var args = new ScrubRoutedEventArgs(position);
            OnScrubbingStarted(args);
            canceled = args.Canceled;
            if (!canceled)
            {
                startScrubPosition = Position;
                rateAfterScrub = PlaybackRate;
                PlaybackRate = 0;
                SetValue(IsScrubbingProperty, true);
            }
        }

        internal void Scrub(TimeSpan position, out bool canceled)
        {
            var args = new ScrubProgressRoutedEventArgs(startScrubPosition, position);
            OnScrubbing(args);
            canceled = args.Canceled;
            if (!canceled)
            {
                if (SeekWhileScrubbing)
                {
                    _Position = position;
                    OnUpdate();
                }
            }
        }

        #endregion

        #region Events
#if SILVERLIGHT
        /// <summary>
        /// Occurs when the log is ready.
        /// </summary>
        public event LogReadyRoutedEventHandler LogReady;

        /// <summary>
        /// Occurs when the Stretch property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<Stretch> StretchChanged;

#else
        /// <summary>
        /// Occurs when the seek point of a requested seek operation is ready for playback.
        /// </summary>
        public event RoutedEventHandler SeekCompleted;

#endif

        /// <summary>
        /// Occurs when the template is loaded for the first time and all plugins have been loaded.
        /// </summary>
        public event RoutedEventHandler Initialized;

        /// <summary>
        /// Occurs when the SelectedCaption property changed.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<Caption> SelectedCaptionChanged;

        /// <summary>
        /// Occurs when the SelectedAudioStream property changed.
        /// </summary>
        public event EventHandler<SelectedAudioStreamChangedEventArgs> SelectedAudioStreamChanged;

        /// <summary>
        /// Occurs when the PlayerState property changed. This is different from the MediaState.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<PlayerState> PlayerStateChanged;

        /// <summary>
        /// Occurs just before the source is set and offers the ability to perform blocking async operations.
        /// </summary>
        public event EventHandler<MediaPlayerDeferrableEventArgs> MediaLoading;

        /// <summary>
        /// Occurs just before the MediaEnded event fires and offers the ability to perform blocking async operations.
        /// </summary>
        public event EventHandler<MediaPlayerDeferrableEventArgs> MediaEnding;

        /// <summary>
        /// Occurs when the timer fires and gives an opportunity to update info without creating a separate timer.
        /// This only fires while media is open and continues to fire even after it's ended or while paused.
        /// </summary>
        public event RoutedEventHandler UpdateCompleted;

        /// <summary>
        /// Occurs when the BufferingProgress property changes.
        /// </summary>
        public event RoutedEventHandler BufferingProgressChanged;

        /// <summary>
        /// Occurs when the value of the CurrentState property changes.
        /// </summary>
        public event RoutedEventHandler CurrentStateChanged;

        /// <summary>
        /// Occurs when the DownloadProgress property has changed.
        /// </summary>
        public event RoutedEventHandler DownloadProgressChanged;

        /// <summary>
        /// Occurs when a timeline marker is encountered during media playback.
        /// </summary>
        public event TimelineMarkerRoutedEventHandler MarkerReached;

        /// <summary>
        /// Occurs when the MediaElement is no longer playing audio or video.
        /// </summary>
        public event MediaPlayerActionEventHandler MediaEnded;

        /// <summary>
        /// Occurs when there is an error associated with the media Source.
        /// </summary>
        public event ExceptionRoutedEventHandler MediaFailed;

        /// <summary>
        /// Occurs when the playback of new media is about to start.
        /// </summary>
        public event EventHandler<MediaPlayerDeferrableEventArgs> MediaStarting;

        /// <summary>
        /// Occurs when the playback of new media has actually started.
        /// </summary>
        public event RoutedEventHandler MediaStarted;

        /// <summary>
        /// Occurs when the MediaElement has opened the media source audio or video.
        /// </summary>
        public event RoutedEventHandler MediaOpened;

        /// <summary>
        /// Occurs when the PlaybackRate property changes.
        /// </summary>
        public event RateChangedRoutedEventHandler RateChanged;

        /// <summary>
        /// Occurs when the MediaElement source has been closed (set to null).
        /// </summary>
        public event RoutedEventHandler MediaClosed;

        /// <summary>
        /// Occurs when the Position property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan> PositionChanged;

        /// <summary>
        /// Occurs when the Volume property changes.
        /// </summary>
#if SILVERLIGHT
        public event RoutedPropertyChangedEventHandler<double> VolumeChanged;
#else
        public event RoutedEventHandler VolumeChanged;
#endif

        /// <summary>
        /// Occurs when the IsMuted property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsMutedChanged;

        /// <summary>
        /// Occurs when the IsLive property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsLiveChanged;

        /// <summary>
        /// Occurs when the AudioStreamIndex property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<int?> AudioStreamIndexChanged;

        /// <summary>
        /// Occurs when the IsFullScreen property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsFullScreenChanged;

        /// <summary>
        /// Occurs when the AdvertisingState property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<AdvertisingState> AdvertisingStateChanged;

        /// <summary>
        /// Occurs when the IsCaptionsActive property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsCaptionsActiveChanged;

        /// <summary>
        /// Occurs while the user seeks. This is mutually exclusive from scrubbing.
        /// </summary>
        public event EventHandler<SeekRoutedEventArgs> Seeked;

        /// <summary>
        /// Occurs while the user skips forward.
        /// </summary>
        public event EventHandler<SkipRoutedEventArgs> SkippingAhead;

        /// <summary>
        /// Occurs while the user skips backward.
        /// </summary>
        public event EventHandler<SkipRoutedEventArgs> SkippingBack;

        /// <summary>
        /// Occurs while the user is scrubbing. Raised for each new position.
        /// </summary>
        public event EventHandler<ScrubProgressRoutedEventArgs> Scrubbing;

        /// <summary>
        /// Occurs when the user has completed scrubbing.
        /// </summary>
        public event EventHandler<ScrubProgressRoutedEventArgs> ScrubbingCompleted;

        /// <summary>
        /// Occurs when the user starts scrubbing.
        /// </summary>
        public event EventHandler<ScrubRoutedEventArgs> ScrubbingStarted;

        /// <summary>
        /// Occurs when the SignalStrength property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<double> SignalStrengthChanged;

        /// <summary>
        /// Occurs when the HighDefinition property changes.
        /// </summary>
        public event RoutedEventHandler MediaQualityChanged;

        /// <summary>
        /// Occurs when the IsSlowMotion property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<bool> IsSlowMotionChanged;

        /// <summary>
        /// Occurs when the Duration property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan> DurationChanged;

        /// <summary>
        /// Occurs when the StartTime property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan> StartTimeChanged;

        /// <summary>
        /// Occurs when the EndTime property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan> EndTimeChanged;

        /// <summary>
        /// Occurs when the TimeRemaining property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan> TimeRemainingChanged;

        /// <summary>
        /// Occurs when the MaxPosition property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan?> LivePositionChanged;

        /// <summary>
        /// Occurs when the TimeFormatConverter property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<IValueConverter> TimeFormatConverterChanged;

        /// <summary>
        /// Occurs when the SkipBackInterval property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan?> SkipBackIntervalChanged;

        /// <summary>
        /// Occurs when the SkipAheadInterval property changes.
        /// </summary>
        public event RoutedPropertyChangedEventHandler<TimeSpan?> SkipAheadIntervalChanged;

        /// <summary>
        /// Occurs when the SeekToLive method is called.
        /// </summary>
        public event RoutedEventHandler GoLive;

        /// <summary>
        /// Occurs when the InvokeCaptionSelection method is called.
        /// </summary>
        public event RoutedEventHandler CaptionsInvoked;

        /// <summary>
        /// Occurs when the InvokeAudioSelection method is called.
        /// </summary>
        public event RoutedEventHandler AudioSelectionInvoked;

        #endregion

        #region Properties

        #region Enabled

        #region IsCaptionSelectionEnabled

        /// <summary>
        /// Occurs when the IsCaptionSelectionEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsCaptionSelectionEnabledChanged;

        /// <summary>
        /// Identifies the IsCaptionSelectionEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCaptionSelectionEnabledProperty = RegisterDependencyProperty<bool>("IsCaptionSelectionEnabled", (t, o, n) => t.OnIsCaptionSelectionEnabledChanged(), true);

        void OnIsCaptionSelectionEnabledChanged()
        {
            if (IsCaptionSelectionEnabledChanged != null) IsCaptionSelectionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether GoLive can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsCaptionSelectionEnabled
        {
            get { return (bool)GetValue(IsCaptionSelectionEnabledProperty); }
            set { SetValue(IsCaptionSelectionEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsCaptionSelectionAllowed property changes.
        /// </summary>
        public event RoutedEventHandler IsCaptionSelectionAllowedChanged;

        /// <summary>
        /// Gets whether go live is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsCaptionSelectionAllowed
        {
            get
            {
                return CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading;
            }
        }

        /// <summary>
        /// Indicates that the go live enabled state may have changed.
        /// </summary>
        protected void NotifyIsCaptionSelectionAllowedChanged()
        {
            if (IsCaptionSelectionAllowedChanged != null) IsCaptionSelectionAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsGoLiveEnabled

        /// <summary>
        /// Occurs when the IsGoLiveEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsGoLiveEnabledChanged;

        /// <summary>
        /// Identifies the IsGoLiveEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsGoLiveEnabledProperty = RegisterDependencyProperty<bool>("IsGoLiveEnabled", (t, o, n) => t.OnIsGoLiveEnabledChanged(), true);

        void OnIsGoLiveEnabledChanged()
        {
            if (IsGoLiveEnabledChanged != null) IsGoLiveEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether GoLive can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsGoLiveEnabled
        {
            get { return (bool)GetValue(IsGoLiveEnabledProperty); }
            set { SetValue(IsGoLiveEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsGoLiveAllowed property changes.
        /// </summary>
        public event RoutedEventHandler IsGoLiveAllowedChanged;

        /// <summary>
        /// Gets whether go live is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsGoLiveAllowed
        {
            get
            {
                return CanSeek && IsLive && !IsPositionLive && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading;
            }
        }

        /// <summary>
        /// Indicates that the go live enabled state may have changed.
        /// </summary>
        protected void NotifyIsGoLiveAllowedChanged()
        {
            if (IsGoLiveAllowedChanged != null) IsGoLiveAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsPlayResumeEnabled

        /// <summary>
        /// Occurs when the IsPlayResumeEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsPlayResumeEnabledChanged;

        /// <summary>
        /// Identifies the IsPlayResumeEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayResumeEnabledProperty = RegisterDependencyProperty<bool>("IsPlayResumeEnabled", (t, o, n) => t.OnIsPlayResumeEnabledChanged(), true);

        void OnIsPlayResumeEnabledChanged()
        {
            if (IsPlayResumeEnabledChanged != null) IsPlayResumeEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether PlayResume can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsPlayResumeEnabled
        {
            get { return (bool)GetValue(IsPlayResumeEnabledProperty); }
            set { SetValue(IsPlayResumeEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsPlayResumeAllowed property changes.
        /// </summary>
        public event RoutedEventHandler IsPlayResumeAllowedChanged;

        /// <summary>
        /// Indicates that play is preferred over pause. Useful for binding to the toggle state of a Play/Pause button.
        /// </summary>
        public virtual bool IsPlayResumeAllowed
        {
            get { return CurrentState != MediaElementState.Closed && (CurrentState != MediaElementState.Playing || (PlaybackRate != DefaultPlaybackRate && PlaybackRate != 0)) && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the play resume enabled state may have changed.
        /// </summary>
        protected void NotifyIsPlayResumeAllowedChanged()
        {
            if (IsPlayResumeAllowedChanged != null) IsPlayResumeAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsPauseEnabled

        /// <summary>
        /// Occurs when the IsPauseEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsPauseEnabledChanged;

        /// <summary>
        /// Identifies the IsPauseEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPauseEnabledProperty = RegisterDependencyProperty<bool>("IsPauseEnabled", (t, o, n) => t.OnIsPauseEnabledChanged(), true);

        void OnIsPauseEnabledChanged()
        {
            if (IsPauseEnabledChanged != null) IsPauseEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether Pause can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsPauseEnabled
        {
            get { return (bool)GetValue(IsPauseEnabledProperty); }
            set { SetValue(IsPauseEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsPauseEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsPauseAllowedChanged;

        /// <summary>
        /// Gets whether pause is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsPauseAllowed
        {
            get { return CanPause && CurrentState == MediaElementState.Playing && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the pause enabled state may have changed.
        /// </summary>
        protected void NotifyIsPauseAllowedChanged()
        {
            if (IsPauseAllowedChanged != null) IsPauseAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsStopEnabled

        /// <summary>
        /// Occurs when the IsStopEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsStopEnabledChanged;

        /// <summary>
        /// Identifies the IsStopEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStopEnabledProperty = RegisterDependencyProperty<bool>("IsStopEnabled", (t, o, n) => t.OnIsStopEnabledChanged(), true);

        void OnIsStopEnabledChanged()
        {
            if (IsStopEnabledChanged != null) IsStopEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether Stop can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsStopEnabled
        {
            get { return (bool)GetValue(IsStopEnabledProperty); }
            set { SetValue(IsStopEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsStopEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsStopAllowedChanged;

        /// <summary>
        /// Gets whether stop is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsStopAllowed
        {
            get { return CurrentState != MediaElementState.Closed || CurrentState != MediaElementState.Stopped; }
        }

        /// <summary>
        /// Indicates that the stop enabled state may have changed.
        /// </summary>
        protected void NotifyIsStopAllowedChanged()
        {
            if (IsStopAllowedChanged != null) IsStopAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsReplayEnabled

        /// <summary>
        /// Occurs when the IsReplayEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsReplayEnabledChanged;

        /// <summary>
        /// Identifies the IsReplayEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReplayEnabledProperty = RegisterDependencyProperty<bool>("IsReplayEnabled", (t, o, n) => t.OnIsReplayEnabledChanged(), true);

        void OnIsReplayEnabledChanged()
        {
            if (IsReplayEnabledChanged != null) IsReplayEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether Replay can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsReplayEnabled
        {
            get { return (bool)GetValue(IsReplayEnabledProperty); }
            set { SetValue(IsReplayEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsReplayEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsReplayAllowedChanged;

        /// <summary>
        /// Gets whether replay is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsReplayAllowed
        {
            get { return CanSeek && CurrentState == MediaElementState.Paused || CurrentState == MediaElementState.Playing && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the replay enabled state may have changed.
        /// </summary>
        protected void NotifyIsReplayAllowedChanged()
        {
            if (IsReplayAllowedChanged != null) IsReplayAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsAudioSelectionEnabled

        /// <summary>
        /// Occurs when the IsAudioSelectionEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsAudioSelectionEnabledChanged;

        /// <summary>
        /// Identifies the IsAudioSelectionEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAudioSelectionEnabledProperty = RegisterDependencyProperty<bool>("IsAudioSelectionEnabled", (t, o, n) => t.OnIsAudioSelectionEnabledChanged(), true);

        void OnIsAudioSelectionEnabledChanged()
        {
            if (IsAudioSelectionEnabledChanged != null) IsAudioSelectionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether AudioStreamSelection can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsAudioSelectionEnabled
        {
            get { return (bool)GetValue(IsAudioSelectionEnabledProperty); }
            set { SetValue(IsAudioSelectionEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsAudioSelectionEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsAudioSelectionAllowedChanged;

        /// <summary>
        /// Gets whether audio stream selection is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsAudioSelectionAllowed
        {
            get { return AvailableAudioStreams.Any() && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the audio stream selection enabled state may have changed.
        /// </summary>
        protected void NotifyIsAudioSelectionAllowedChanged()
        {
            if (IsAudioSelectionAllowedChanged != null) IsAudioSelectionAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsRewindEnabled

        /// <summary>
        /// Occurs when the IsRewindEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsRewindEnabledChanged;

        /// <summary>
        /// Identifies the IsRewindEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRewindEnabledProperty = RegisterDependencyProperty<bool>("IsRewindEnabled", (t, o, n) => t.OnIsRewindEnabledChanged(), true);

        void OnIsRewindEnabledChanged()
        {
            if (IsRewindEnabledChanged != null) IsRewindEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether Rewind can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsRewindEnabled
        {
            get { return (bool)GetValue(IsRewindEnabledProperty); }
            set { SetValue(IsRewindEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsRewindEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsRewindAllowedChanged;

        /// <summary>
        /// Gets whether rewind is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsRewindAllowed
        {
            get { return CurrentState == MediaElementState.Playing && PlaybackRate > MinPlaybackRate && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the rewind enabled state may have changed.
        /// </summary>
        protected void NotifyIsRewindAllowedChanged()
        {
            if (IsRewindAllowedChanged != null) IsRewindAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsFastForwardEnabled

        /// <summary>
        /// Occurs when the IsFastForwardEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsFastForwardEnabledChanged;

        /// <summary>
        /// Identifies the IsFastForwardEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastForwardEnabledProperty = RegisterDependencyProperty<bool>("IsFastForwardEnabled", (t, o, n) => t.OnIsFastForwardEnabledChanged(), true);

        void OnIsFastForwardEnabledChanged()
        {
            if (IsFastForwardEnabledChanged != null) IsFastForwardEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether FastForward can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsFastForwardEnabled
        {
            get { return (bool)GetValue(IsFastForwardEnabledProperty); }
            set { SetValue(IsFastForwardEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsFastForwardEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsFastForwardAllowedChanged;

        /// <summary>
        /// Gets whether fast forward is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsFastForwardAllowed
        {
            get { return CurrentState == MediaElementState.Playing && PlaybackRate < MaxPlaybackRate && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the fast forward enabled state may have changed.
        /// </summary>
        protected void NotifyIsFastForwardAllowedChanged()
        {
            if (IsFastForwardAllowedChanged != null) IsFastForwardAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSlowMotionEnabled

        /// <summary>
        /// Occurs when the IsSlowMotionEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSlowMotionEnabledChanged;

        /// <summary>
        /// Identifies the IsSlowMotionEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSlowMotionEnabledProperty = RegisterDependencyProperty<bool>("IsSlowMotionEnabled", (t, o, n) => t.OnIsSlowMotionEnabledChanged(), true);

        void OnIsSlowMotionEnabledChanged()
        {
            if (IsSlowMotionEnabledChanged != null) IsSlowMotionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether SlowMotion can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSlowMotionEnabled
        {
            get { return (bool)GetValue(IsSlowMotionEnabledProperty); }
            set { SetValue(IsSlowMotionEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSlowMotionEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSlowMotionAllowedChanged;

        /// <summary>
        /// Gets whether slow motion is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSlowMotionAllowed
        {
            get { return CurrentState == MediaElementState.Playing && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the slow motion enabled state may have changed.
        /// </summary>
        protected void NotifyIsSlowMotionAllowedChanged()
        {
            if (IsSlowMotionAllowedChanged != null) IsSlowMotionAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSeekEnabled

        /// <summary>
        /// Occurs when the IsSeekEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSeekEnabledChanged;

        /// <summary>
        /// Identifies the IsSeekEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSeekEnabledProperty = RegisterDependencyProperty<bool>("IsSeekEnabled", (t, o, n) => t.OnIsSeekEnabledChanged(), true);

        void OnIsSeekEnabledChanged()
        {
            if (IsSeekEnabledChanged != null) IsSeekEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether Seek can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSeekEnabled
        {
            get { return (bool)GetValue(IsSeekEnabledProperty); }
            set { SetValue(IsSeekEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSeekEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSeekAllowedChanged;

        /// <summary>
        /// Gets whether seek is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSeekAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the seek enabled state may have changed.
        /// </summary>
        protected void NotifyIsSeekAllowedChanged()
        {
            if (IsSeekAllowedChanged != null) IsSeekAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSkipPreviousEnabled

        /// <summary>
        /// Occurs when the IsSkipPreviousEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipPreviousEnabledChanged;

        /// <summary>
        /// Identifies the IsSkipPreviousEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipPreviousEnabledProperty = RegisterDependencyProperty<bool>("IsSkipPreviousEnabled", (t, o, n) => t.OnIsSkipPreviousEnabledChanged(), true);

        void OnIsSkipPreviousEnabledChanged()
        {
            if (IsSkipPreviousEnabledChanged != null) IsSkipPreviousEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether the SkipPrevious method can be called.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSkipPreviousEnabled
        {
            get { return (bool)GetValue(IsSkipPreviousEnabledProperty); }
            set { SetValue(IsSkipPreviousEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSkipPreviousEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipPreviousAllowedChanged;

        /// <summary>
        /// Gets whether skipping previous is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSkipPreviousAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the skip previous enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipPreviousAllowedChanged()
        {
            if (IsSkipPreviousAllowedChanged != null) IsSkipPreviousAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSkipNextEnabled

        /// <summary>
        /// Occurs when the IsSkipNextEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipNextEnabledChanged;

        /// <summary>
        /// Identifies the IsSkipNextEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipNextEnabledProperty = RegisterDependencyProperty<bool>("IsSkipNextEnabled", (t, o, n) => t.OnIsSkipNextEnabledChanged(), true);

        void OnIsSkipNextEnabledChanged()
        {
            if (IsSkipNextEnabledChanged != null) IsSkipNextEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether the SkipNext method can be called.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSkipNextEnabled
        {
            get { return (bool)GetValue(IsSkipNextEnabledProperty); }
            set { SetValue(IsSkipNextEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSkipNextEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipNextAllowedChanged;

        /// <summary>
        /// Gets whether skipping next is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSkipNextAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the skip next enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipNextAllowedChanged()
        {
            if (IsSkipNextAllowedChanged != null) IsSkipNextAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSkipBackEnabled

        /// <summary>
        /// Occurs when the IsSkipBackEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipBackEnabledChanged;

        /// <summary>
        /// Identifies the IsSkipBackEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipBackEnabledProperty = RegisterDependencyProperty<bool>("IsSkipBackEnabled", (t, o, n) => t.OnIsSkipBackEnabledChanged(), true);

        void OnIsSkipBackEnabledChanged()
        {
            if (IsSkipBackEnabledChanged != null) IsSkipBackEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether the SkipBack method can be called.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSkipBackEnabled
        {
            get { return (bool)GetValue(IsSkipBackEnabledProperty); }
            set { SetValue(IsSkipBackEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSkipBackEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipBackAllowedChanged;

        /// <summary>
        /// Gets whether skipping back is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSkipBackAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the skip back enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipBackAllowedChanged()
        {
            if (IsSkipBackAllowedChanged != null) IsSkipBackAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsSkipAheadEnabled

        /// <summary>
        /// Occurs when the IsSkipAheadEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipAheadEnabledChanged;

        /// <summary>
        /// Identifies the IsSkipAheadEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipAheadEnabledProperty = RegisterDependencyProperty<bool>("IsSkipAheadEnabled", (t, o, n) => t.OnIsSkipAheadEnabledChanged(), true);

        void OnIsSkipAheadEnabledChanged()
        {
            if (IsSkipAheadEnabledChanged != null) IsSkipAheadEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether the SkipAhead method can be called.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsSkipAheadEnabled
        {
            get { return (bool)GetValue(IsSkipAheadEnabledProperty); }
            set { SetValue(IsSkipAheadEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsSkipAheadEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsSkipAheadAllowedChanged;

        /// <summary>
        /// Gets whether skipping ahead is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsSkipAheadAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the skip ahead enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipAheadAllowedChanged()
        {
            if (IsSkipAheadAllowedChanged != null) IsSkipAheadAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #region IsScrubbingEnabled

        /// <summary>
        /// Occurs when the IsScrubbingEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsScrubbingEnabledChanged;

        /// <summary>
        /// Identifies the IsScrubbingEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsScrubbingEnabledProperty = RegisterDependencyProperty<bool>("IsScrubbingEnabled", (t, o, n) => t.OnIsScrubbingEnabledChanged(), true);

        void OnIsScrubbingEnabledChanged()
        {
            if (IsScrubbingEnabledChanged != null) IsScrubbingEnabledChanged(this, new RoutedEventArgs());
        }

        /// <summary>
        /// Gets based on the current state whether scrubbing can occur.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsScrubbingEnabled
        {
            get { return (bool)GetValue(IsScrubbingEnabledProperty); }
            set { SetValue(IsScrubbingEnabledProperty, value); }
        }

        /// <summary>
        /// Occurs when the IsScrubbingEnabled property changes.
        /// </summary>
        public event RoutedEventHandler IsScrubbingAllowedChanged;

        /// <summary>
        /// Gets whether scrubbing is allowed based on the state of the player.
        /// </summary>
        public virtual bool IsScrubbingAllowed
        {
            get { return CanSeek && CurrentState != MediaElementState.Closed && (PlayerState == PlayerState.Started || PlayerState == PlayerState.Opened) && AdvertisingState != AdvertisingState.Linear && AdvertisingState != AdvertisingState.Loading; }
        }

        /// <summary>
        /// Indicates that the scrubbing enabled state may have changed.
        /// </summary>
        protected void NotifyIsScrubbingAllowedChanged()
        {
            if (IsScrubbingAllowedChanged != null) IsScrubbingAllowedChanged(this, new RoutedEventArgs());
        }
        #endregion

        #endregion

        #region Visibility

#if SILVERLIGHT
        #region IsDisplayModeVisible
        /// <summary>
        /// Identifies the IsDisplayModeVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDisplayModeVisibleProperty = RegisterDependencyProperty<bool>("IsDisplayModeVisible", (t, o, n) => t.OnIsDisplayModeVisibleChanged(), DefaultIsDisplayModeVisible);

        static bool DefaultIsDisplayModeVisible
        {
            get
            {
#if WINDOWS_PHONE
                return true;
#else
                return false;
#endif
            }
        }

        void OnIsDisplayModeVisibleChanged()
        {
            if (IsDisplayModeVisibleChanged != null) IsDisplayModeVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsDisplayModeVisible property changes.
        /// </summary>
        public event EventHandler IsDisplayModeVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive DisplayMode feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsDisplayModeVisible
        {
            get { return (bool)GetValue(IsDisplayModeVisibleProperty); }
            set { SetValue(IsDisplayModeVisibleProperty, value); }
        }
        #endregion
#endif

        #region IsAudioSelectionVisible
        /// <summary>
        /// Identifies the IsAudioSelectionVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAudioSelectionVisibleProperty = RegisterDependencyProperty<bool>("IsAudioSelectionVisible", (t, o, n) => t.OnIsAudioSelectionVisibleChanged(), false);

        void OnIsAudioSelectionVisibleChanged()
        {
            if (IsAudioSelectionVisibleChanged != null) IsAudioSelectionVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsAudioSelectionVisible property changes.
        /// </summary>
        public event EventHandler IsAudioSelectionVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive AudioStreamSelection feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsAudioSelectionVisible
        {
            get { return (bool)GetValue(IsAudioSelectionVisibleProperty); }
            set { SetValue(IsAudioSelectionVisibleProperty, value); }
        }
        #endregion

        #region IsCaptionSelectionVisible
        /// <summary>
        /// Identifies the IsCaptionSelectionVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCaptionSelectionVisibleProperty = RegisterDependencyProperty<bool>("IsCaptionSelectionVisible", (t, o, n) => t.OnIsCaptionSelectionVisibleChanged(), false);

        void OnIsCaptionSelectionVisibleChanged()
        {
            if (IsCaptionSelectionVisibleChanged != null) IsCaptionSelectionVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsCaptionSelectionVisible property changes.
        /// </summary>
        public event EventHandler IsCaptionSelectionVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Captions feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsCaptionSelectionVisible
        {
            get { return (bool)GetValue(IsCaptionSelectionVisibleProperty); }
            set { SetValue(IsCaptionSelectionVisibleProperty, value); }
        }
        #endregion

        #region IsDurationVisible
        /// <summary>
        /// Identifies the IsDurationVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDurationVisibleProperty = RegisterDependencyProperty<bool>("IsDurationVisible", (t, o, n) => t.OnIsDurationVisibleChanged(), false);

        void OnIsDurationVisibleChanged()
        {
            if (IsDurationVisibleChanged != null) IsDurationVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsDurationVisible property changes.
        /// </summary>
        public event EventHandler IsDurationVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Duration feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsDurationVisible
        {
            get { return (bool)GetValue(IsDurationVisibleProperty); }
            set { SetValue(IsDurationVisibleProperty, value); }
        }
        #endregion

        #region IsTimeRemainingVisible
        /// <summary>
        /// Identifies the IsTimeRemainingVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTimeRemainingVisibleProperty = RegisterDependencyProperty<bool>("IsTimeRemainingVisible", (t, o, n) => t.OnIsTimeRemainingVisibleChanged(), true);

        void OnIsTimeRemainingVisibleChanged()
        {
            if (IsTimeRemainingVisibleChanged != null) IsTimeRemainingVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsTimeRemainingVisible property changes.
        /// </summary>
        public event EventHandler IsTimeRemainingVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive TimeRemaining feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsTimeRemainingVisible
        {
            get { return (bool)GetValue(IsTimeRemainingVisibleProperty); }
            set { SetValue(IsTimeRemainingVisibleProperty, value); }
        }
        #endregion

        #region IsFastForwardVisible
        /// <summary>
        /// Identifies the IsFastForwardVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFastForwardVisibleProperty = RegisterDependencyProperty<bool>("IsFastForwardVisible", (t, o, n) => t.OnIsFastForwardVisibleChanged(), false);

        void OnIsFastForwardVisibleChanged()
        {
            if (IsFastForwardVisibleChanged != null) IsFastForwardVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsFastForwardVisible property changes.
        /// </summary>
        public event EventHandler IsFastForwardVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive FastForward feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsFastForwardVisible
        {
            get { return (bool)GetValue(IsFastForwardVisibleProperty); }
            set { SetValue(IsFastForwardVisibleProperty, value); }
        }
        #endregion

        #region IsFullScreenVisible
        /// <summary>
        /// Identifies the IsFullScreenVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullScreenVisibleProperty = RegisterDependencyProperty<bool>("IsFullScreenVisible", (t, o, n) => t.OnIsFullScreenVisibleChanged(), false);

        void OnIsFullScreenVisibleChanged()
        {
            if (IsFullScreenVisibleChanged != null) IsFullScreenVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsFullScreenVisible property changes.
        /// </summary>
        public event EventHandler IsFullScreenVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive FullScreen feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsFullScreenVisible
        {
            get { return (bool)GetValue(IsFullScreenVisibleProperty); }
            set { SetValue(IsFullScreenVisibleProperty, value); }
        }
        #endregion

        #region IsGoLiveVisible
        /// <summary>
        /// Identifies the IsGoLiveVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsGoLiveVisibleProperty = RegisterDependencyProperty<bool>("IsGoLiveVisible", (t, o, n) => t.OnIsGoLiveVisibleChanged(), false);

        void OnIsGoLiveVisibleChanged()
        {
            if (IsGoLiveVisibleChanged != null) IsGoLiveVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsGoLiveVisible property changes.
        /// </summary>
        public event EventHandler IsGoLiveVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive GoLive feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsGoLiveVisible
        {
            get { return (bool)GetValue(IsGoLiveVisibleProperty); }
            set { SetValue(IsGoLiveVisibleProperty, value); }
        }
        #endregion

        #region IsPlayPauseVisible
        /// <summary>
        /// Identifies the IsPlayPauseVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPlayPauseVisibleProperty = RegisterDependencyProperty<bool>("IsPlayPauseVisible", (t, o, n) => t.OnIsPlayPauseVisibleChanged(), true);

        void OnIsPlayPauseVisibleChanged()
        {
            if (IsPlayPauseVisibleChanged != null) IsPlayPauseVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsPlayPauseVisible property changes.
        /// </summary>
        public event EventHandler IsPlayPauseVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive PlayPause feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsPlayPauseVisible
        {
            get { return (bool)GetValue(IsPlayPauseVisibleProperty); }
            set { SetValue(IsPlayPauseVisibleProperty, value); }
        }
        #endregion

        #region IsTimeElapsedVisible
        /// <summary>
        /// Identifies the IsTimeElapsedVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTimeElapsedVisibleProperty = RegisterDependencyProperty<bool>("IsTimeElapsedVisible", (t, o, n) => t.OnIsTimeElapsedVisibleChanged(), true);

        void OnIsTimeElapsedVisibleChanged()
        {
            if (IsTimeElapsedVisibleChanged != null) IsTimeElapsedVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsTimeElapsedVisible property changes.
        /// </summary>
        public event EventHandler IsTimeElapsedVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Position feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsTimeElapsedVisible
        {
            get { return (bool)GetValue(IsTimeElapsedVisibleProperty); }
            set { SetValue(IsTimeElapsedVisibleProperty, value); }
        }
        #endregion

        #region IsReplayVisible
        /// <summary>
        /// Identifies the IsReplayVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReplayVisibleProperty = RegisterDependencyProperty<bool>("IsReplayVisible", (t, o, n) => t.OnIsReplayVisibleChanged(), false);

        void OnIsReplayVisibleChanged()
        {
            if (IsReplayVisibleChanged != null) IsReplayVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsReplayVisible property changes.
        /// </summary>
        public event EventHandler IsReplayVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Replay feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsReplayVisible
        {
            get { return (bool)GetValue(IsReplayVisibleProperty); }
            set { SetValue(IsReplayVisibleProperty, value); }
        }
        #endregion

        #region IsRewindVisible
        /// <summary>
        /// Identifies the IsRewindVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsRewindVisibleProperty = RegisterDependencyProperty<bool>("IsRewindVisible", (t, o, n) => t.OnIsRewindVisibleChanged(), false);

        void OnIsRewindVisibleChanged()
        {
            if (IsRewindVisibleChanged != null) IsRewindVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsRewindVisible property changes.
        /// </summary>
        public event EventHandler IsRewindVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Rewind feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsRewindVisible
        {
            get { return (bool)GetValue(IsRewindVisibleProperty); }
            set { SetValue(IsRewindVisibleProperty, value); }
        }
        #endregion

        #region IsSkipPreviousVisible
        /// <summary>
        /// Identifies the IsSkipPreviousVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipPreviousVisibleProperty = RegisterDependencyProperty<bool>("IsSkipPreviousVisible", (t, o, n) => t.OnIsSkipPreviousVisibleChanged(), false);

        void OnIsSkipPreviousVisibleChanged()
        {
            if (IsSkipPreviousVisibleChanged != null) IsSkipPreviousVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsSkipPreviousVisible property changes.
        /// </summary>
        public event EventHandler IsSkipPreviousVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive SkipPrevious feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsSkipPreviousVisible
        {
            get { return (bool)GetValue(IsSkipPreviousVisibleProperty); }
            set { SetValue(IsSkipPreviousVisibleProperty, value); }
        }
        #endregion

        #region IsSkipNextVisible
        /// <summary>
        /// Identifies the IsSkipNextVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSkipNextVisibleProperty = RegisterDependencyProperty<bool>("IsSkipNextVisible", (t, o, n) => t.OnIsSkipNextVisibleChanged(), false);

        void OnIsSkipNextVisibleChanged()
        {
            if (IsSkipNextVisibleChanged != null) IsSkipNextVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsSkipNextVisible property changes.
        /// </summary>
        public event EventHandler IsSkipNextVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive SkipNext feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsSkipNextVisible
        {
            get { return (bool)GetValue(IsSkipNextVisibleProperty); }
            set { SetValue(IsSkipNextVisibleProperty, value); }
        }
        #endregion

        #region IsSlowMotionVisible
        /// <summary>
        /// Identifies the IsSlowMotionVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSlowMotionVisibleProperty = RegisterDependencyProperty<bool>("IsSlowMotionVisible", (t, o, n) => t.OnIsSlowMotionVisibleChanged(), false);

        void OnIsSlowMotionVisibleChanged()
        {
            if (IsSlowMotionVisibleChanged != null) IsSlowMotionVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsSlowMotionVisible property changes.
        /// </summary>
        public event EventHandler IsSlowMotionVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive SlowMotion feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsSlowMotionVisible
        {
            get { return (bool)GetValue(IsSlowMotionVisibleProperty); }
            set { SetValue(IsSlowMotionVisibleProperty, value); }
        }
        #endregion

        #region IsStopVisible
        /// <summary>
        /// Identifies the IsStopVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStopVisibleProperty = RegisterDependencyProperty<bool>("IsStopVisible", (t, o, n) => t.OnIsStopVisibleChanged(), false);

        void OnIsStopVisibleChanged()
        {
            if (IsStopVisibleChanged != null) IsStopVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsStopVisible property changes.
        /// </summary>
        public event EventHandler IsStopVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Stop feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsStopVisible
        {
            get { return (bool)GetValue(IsStopVisibleProperty); }
            set { SetValue(IsStopVisibleProperty, value); }
        }
        #endregion

        #region IsTimelineVisible
        /// <summary>
        /// Identifies the IsTimelineVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTimelineVisibleProperty = RegisterDependencyProperty<bool>("IsTimelineVisible", (t, o, n) => t.OnIsTimelineVisibleChanged(), true);

        void OnIsTimelineVisibleChanged()
        {
            if (IsTimelineVisibleChanged != null) IsTimelineVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsTimelineVisible property changes.
        /// </summary>
        public event EventHandler IsTimelineVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Timeline feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsTimelineVisible
        {
            get { return (bool)GetValue(IsTimelineVisibleProperty); }
            set { SetValue(IsTimelineVisibleProperty, value); }
        }
        #endregion

        #region IsVolumeVisible
        /// <summary>
        /// Identifies the IsVolumeVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsVolumeVisibleProperty = RegisterDependencyProperty<bool>("IsVolumeVisible", (t, o, n) => t.OnIsVolumeVisibleChanged(), true);

        void OnIsVolumeVisibleChanged()
        {
            if (IsVolumeVisibleChanged != null) IsVolumeVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsVolumeVisible property changes.
        /// </summary>
        public event EventHandler IsVolumeVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive Volume feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsVolumeVisible
        {
            get { return (bool)GetValue(IsVolumeVisibleProperty); }
            set { SetValue(IsVolumeVisibleProperty, value); }
        }
        #endregion

        #region IsSignalStrengthVisible
        /// <summary>
        /// Identifies the IsSignalStrengthVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSignalStrengthVisibleProperty = RegisterDependencyProperty<bool>("IsSignalStrengthVisible", (t, o, n) => t.OnIsSignalStrengthVisibleChanged(), false);

        void OnIsSignalStrengthVisibleChanged()
        {
            if (IsSignalStrengthVisibleChanged != null) IsSignalStrengthVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsSignalStrengthVisible property changes.
        /// </summary>
        public event EventHandler IsSignalStrengthVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive SignalStrength feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsSignalStrengthVisible
        {
            get { return (bool)GetValue(IsSignalStrengthVisibleProperty); }
            set { SetValue(IsSignalStrengthVisibleProperty, value); }
        }
        #endregion

        #region IsResolutionIndicatorVisible
        /// <summary>
        /// Identifies the IsResolutionIndicatorVisible dependency property.
        /// </summary>
        public static readonly DependencyProperty IsResolutionIndicatorVisibleProperty = RegisterDependencyProperty<bool>("IsResolutionIndicatorVisible", (t, o, n) => t.OnIsResolutionIndicatorVisibleChanged(), false);

        void OnIsResolutionIndicatorVisibleChanged()
        {
            if (IsResolutionIndicatorVisibleChanged != null) IsResolutionIndicatorVisibleChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Occurs when the IsResolutionIndicatorVisible property changes.
        /// </summary>
        public event EventHandler IsResolutionIndicatorVisibleChanged;

        /// <summary>
        /// Gets or sets if the interactive SignalStrength feature should be visible and therefore available for the user to control.
        /// </summary>
        [Category(Categories.Appearance)]
        public bool IsResolutionIndicatorVisible
        {
            get { return (bool)GetValue(IsResolutionIndicatorVisibleProperty); }
            set { SetValue(IsResolutionIndicatorVisibleProperty, value); }
        }
        #endregion

        #endregion

        /// <inheritdoc /> 
        MediaPlayer IMediaSource.Player { get { return this; } }

        #region TimeFormatConverter
        /// <summary>
        /// Identifies the TimeFormatConverter dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeFormatConverterProperty = RegisterDependencyProperty<IValueConverter>("TimeFormatConverter", (t, o, n) => t.OnTimeFormatConverterChanged(o, n));

        void OnTimeFormatConverterChanged(IValueConverter oldValue, IValueConverter newValue)
        {
            OnTimeFormatConverterChanged(new RoutedPropertyChangedEventArgs<IValueConverter>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets a an IValueConverter that is used to display the time to the user such as the position, duration, and time remaining.
        /// The default value applies the string format of "h\\:mm\\:ss".
        /// </summary>
        [Category(Categories.Appearance)]
        public IValueConverter TimeFormatConverter
        {
            get { return (IValueConverter)GetValue(TimeFormatConverterProperty); }
            set { SetValue(TimeFormatConverterProperty, value); }
        }

        /// <summary>
        /// The default TimeFormat string to use to display position, time remaining and duration.
        /// </summary>
        public static string DefaultTimeFormat
        {
            get
            {
#if SILVERLIGHT
                return PlayerFramework.Resources.TimeSpanReadableFormat;
#else
                if (!IsInDesignMode)
                {
                    return GetResourceString("TimeSpanReadableFormat");
                }
                else
                {
                    return @"h\:mm\:ss";
                }
#endif
            }
        }
        #endregion

        #region SkipBackInterval
        /// <summary>
        /// Identifies the SkipBackInterval dependency property.
        /// </summary>
        public static readonly DependencyProperty SkipBackIntervalProperty = RegisterDependencyProperty<TimeSpan?>("SkipBackInterval", (t, o, n) => t.OnSkipBackIntervalChanged(o, n), DefaultSkipBackInterval);

        void OnSkipBackIntervalChanged(TimeSpan? oldValue, TimeSpan? newValue)
        {
            OnSkipBackIntervalChanged(new RoutedPropertyChangedEventArgs<TimeSpan?>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the amount of time in the video to skip back when the user selects skip back.
        /// This can be set to null to cause the skip back action to go back to the beginning.
        /// The default is 30 seconds although it will never go back past the beginning.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan? SkipBackInterval
        {
            get { return (TimeSpan?)GetValue(SkipBackIntervalProperty); }
            set { SetValue(SkipBackIntervalProperty, value); }
        }

        /// <summary>
        /// The default SkipBackInterval value.
        /// </summary>
        public static TimeSpan DefaultSkipBackInterval
        {
            get
            {
#if WINDOWS_PHONE
                return TimeSpan.FromSeconds(7);
#else
                return TimeSpan.FromSeconds(30);
#endif
            }
        }
        #endregion

        #region SkipAheadInterval
        /// <summary>
        /// Identifies the SkipAheadInterval dependency property.
        /// </summary>
        public static readonly DependencyProperty SkipAheadIntervalProperty = RegisterDependencyProperty<TimeSpan?>("SkipAheadInterval", (t, o, n) => t.OnSkipAheadIntervalChanged(o, n), DefaultSkipAheadInterval);

        void OnSkipAheadIntervalChanged(TimeSpan? oldValue, TimeSpan? newValue)
        {
            OnSkipAheadIntervalChanged(new RoutedPropertyChangedEventArgs<TimeSpan?>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the amount of time in the video to skip ahead when the user selects skip ahead.
        /// This can be set to null to cause the skip ahead action to go directly to the end.
        /// The default is 30 seconds although it will never go past the end (or MaxPosition if set).
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan? SkipAheadInterval
        {
            get { return (TimeSpan?)GetValue(SkipAheadIntervalProperty); }
            set { SetValue(SkipAheadIntervalProperty, value); }
        }

        /// <summary>
        /// The default SkipAheadInterval value.
        /// </summary>
        public static TimeSpan DefaultSkipAheadInterval
        {
            get { return TimeSpan.FromSeconds(30); }
        }
        #endregion

        #region VisualMarkers
        /// <summary>
        /// Identifies the TimelineMarkers dependency property.
        /// </summary>
        public static readonly DependencyProperty VisualMarkersProperty = RegisterDependencyProperty<ObservableCollection<VisualMarker>>("VisualMarkers");

        /// <summary>
        /// Gets or sets the collection of markers to be displayed in the timeline.
        /// </summary>
        [Category(Categories.Common)]
        public ObservableCollection<VisualMarker> VisualMarkers
        {
            get { return (ObservableCollection<VisualMarker>)GetValue(VisualMarkersProperty); }
        }

        ICollection<VisualMarker> IMediaSource.VisualMarkers
        {
            get { return VisualMarkers; }
        }
        #endregion

        #region Markers

        /// <summary>
        /// Gets the collection of timeline markers associated with the currently loaded media file.
        /// </summary>
        public TimelineMarkerCollection Markers { get { return _Markers; } }

        #endregion

        #region AutoLoad
        /// <summary>
        /// Identifies the AutoLoad dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoLoadProperty = RegisterDependencyProperty<bool>("AutoLoad", (t, o, n) => t.OnAutoLoadChanged(n), true);

        void OnAutoLoadChanged(bool newValue)
        {
            if (newValue && PendingLoadAction != null)
            {
                PendingLoadAction();
                PendingLoadAction = null;
            }
        }

        /// <summary>
        /// Gets or sets a gate for loading the source. Setting this to false postpones any subsequent calls to the Source property and SetSource method.
        /// Once the source is set on the underlying MediaElement, the media begins to download.
        /// Note: There is another opportunity to block setting the source by using the awaitable BeforeMediaLoaded event.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool AutoLoad
        {
            get { return (bool)GetValue(AutoLoadProperty); }
            set { SetValue(AutoLoadProperty, value); }
        }
        #endregion

        #region SignalStrength
        /// <summary>
        /// Identifies the SignalStrength dependency property.
        /// </summary>
        public static readonly DependencyProperty SignalStrengthProperty = RegisterDependencyProperty<double>("SignalStrength", (t, o, n) => t.OnSignalStrengthChanged(o, n), 0.0);

        void OnSignalStrengthChanged(double oldValue, double newValue)
        {
            OnSignalStrengthChanged(new RoutedPropertyChangedEventArgs<double>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the signal strength used to indicate visually to the user the quality of the bitrate.
        /// This is only useful for adaptive streaming and is only displayed when IsSignalStrengthVisible = true
        /// </summary>
        [Category(Categories.Info)]
        public double SignalStrength
        {
            get { return (double)GetValue(SignalStrengthProperty); }
            set { SetValue(SignalStrengthProperty, value); }
        }
        #endregion

        #region MediaQuality
        /// <summary>
        /// Identifies the MediaQuality dependency property.
        /// </summary>
        public static readonly DependencyProperty MediaQualityProperty = RegisterDependencyProperty<MediaQuality>("MediaQuality", (t, o, n) => t.OnMediaQualityChanged(), MediaQuality.StandardDefinition);

        void OnMediaQualityChanged()
        {
            OnMediaQualityChanged(new RoutedEventArgs());
        }

        /// <summary>
        /// Gets or sets an enum indicating the quality or resolution of the media. This does not affect the actual quality and only offers visual indication to the end-user when IsResolutionIndicatorVisible is true.
        /// </summary>
        [Category(Categories.Info)]
        public MediaQuality MediaQuality
        {
            get { return (MediaQuality)GetValue(MediaQualityProperty); }
            set { SetValue(MediaQualityProperty, value); }
        }
        #endregion

        #region LivePositionBuffer
        /// <summary>
        /// Identifies the LivePositionBuffer dependency property.
        /// </summary>
        public static readonly DependencyProperty LivePositionBufferProperty = RegisterDependencyProperty<TimeSpan>("LivePositionBuffer", (t, o, n) => t.OnLivePositionBufferChanged(o, n), TimeSpan.FromSeconds(10));

        void OnLivePositionBufferChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            UpdateIsPositionLive();
        }

        void UpdateIsPositionLive()
        {
            var liveThreshold = IsPositionLive
                           ? TimeSpan.FromSeconds(LivePositionBuffer.TotalSeconds + (LivePositionBuffer.TotalSeconds * .1))
                           : TimeSpan.FromSeconds(LivePositionBuffer.TotalSeconds - (LivePositionBuffer.TotalSeconds * .1));

            IsPositionLive = LivePosition.HasValue && LivePosition.Value.Subtract(Position) < liveThreshold;
        }

        /// <summary>
        /// Gets or sets a value indicating what the tollerance is for determining whether or not the current position is live. IsPositionLive is affected by this property.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan LivePositionBuffer
        {
            get { return (TimeSpan)GetValue(LivePositionBufferProperty); }
            set { SetValue(LivePositionBufferProperty, value); }
        }
        #endregion

        #region IsPositionLive
        /// <summary>
        /// Identifies the IsPositionLive dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPositionLiveProperty = RegisterDependencyProperty<bool>("IsPositionLive", (t, o, n) => t.OnIsPositionLiveChanged(o, n), false);

        void OnIsPositionLiveChanged(bool oldValue, bool newValue)
        {
            NotifyIsGoLiveAllowedChanged();
        }

        /// <summary>
        /// Gets or sets a value indicating what the tollerance is for determining whether or not the current position is live. IsPositionLive is affected by this property.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool IsPositionLive
        {
            get { return (bool)GetValue(IsPositionLiveProperty); }
            private set { SetValue(IsPositionLiveProperty, value); }
        }
        #endregion

        #region LivePosition
        /// <summary>
        /// Identifies the LivePosition dependency property.
        /// </summary>
        public static readonly DependencyProperty LivePositionProperty = RegisterDependencyProperty<TimeSpan?>("LivePosition", (t, o, n) => t.OnLivePositionChanged(o, n), (TimeSpan?)null);

        void OnLivePositionChanged(TimeSpan? oldValue, TimeSpan? newValue)
        {
            UpdateIsPositionLive();
            OnLivePositionChanged(new RoutedPropertyChangedEventArgs<TimeSpan?>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the Live position for realtime/live playback.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan? LivePosition
        {
            get { return (TimeSpan?)GetValue(LivePositionProperty); }
            set { SetValue(LivePositionProperty, value); }
        }
        #endregion

        #region Duration
        /// <summary>
        /// Identifies the Duration dependency property.
        /// </summary>
        public static readonly DependencyProperty DurationProperty = RegisterDependencyProperty<TimeSpan>("Duration", (t, o, n) => t.OnDurationChanged(o, n), TimeSpan.Zero);

        void OnDurationChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            OnDurationChanged(new RoutedPropertyChangedEventArgs<TimeSpan>(oldValue, newValue));
        }

        /// <summary>
        /// Gets the duration of the current video or audio. For VOD, this is automatically set from the NaturalDuration property.
        /// </summary>
        [Category(Categories.Info)]
        public TimeSpan Duration
        {
            get { return (TimeSpan)GetValue(DurationProperty); }
        }
        #endregion

        #region IsStartTimeOffset
        /// <summary>
        /// Identifies the IsStartTimeOffset dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStartTimeOffsetProperty = RegisterDependencyProperty<bool>("IsStartTimeOffset", false);

        /// <summary>
        /// Gets or sets the IsStartTimeOffset of the current video or audio. For VOD, this is automatically set from the NaturalIsStartTimeOffset property.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsStartTimeOffset
        {
            get { return (bool)GetValue(IsStartTimeOffsetProperty); }
            set { SetValue(IsStartTimeOffsetProperty, value); }
        }
        #endregion

        #region StartTime
        /// <summary>
        /// Identifies the StartTime dependency property.
        /// </summary>
        public static readonly DependencyProperty StartTimeProperty = RegisterDependencyProperty<TimeSpan>("StartTime", (t, o, n) => t.OnStartTimeChanged(o, n), TimeSpan.Zero);

        void OnStartTimeChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            SetValue(DurationProperty, EndTime.Subtract(newValue));
            OnStartTimeChanged(new RoutedPropertyChangedEventArgs<TimeSpan>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the StartTime of the current video or audio. For VOD, this is automatically set from the NaturalStartTime property.
        /// </summary>
        [Category(Categories.Info)]
        public TimeSpan StartTime
        {
            get { return (TimeSpan)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        #endregion

        #region EndTime
        /// <summary>
        /// Identifies the EndTime dependency property.
        /// </summary>
        public static readonly DependencyProperty EndTimeProperty = RegisterDependencyProperty<TimeSpan>("EndTime", (t, o, n) => t.OnEndTimeChanged(o, n), TimeSpan.Zero);

        void OnEndTimeChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            SetValue(DurationProperty, newValue.Subtract(StartTime));
            SetValue(TimeRemainingProperty, TimeSpanExtensions.Max(newValue.Subtract(Position), TimeSpan.Zero));
            OnEndTimeChanged(new RoutedPropertyChangedEventArgs<TimeSpan>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the EndTime of the current media. For progressive video, this is automatically set from Duration - StartTime.
        /// </summary>
        [Category(Categories.Info)]
        public TimeSpan EndTime
        {
            get { return (TimeSpan)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }
        #endregion

        #region TimeRemaining
        /// <summary>
        /// Identifies the TimeRemaining dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeRemainingProperty = RegisterDependencyProperty<TimeSpan>("TimeRemaining", (t, o, n) => t.OnTimeRemainingChanged(o, n), TimeSpan.Zero);

        void OnTimeRemainingChanged(TimeSpan oldValue, TimeSpan newValue)
        {
            OnTimeRemainingChanged(new RoutedPropertyChangedEventArgs<TimeSpan>(oldValue, newValue));
        }

        /// <summary>
        /// Gets the time remaining before the media will finish. This is calculated automatically whenever the Position or Duration properties change.
        /// </summary>
        [Category(Categories.Info)]
        public TimeSpan TimeRemaining
        {
            get { return (TimeSpan)GetValue(TimeRemainingProperty); }
        }
        #endregion

        #region SeekWhileScrubbing
        /// <summary>
        /// Identifies the SeekWhileScrubbing dependency property.
        /// </summary>
        public static readonly DependencyProperty SeekWhileScrubbingProperty = RegisterDependencyProperty<bool>("SeekWhileScrubbing", true);

        /// <summary>
        /// Gets or sets whether or not the position should change while the user is actively scrubbing. If false, media will be paused until the user has finished scrubbing.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool SeekWhileScrubbing
        {
            get { return (bool)GetValue(SeekWhileScrubbingProperty); }
            set { SetValue(SeekWhileScrubbingProperty, value); }
        }
        #endregion

        #region ReplayOffset
        /// <summary>
        /// Identifies the ReplayOffset dependency property.
        /// </summary>
        public static readonly DependencyProperty ReplayOffsetProperty = RegisterDependencyProperty<TimeSpan>("ReplayOffset", TimeSpan.FromSeconds(5));

        /// <summary>
        /// Gets or sets the amount of time to reset the current play position back for an instant replay. Default 5 seconds.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan ReplayOffset
        {
            get { return (TimeSpan)GetValue(ReplayOffsetProperty); }
            set { SetValue(ReplayOffsetProperty, value); }
        }
        #endregion

        #region SlowMotionPlaybackRate
        /// <summary>
        /// Identifies the SlowMotionPlaybackRate dependency property.
        /// </summary>
        public static readonly DependencyProperty SlowMotionPlaybackRateProperty = RegisterDependencyProperty<double>("SlowMotionPlaybackRate", .25);

        /// <summary>
        /// Gets or sets the playback rate when operating in slow motion (IsSlowMotion). Default .25
        /// </summary>
        [Category(Categories.Advanced)]
        public double SlowMotionPlaybackRate
        {
            get { return (double)GetValue(SlowMotionPlaybackRateProperty); }
            set { SetValue(SlowMotionPlaybackRateProperty, value); }
        }
        #endregion

        #region IsSlowMotion
        /// <summary>
        /// Identifies the IsSlowMotion dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSlowMotionProperty = RegisterDependencyProperty<bool>("IsSlowMotion", (t, o, n) => t.OnIsSlowMotionChanged(o, n), false);

        void OnIsSlowMotionChanged(bool oldValue, bool newValue)
        {
            if (newValue) PlaybackRate = SlowMotionPlaybackRate;
            else PlaybackRate = DefaultPlaybackRate;
            OnIsSlowMotionChanged(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets whether or not the media is playing in slow motion.
        /// The slow motion playback rate is defined by the SlowMotionPlaybackRate property.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool IsSlowMotion
        {
            get { return (bool)GetValue(IsSlowMotionProperty); }
            set { SetValue(IsSlowMotionProperty, value); }
        }
        #endregion

        #region IsCaptionsActive
        /// <summary>
        /// Identifies the IsCaptionsActive dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCaptionsActiveProperty = RegisterDependencyProperty<bool>("IsCaptionsActive", (t, o, n) => t.OnIsCaptionsActiveChanged(o, n), false);

        void OnIsCaptionsActiveChanged(bool oldValue, bool newValue)
        {
            _IsCaptionsActive = newValue;
            OnIsCaptionsActiveChanged(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets if the player should show the captions configuration window.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsCaptionsActive
        {
            get { return (bool)GetValue(IsCaptionsActiveProperty); }
            set { SetValue(IsCaptionsActiveProperty, value); }
        }
        #endregion

        #region IsFullScreen
        /// <summary>
        /// Identifies the IsFullScreen dependency property.
        /// </summary>
        public static readonly DependencyProperty IsFullScreenProperty = RegisterDependencyProperty<bool>("IsFullScreen", (t, o, n) => t.OnIsFullScreenChanged(o, n), false);

        void OnIsFullScreenChanged(bool oldValue, bool newValue)
        {
            if (!newValue && oldValue)
            {
#if SILVERLIGHT
                this.Cursor = IsInteractive ? Cursors.Arrow : Cursors.None;
#endif
            }
            _IsFullScreen = newValue;
            OnIsFullScreenChanged(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets if the player should indicate it is in fullscreen mode.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsFullScreen
        {
            get { return (bool)GetValue(IsFullScreenProperty); }
            set { SetValue(IsFullScreenProperty, value); }
        }
        #endregion

        #region AdvertisingState
        /// <summary>
        /// Identifies the AdvertisingState dependency property.
        /// </summary>
        public static readonly DependencyProperty AdvertisingStateProperty = RegisterDependencyProperty<AdvertisingState>("AdvertisingState", (t, o, n) => t.OnAdvertisingStateChanged(o, n), AdvertisingState.None);

        void OnAdvertisingStateChanged(AdvertisingState oldValue, AdvertisingState newValue)
        {
            _AdvertisingState = newValue;
            OnAdvertisingStateChanged(new RoutedPropertyChangedEventArgs<AdvertisingState>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets if the player should indicate it is in Advertising mode.
        /// </summary>
        [Category(Categories.Common)]
        public AdvertisingState AdvertisingState
        {
            get { return (AdvertisingState)GetValue(AdvertisingStateProperty); }
            set { SetValue(AdvertisingStateProperty, value); }
        }
        #endregion

        #region IsScrubbing
        /// <summary>
        /// Identifies the IsScrubbing dependency property.
        /// </summary>
        public static readonly DependencyProperty IsScrubbingProperty = RegisterDependencyProperty<bool>("IsScrubbing", false);

        /// <summary>
        /// Gets whether or not the user is actively scrubbing.
        /// </summary>
        [Category(Categories.Info)]
        public bool IsScrubbing
        {
            get { return (bool)GetValue(IsScrubbingProperty); }
        }
        #endregion

        #region StartupPosition
        /// <summary>
        /// Identifies the StartupPosition dependency property.
        /// </summary>
        public static readonly DependencyProperty StartupPositionProperty = RegisterDependencyProperty<TimeSpan?>("StartupPosition", (TimeSpan?)null);

        /// <summary>
        /// Gets or sets the position at which to start the video at. This is useful for resuming videos at the place they were left off at.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan? StartupPosition
        {
            get { return (TimeSpan?)GetValue(StartupPositionProperty); }
            set { SetValue(StartupPositionProperty, value); }
        }
        #endregion

        #region MediaEndedBehavior
        /// <summary>
        /// Identifies the MediaEndedBehavior dependency property.
        /// </summary>
        public static readonly DependencyProperty MediaEndedBehaviorProperty = RegisterDependencyProperty<MediaEndedBehavior>("MediaEndedBehavior", MediaEndedBehavior.Stop);

        /// <summary>
        /// Gets or sets the desired behavior when the media reaches the end.
        /// Note: This will be ignored if IsLooping = true.
        /// </summary>
        [Category(Categories.Advanced)]
        public MediaEndedBehavior MediaEndedBehavior
        {
            get { return (MediaEndedBehavior)GetValue(MediaEndedBehaviorProperty); }
            set { SetValue(MediaEndedBehaviorProperty, value); }
        }
        #endregion

        #region UpdateInterval

        /// <summary>
        /// Identifies the UpdateInterval dependency property.
        /// </summary>
        public static readonly DependencyProperty UpdateIntervalProperty = RegisterDependencyProperty<TimeSpan>("UpdateInterval", (t, o, n) => t.OnUpdateIntervalChanged(n), TimeSpan.FromMilliseconds(250));

        void OnUpdateIntervalChanged(TimeSpan newValue)
        {
            UpdateTimer.Interval = newValue;
        }

        /// <summary>
        /// Gets or sets the interval that the timeline and other properties affected by the position will change.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan UpdateInterval
        {
            get { return (TimeSpan)GetValue(UpdateIntervalProperty); }
            set { SetValue(UpdateIntervalProperty, value); }
        }

        void UpdateTimer_Tick(object sender, object e)
        {
            OnUpdate();
        }

        #endregion

        #region AvailableCaptions
        /// <summary>
        /// Identifies the AvailableCaptions dependency property.
        /// </summary>
        public static readonly DependencyProperty AvailableCaptionsProperty = RegisterDependencyProperty<List<Caption>>("AvailableCaptions");

        /// <summary>
        /// Gets or sets the list of captions that can be chosen by the user.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Can be set from xaml")]
        [Category(Categories.Advanced)]
        public List<Caption> AvailableCaptions
        {
            get { return (List<Caption>)GetValue(AvailableCaptionsProperty); }
        }

        ICollection<Caption> IMediaSource.AvailableCaptions
        {
            get { return AvailableCaptions; }
        }
        #endregion

        #region SelectedCaption
        /// <summary>
        /// Identifies the SelectedCaption dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedCaptionProperty = RegisterDependencyProperty<Caption>("SelectedCaption", (t, o, n) => t.OnSelectedCaptionChanged(o, n));

        void OnSelectedCaptionChanged(Caption oldValue, Caption newValue)
        {
            OnSelectedCaptionChanged(new RoutedPropertyChangedEventArgs<Caption>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the selected caption stream.
        /// </summary>
        [Category(Categories.Advanced)]
        public Caption SelectedCaption
        {
            get { return (Caption)GetValue(SelectedCaptionProperty); }
            set { SetValue(SelectedCaptionProperty, value); }
        }
        #endregion

        #region AvailableAudioStreams
        /// <summary>
        /// Identifies the AvailableAudioStreams dependency property.
        /// </summary>
        public static readonly DependencyProperty AvailableAudioStreamsProperty = RegisterDependencyProperty<List<AudioStream>>("AvailableAudioStreams");

        /// <summary>
        /// Gets or sets the list of AudioStreams that can be chosen by the user.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Can be set from xaml")]
        [Category(Categories.Advanced)]
        public List<AudioStream> AvailableAudioStreams
        {
            get { return GetValue(AvailableAudioStreamsProperty) as List<AudioStream>; }
        }

        ICollection<AudioStream> IMediaSource.AvailableAudioStreams
        {
            get { return AvailableAudioStreams; }
        }
        #endregion

        #region SelectedAudioStream
        /// <summary>
        /// Identifies the SelectedAudioStream dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedAudioStreamProperty = RegisterDependencyProperty<AudioStream>("SelectedAudioStream", (t, o, n) => t.OnSelectedAudioStreamChanged(o, n));

        void OnSelectedAudioStreamChanged(AudioStream oldValue, AudioStream newValue)
        {
            var eventArgs = new SelectedAudioStreamChangedEventArgs(oldValue, newValue);
            OnSelectedAudioStreamChanged(eventArgs);
            if (!eventArgs.Handled)
            {
                AudioStreamIndex = AvailableAudioStreams.IndexOf(newValue);
            }
        }

        /// <summary>
        /// Gets or sets the selected AudioStream stream.
        /// </summary>
        [Category(Categories.Advanced)]
        public AudioStream SelectedAudioStream
        {
            get { return (AudioStream)GetValue(SelectedAudioStreamProperty); }
            set { SetValue(SelectedAudioStreamProperty, value); }
        }
        #endregion

        #region IsLive
        /// <summary>
        /// Identifies the IsLive dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLiveProperty = RegisterDependencyProperty<bool>("IsLive", (t, o, n) => t.OnIsLiveChanged(o, n), false);

        void OnIsLiveChanged(bool oldValue, bool newValue)
        {
            OnIsLiveChanged(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
            NotifyIsGoLiveAllowedChanged();
        }

        /// <summary>
        /// Gets or sets a value indicating whether the media is Live (vs. VOD).
        /// </summary>
        [Category(Categories.Common)]
        public bool IsLive
        {
            get { return (bool)GetValue(IsLiveProperty); }
            set { SetValue(IsLiveProperty, value); }
        }
        #endregion

#if SILVERLIGHT

        #region BufferingTime
        /// <summary>
        /// Identifies the BufferingTime dependency property.
        /// </summary>
        public static readonly DependencyProperty BufferingTimeProperty = RegisterDependencyProperty<TimeSpan>("BufferingTime", (t, o, n) => t.OnBufferingTimeChanged(n), DefaultBufferingTime);

        void OnBufferingTimeChanged(TimeSpan newValue)
        {
            _BufferingTime = newValue;
        }

        /// <summary>
        /// Gets or sets the amount of time to buffer. The default value is the recommended value for optimal performance.
        /// </summary>
        [Category(Categories.Advanced)]
        public TimeSpan BufferingTime
        {
            get { return (TimeSpan)GetValue(BufferingTimeProperty); }
            set { SetValue(BufferingTimeProperty, value); }
        }
        #endregion

        #region LicenseAcquirer

        /// <summary>
        /// Gets or sets the System.Windows.Media.LicenseAcquirer associated with the MediaElement. The LicenseAcquirer handles acquiring licenses for DRM encrypted content.
        /// </summary>
        public LicenseAcquirer LicenseAcquirer
        {
            get { return _LicenseAcquirer; }
            set { _LicenseAcquirer = value; }
        }

        #endregion

#if !WINDOWS_PHONE
        #region Attributes
        /// <summary>
        /// Identifies the Attributes dependency property.
        /// </summary>
        public static readonly DependencyProperty AttributesProperty = RegisterDependencyProperty<Dictionary<string, string>>("Attributes", DefaultAttributes);

        /// <summary>
        /// Gets the collection of attributes that corresponds to the current entry in the ASX file that Source is set to.
        /// </summary>
        [Category(Categories.Info)]
        public Dictionary<string, string> Attributes
        {
            get { return (Dictionary<string, string>)GetValue(AttributesProperty); }
        }
        #endregion

        #region IsDecodingOnGPU
        /// <summary>
        /// Identifies the IsDecodingOnGPU dependency property.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "MediaElement compatibility")]
        public static readonly DependencyProperty IsDecodingOnGPUProperty = RegisterDependencyProperty<bool>("IsDecodingOnGPU", DefaultIsDecodingOnGPU);


        /// <summary>
        /// Gets a value that indicates whether the media is being decoded in hardware.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", Justification = "MediaElement compatibility")]
        [Category(Categories.Info)]
        public bool IsDecodingOnGPU
        {
            get { return (bool)GetValue(IsDecodingOnGPUProperty); }
        }
        #endregion
#endif

        #region DroppedFramesPerSecond
        /// <summary>
        /// Identifies the DroppedFramesPerSecond dependency property.
        /// </summary>
        public static readonly DependencyProperty DroppedFramesPerSecondProperty = RegisterDependencyProperty<double>("DroppedFramesPerSecond", DefaultDroppedFramesPerSecond);

        /// <summary>
        /// Gets the number of frames per second being dropped by the media.
        /// </summary>
        [Category(Categories.Info)]
        public double DroppedFramesPerSecond
        {
            get { return (double)GetValue(DroppedFramesPerSecondProperty); }
        }
        #endregion

        #region RenderedFramesPerSecond

        /// <summary>
        /// Identifies the RenderedFramesPerSecond dependency property.
        /// </summary>
        public static readonly DependencyProperty RenderedFramesPerSecondProperty = RegisterDependencyProperty<double>("RenderedFramesPerSecond", DefaultRenderedFramesPerSecond);

        /// <summary>
        /// Gets the number of frames per second being rendered by the media.
        /// </summary>
        [Category(Categories.Info)]
        public double RenderedFramesPerSecond
        {
            get { return (double)GetValue(RenderedFramesPerSecondProperty); }
        }

        #endregion

        #region DefaultPlaybackRate
        /// <summary>
        /// Identifies the DefaultPlaybackRate dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultPlaybackRateProperty = RegisterDependencyProperty<double>("DefaultPlaybackRate", 1.0);

        /// <summary>
        /// Gets or sets the default playback rate for the media. The playback rate applies when the user is not using fast forward or reverse.
        /// </summary>
        [Category(Categories.Common)]
        public double DefaultPlaybackRate
        {
            get { return (double)GetValue(DefaultPlaybackRateProperty); }
            set { SetValue(DefaultPlaybackRateProperty, value); }
        }
        #endregion

        #region IsLooping
        /// <summary>
        /// Identifies the IsLooping dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLoopingProperty = RegisterDependencyProperty<bool>("IsLooping", false);

        /// <summary>
        /// Gets or sets a value that describes whether the media source should seek to the start after reaching its end. Set to true to loop the media and play continuously.
        /// If set to true, MediaEndedBehavior will have no effect.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsLooping
        {
            get { return (bool)GetValue(IsLoopingProperty); }
            set { SetValue(IsLoopingProperty, value); }
        }

        #endregion

        #region PosterSource
        /// <summary>
        /// Identifies the PosterSource dependency property.
        /// </summary>
        public static readonly DependencyProperty PosterSourceProperty = RegisterDependencyProperty<ImageSource>("PosterSource");

        /// <summary>
        /// Gets or sets an ImageSource to be displayed before the content is loaded. Only shows until MediaOpened fires and is hidden when the first frame of the video is available.
        /// Note: This will not show when waiting for AutoPlay to be set to true since MediaOpened still fires.
        /// </summary>
        [Category(Categories.Common)]
        public ImageSource PosterSource
        {
            get { return (ImageSource)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        #endregion

        #region Stretch
        /// <summary>
        /// Identifies the Stretch dependency property.
        /// </summary>
        public static readonly DependencyProperty StretchProperty = RegisterDependencyProperty<Stretch>("Stretch", (t, o, n) => t.OnStretchChanged(o, n), DefaultStretch);

        void OnStretchChanged(Stretch oldValue, Stretch newValue)
        {
            _Stretch = newValue;
            OnStretchChanged(new RoutedPropertyChangedEventArgs<Stretch>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets a Stretch value that describes how to fill the destination rectangle. The default is Uniform.
        /// You can also cycle through the enumerations by calling the CycleDisplayMode method.
        /// </summary>
        [Category(Categories.Appearance)]
        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }
        #endregion
#else

        #region MediaExtensionManager
        /// <summary>
        /// Gets or sets the MediaExtensionManager to be used by PlayerFramework plugins.
        /// One will be created on first use if it is not set.
        /// </summary>
        public MediaExtensionManager MediaExtensionManager
        {
            get
            {
                if (mediaExtensionManager == null)
                {
                    mediaExtensionManager = new Windows.Media.MediaExtensionManager();
                }
                return mediaExtensionManager;
            }
            set
            {
                mediaExtensionManager = value;
            }
        }
        #endregion

        #region AspectRatioWidth

        /// <summary>
        /// Identifies the AspectRatioWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty AspectRatioWidthProperty = RegisterDependencyProperty<int>("AspectRatioWidth", DefaultAspectRatioWidth);

        /// <summary>
        /// Gets the width portion of the media's native aspect ratio.
        /// </summary>
        [Category(Categories.Info)]
        public int AspectRatioWidth
        {
            get { return (int)GetValue(AspectRatioWidthProperty); }
        }

        #endregion

        #region AspectRatioHeight

        /// <summary>
        /// Identifies the AspectRatioHeight dependency property.
        /// </summary>
        public static readonly DependencyProperty AspectRatioHeightProperty = RegisterDependencyProperty<int>("AspectRatioHeight", DefaultAspectRatioHeight);

        /// <summary>
        /// Gets the height portion of the media's native aspect ratio.
        /// </summary>
        [Category(Categories.Info)]
        public int AspectRatioHeight
        {
            get { return (int)GetValue(AspectRatioHeightProperty); }
        }

        #endregion

        #region AudioCategory

        /// <summary>
        /// Identifies the AudioCategory dependency property.
        /// </summary>
        public static readonly DependencyProperty AudioCategoryProperty = RegisterDependencyProperty<AudioCategory>("AudioCategory", (t, o, n) => t.OnAudioCategoryChanged(n), DefaultAudioCategory);

        void OnAudioCategoryChanged(AudioCategory newValue)
        {
            _AudioCategory = newValue;
        }

        /// <summary>
        /// Gets or sets a value that describes the purpose of the audio information in an audio stream.
        /// </summary>
        [Category(Categories.Advanced)]
        public AudioCategory AudioCategory
        {
            get { return (AudioCategory)GetValue(AudioCategoryProperty); }
            set { SetValue(AudioCategoryProperty, value); }
        }

        #endregion

        #region AudioDeviceType

        /// <summary>
        /// Identifies the AudioDeviceType dependency property.
        /// </summary>
        public static readonly DependencyProperty AudioDeviceTypeProperty = RegisterDependencyProperty<AudioDeviceType>("AudioDeviceType", (t, o, n) => t.OnAudioDeviceTypeChanged(n), DefaultAudioDeviceType);

        void OnAudioDeviceTypeChanged(AudioDeviceType newValue)
        {
            _AudioDeviceType = newValue;
        }

        /// <summary>
        /// Gets or sets a value that describes the primary usage of the device that is being used to play back audio.
        /// </summary>
        [Category(Categories.Advanced)]
        public AudioDeviceType AudioDeviceType
        {
            get { return (AudioDeviceType)GetValue(AudioDeviceTypeProperty); }
            set { SetValue(AudioDeviceTypeProperty, value); }
        }

        #endregion

        #region PlayToSource

        /// <summary>
        /// Identifies the PlayToSource dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayToSourceProperty = RegisterDependencyProperty<PlayToSource>("PlayToSource", (t, o, n) => t.OnPlayToSourceChanged(o, n), DefaultPlayToSource);

        partial void OnPlayToSourceChanged(PlayToSource oldValue, PlayToSource newValue);

        /// <summary>
        /// Gets the information that is transmitted if the MediaElement is used for a "PlayTo" scenario.
        /// </summary>
        [Category(Categories.Info)]
        public PlayToSource PlayToSource
        {
            get { return (PlayToSource)GetValue(PlayToSourceProperty); }
        }

        #endregion

        #region DefaultPlaybackRate

        /// <summary>
        /// Identifies the DefaultPlaybackRate dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultPlaybackRateProperty = RegisterDependencyProperty<double>("DefaultPlaybackRate", (t, o, n) => t.OnDefaultPlaybackRateChanged(n), DefaultDefaultPlaybackRate);

        void OnDefaultPlaybackRateChanged(double newValue)
        {
            _DefaultPlaybackRate = newValue;
        }

        /// <summary>
        /// Gets or sets the default playback rate for the media engine. The playback rate applies when the user is not using fast foward or reverse.
        /// </summary>
        [Category(Categories.Common)]
        public double DefaultPlaybackRate
        {
            get { return (double)GetValue(DefaultPlaybackRateProperty); }
            set { SetValue(DefaultPlaybackRateProperty, value); }
        }

        #endregion

        #region IsAudioOnly

        /// <summary>
        /// Identifies the IsAudioOnly dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAudioOnlyProperty = RegisterDependencyProperty<bool>("IsAudioOnly", DefaultIsAudioOnly);

        /// <summary>
        /// Gets or sets a value that describes whether the media source loaded in the media engine should seek to the start after reaching its end.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsAudioOnly
        {
            get { return (bool)GetValue(IsAudioOnlyProperty); }
        }

        #endregion

        #region IsLooping

        /// <summary>
        /// Identifies the IsLooping dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLoopingProperty = RegisterDependencyProperty<bool>("IsLooping", (t, o, n) => t.OnIsLoopingChanged(n), DefaultIsLooping);

        void OnIsLoopingChanged(bool newValue)
        {
            _IsLooping = newValue;
        }

        /// <summary>
        /// Gets or sets a value that describes whether the media source loaded in the media engine should seek to the start after reaching its end.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsLooping
        {
            get { return (bool)GetValue(IsLoopingProperty); }
            set { SetValue(IsLoopingProperty, value); }
        }

        #endregion

        #region PosterSource

        /// <summary>
        /// Identifies the PosterSource dependency property.
        /// </summary>
        public static readonly DependencyProperty PosterSourceProperty = RegisterDependencyProperty<ImageSource>("PosterSource", (t, o, n) => t.OnPosterSourceChanged(n), DefaultPosterSource);

        void OnPosterSourceChanged(ImageSource newValue)
        {
            _PosterSource = newValue;
        }

        /// <summary>
        /// Gets or sets the source used for a default poster effect that is used as background in the default template for MediaPlayer.
        /// </summary>
        [Category(Categories.Common)]
        public ImageSource PosterSource
        {
            get { return (ImageSource)GetValue(PosterSourceProperty); }
            set { SetValue(PosterSourceProperty, value); }
        }

        #endregion

        #region ActualStereo3DVideoPackingMode

        /// <summary>
        /// Identifies the ActualStereo3DVideoPackingMode dependency property.
        /// </summary>
        public static readonly DependencyProperty ActualStereo3DVideoPackingModeProperty = RegisterDependencyProperty<Stereo3DVideoPackingMode>("ActualStereo3DVideoPackingMode", DefaultActualStereo3DVideoPackingMode);

        /// <summary>
        /// Gets a value that reports whether the current source media is a stereo 3-D video media file.
        /// </summary>
        [Category(Categories.Advanced)]
        public Stereo3DVideoPackingMode ActualStereo3DVideoPackingMode
        {
            get { return (Stereo3DVideoPackingMode)GetValue(ActualStereo3DVideoPackingModeProperty); }
        }

        #endregion

        #region Stereo3DVideoPackingMode

        /// <summary>
        /// Identifies the Stereo3DVideoPackingMode dependency property.
        /// </summary>
        public static readonly DependencyProperty Stereo3DVideoPackingModeProperty = RegisterDependencyProperty<Stereo3DVideoPackingMode>("Stereo3DVideoPackingMode", (t, o, n) => t.OnStereo3DVideoPackingModeChanged(n), DefaultStereo3DVideoPackingMode);

        void OnStereo3DVideoPackingModeChanged(Stereo3DVideoPackingMode newValue)
        {
            _Stereo3DVideoPackingMode = newValue;
        }

        /// <summary>
        /// Gets or sets an enumeration value that determines the stereo 3-D video frame-packing mode for the current media source.
        /// </summary>
        [Category(Categories.Advanced)]
        public Stereo3DVideoPackingMode Stereo3DVideoPackingMode
        {
            get { return (Stereo3DVideoPackingMode)GetValue(Stereo3DVideoPackingModeProperty); }
            set { SetValue(Stereo3DVideoPackingModeProperty, value); }
        }
        #endregion

        #region Stereo3DVideoRenderMode

        /// <summary>
        /// Identifies the Stereo3DVideoRenderMode dependency property.
        /// </summary>
        public static readonly DependencyProperty Stereo3DVideoRenderModeProperty = RegisterDependencyProperty<Stereo3DVideoRenderMode>("Stereo3DVideoRenderMode", (t, o, n) => t.OnStereo3DVideoRenderModeChanged(n), DefaultStereo3DVideoRenderMode);

        void OnStereo3DVideoRenderModeChanged(Stereo3DVideoRenderMode newValue)
        {
            _Stereo3DVideoRenderMode = newValue;
        }

        /// <summary>
        /// Gets or sets an enumeration value that determines the stereo 3-D video render mode for the current media source.
        /// </summary>
        [Category(Categories.Advanced)]
        public Stereo3DVideoRenderMode Stereo3DVideoRenderMode
        {
            get { return (Stereo3DVideoRenderMode)GetValue(Stereo3DVideoRenderModeProperty); }
            set { SetValue(Stereo3DVideoRenderModeProperty, value); }
        }
        #endregion

        #region IsStereo3DVideo

        /// <summary>
        /// Identifies the IsStereo3DVideo dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStereo3DVideoProperty = RegisterDependencyProperty<bool>("IsStereo3DVideo", DefaultIsStereo3DVideo);

        /// <summary>
        /// Gets a value that reports whether the current source media is a stereo 3-D video media file.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool IsStereo3DVideo
        {
            get { return (bool)GetValue(IsStereo3DVideoProperty); }
        }

        #endregion

        #region RealTimePlayback

        /// <summary>
        /// Identifies the RealTimePlayback dependency property.
        /// </summary>
        public static readonly DependencyProperty RealTimePlaybackProperty = RegisterDependencyProperty<bool>("RealTimePlayback", (t, o, n) => t.OnRealTimePlaybackChanged(n), DefaultRealTimePlayback);

        void OnRealTimePlaybackChanged(bool newValue)
        {
            _RealTimePlayback = newValue;
        }

        /// <summary>
        /// Gets or sets a value that configures the MediaElement for real-time communications scenarios.
        /// </summary>
        [Category(Categories.Advanced)]
        public bool RealTimePlayback
        {
            get { return (bool)GetValue(RealTimePlaybackProperty); }
            set { SetValue(RealTimePlaybackProperty, value); }
        }

        #endregion

        #region ProtectionManager

        /// <summary>
        /// Identifies the ProtectionManager dependency property.
        /// </summary>
        public static readonly DependencyProperty ProtectionManagerProperty = RegisterDependencyProperty<MediaProtectionManager>("ProtectionManager", (t, o, n) => t.OnProtectionManagerChanged(n), DefaultProtectionManager);

        void OnProtectionManagerChanged(MediaProtectionManager newValue)
        {
            _ProtectionManager = newValue;
        }

        /// <summary>
        /// Gets or sets the dedicated object for media content protection that is associated with this MediaElement.
        /// </summary>
        [Category(Categories.Advanced)]
        public MediaProtectionManager ProtectionManager
        {
            get { return (MediaProtectionManager)GetValue(ProtectionManagerProperty); }
            set { SetValue(ProtectionManagerProperty, value); }
        }

        #endregion

#endif

        #region AudioStreamCount
        /// <summary>
        /// Identifies the AudioStreamCount dependency property.
        /// </summary>
        public static readonly DependencyProperty AudioStreamCountProperty = RegisterDependencyProperty<int>("AudioStreamCount", (t, o, n) => t.OnAudioStreamCountChanged(), DefaultAudioStreamCount);

        void OnAudioStreamCountChanged()
        {
            NotifyIsAudioSelectionAllowedChanged();
        }

        /// <summary>
        /// Gets the number of audio streams available in the current media file. The default is 0.
        /// </summary>
        [Category(Categories.Info)]
        public int AudioStreamCount
        {
            get { return (int)GetValue(AudioStreamCountProperty); }
        }
        #endregion

        #region AudioStreamIndex
        /// <summary>
        /// Identifies the AudioStreamIndex dependency property.
        /// </summary>
        public static readonly DependencyProperty AudioStreamIndexProperty = RegisterDependencyProperty<int?>("AudioStreamIndex", (t, o, n) => t.OnAudioStreamIndexChanged(o, n), DefaultAudioStreamIndex);

        void OnAudioStreamIndexChanged(int? oldValue, int? newValue)
        {
            _AudioStreamIndex = newValue;
            OnAudioStreamIndexChanged(new RoutedPropertyChangedEventArgs<int?>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets the index of the audio stream that plays along with the video component.
        /// The collection of audio streams is composed at run time and represents all audio streams available within the media file.
        /// The index can be unspecified, in which case the value is null.
        /// </summary>
        [Category(Categories.Advanced)]
        public int? AudioStreamIndex
        {
            get { return (int?)GetValue(AudioStreamIndexProperty); }
            set { SetValue(AudioStreamIndexProperty, value); }
        }
        #endregion

        #region AutoPlay
        /// <summary>
        /// Identifies the AutoPlay dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoPlayProperty = RegisterDependencyProperty<bool>("AutoPlay", (t, o, n) => t.OnAutoPlayChanged(n), DefaultAutoPlay);

        void OnAutoPlayChanged(bool newValue)
        {
            // wait until after the template is applied in order to give the dev a chance to set AllowMediaStartingDeferrals
            RegisterApplyTemplateAction(() =>
            {
                // Note: by default we do not set autoplay on the mediaelement. We need to control this ourselves in order to support pre-roll ads
                if (!AllowMediaStartingDeferrals)
                {
                    _AutoPlay = newValue;
                }
            });
        }

        /// <summary>
        /// Gets or sets a value that indicates whether media will begin playback automatically when the Source property is set.
        /// Setting to false will still open, download and buffer the media but will pause on the first frame.
        /// </summary>
        [Category(Categories.Common)]
        public bool AutoPlay
        {
            get { return (bool)GetValue(AutoPlayProperty); }
            set { SetValue(AutoPlayProperty, value); }
        }
        #endregion

        #region BufferingProgress
        /// <summary>
        /// Identifies the BufferingProgress dependency property.
        /// </summary>
        public static readonly DependencyProperty BufferingProgressProperty = RegisterDependencyProperty<double>("BufferingProgress", DefaultBufferingProgress);

        /// <summary>
        /// Gets a value that indicates the current buffering progress.
        /// The amount of buffering that is completed for media content. The value ranges from 0 to 1. Multiply by 100 to obtain a percentage.
        /// </summary>
        [Category(Categories.Info)]
        public double BufferingProgress
        {
            get { return (double)GetValue(BufferingProgressProperty); }
        }
        #endregion

        #region CanPause
        /// <summary>
        /// Identifies the CanPause dependency property.
        /// </summary>
        public static readonly DependencyProperty CanPauseProperty = RegisterDependencyProperty<bool>("CanPause", DefaultCanPause);

        /// <summary>
        /// Gets a value indicating if media can be paused if the Pause() method is called.
        /// </summary>
        [Category(Categories.Info)]
        public bool CanPause
        {
            get { return (bool)GetValue(CanPauseProperty); }
        }
        #endregion

        #region CanSeek
        /// <summary>
        /// Identifies the CanSeek dependency property.
        /// </summary>
        public static readonly DependencyProperty CanSeekProperty = RegisterDependencyProperty<bool>("CanSeek", DefaultCanSeek);

        /// <summary>
        /// Gets a value indicating if media can be repositioned by setting the value of the Position property.
        /// </summary>
        [Category(Categories.Info)]
        public bool CanSeek
        {
            get { return (bool)GetValue(CanSeekProperty); }
        }
        #endregion

        #region Balance
        /// <summary>
        /// Identifies the Balance dependency property.
        /// </summary>
        public static readonly DependencyProperty BalanceProperty = RegisterDependencyProperty<double>("Balance", (t, o, n) => t.OnBalanceChanged(n), DefaultBalance);

        void OnBalanceChanged(double newValue)
        {
            _Balance = newValue;
        }

        /// <summary>
        /// Gets or sets a ratio of volume across stereo speakers. The ratio of volume across speakers in the range between -1 and 1.
        /// </summary>
        [Category(Categories.Common)]
        public double Balance
        {
            get { return (double)GetValue(BalanceProperty); }
            set { SetValue(BalanceProperty, value); }
        }
        #endregion

        #region DownloadProgress
        /// <summary>
        /// Identifies the DownloadProgress dependency property.
        /// </summary>
        public static readonly DependencyProperty DownloadProgressProperty = RegisterDependencyProperty<double>("DownloadProgress", DefaultDownloadProgress);

        /// <summary>
        /// Gets a percentage value indicating the amount of download completed for content located on a remote server.
        /// The value ranges from 0 to 1. Multiply by 100 to obtain a percentage.
        /// </summary>
        [Category(Categories.Info)]
        public double DownloadProgress
        {
            get { return (double)GetValue(DownloadProgressProperty); }
        }
        #endregion

        #region DownloadProgressOffset
        /// <summary>
        /// Identifies the DownloadProgressOffset dependency property.
        /// </summary>
        public static readonly DependencyProperty DownloadProgressOffsetProperty = RegisterDependencyProperty<double>("DownloadProgressOffset", DefaultDownloadProgressOffset);

        /// <summary>
        /// Gets the offset of the download progress.
        /// </summary>
        [Category(Categories.Info)]
        public double DownloadProgressOffset
        {
            get { return (double)GetValue(DownloadProgressOffsetProperty); }
        }
        #endregion

        #region IsMuted
        /// <summary>
        /// Identifies the IsMuted dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMutedProperty = RegisterDependencyProperty<bool>("IsMuted", (t, o, n) => t.OnIsMutedChanged(o, n), DefaultIsMuted);

        void OnIsMutedChanged(bool oldValue, bool newValue)
        {
            _IsMuted = newValue;
            OnIsMutedChanged(new RoutedPropertyChangedEventArgs<bool>(oldValue, newValue));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the audio is muted.
        /// </summary>
        [Category(Categories.Common)]
        public bool IsMuted
        {
            get { return (bool)GetValue(IsMutedProperty); }
            set { SetValue(IsMutedProperty, value); }
        }
        #endregion

        #region NaturalDuration
        /// <summary>
        /// Identifies the NaturalDuration dependency property.
        /// </summary>
        public static readonly DependencyProperty NaturalDurationProperty = RegisterDependencyProperty<Duration>("NaturalDuration", (t, o, n) => t.OnNaturalDurationChanged(n), DefaultNaturalDuration);

        void OnNaturalDurationChanged(Duration newValue)
        {
            if (newValue.HasTimeSpan && newValue != TimeSpan.MaxValue)
            {
                EndTime = StartTime.Add(newValue.TimeSpan); // this will trigger Duration to get set.
            }
        }

        /// <summary>
        /// The natural duration of the media. The default value is Duration.Automatic, which is the value held if you query this property before MediaOpened.
        /// </summary>
        [Category(Categories.Info)]
        public Duration NaturalDuration
        {
            get { return (Duration)GetValue(NaturalDurationProperty); }
        }
        #endregion

        #region NaturalVideoHeight
        /// <summary>
        /// Identifies the NaturalVideoHeight dependency property.
        /// </summary>
        public static readonly DependencyProperty NaturalVideoHeightProperty = RegisterDependencyProperty<int>("NaturalVideoHeight", DefaultNaturalVideoHeight);

        /// <summary>
        /// Gets the height of the video associated with the media.
        /// The height of the video that is associated with the media, in pixels. Audio files will return 0.
        /// </summary>
        [Category(Categories.Info)]
        public int NaturalVideoHeight
        {
            get { return (int)GetValue(NaturalVideoHeightProperty); }
        }
        #endregion

        #region NaturalVideoWidth
        /// <summary>
        /// Identifies the NaturalVideoWidth dependency property.
        /// </summary>
        public static readonly DependencyProperty NaturalVideoWidthProperty = RegisterDependencyProperty<int>("NaturalVideoWidth", DefaultNaturalVideoWidth);

        /// <summary>
        /// Gets the width of the video associated with the media.
        /// The width of the video associated with the media, in pixels. Audio files will return 0.
        /// </summary>
        [Category(Categories.Info)]
        public int NaturalVideoWidth
        {
            get { return (int)GetValue(NaturalVideoWidthProperty); }
        }
        #endregion

        #region PlaybackRate
        /// <summary>
        /// Identifies the PlaybackRate dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaybackRateProperty = RegisterDependencyProperty<double>("PlaybackRate", (t, o, n) => t.OnPlaybackRateChanged(n), DefaultRate);

        void OnPlaybackRateChanged(double newValue)
        {
            _PlaybackRate = newValue;
        }

        /// <summary>
        /// Gets or sets the playback rate of the media. Use this property to directly control features like fast forward, reverse, and slow motion.
        /// IncreasePlaybackRate, DecreasePlaybackRate, and IsSlowMotion are also available to help control this.
        /// </summary>
        [Category(Categories.Common)]
        public double PlaybackRate
        {
            get { return (double)GetValue(PlaybackRateProperty); }
            set { SetValue(PlaybackRateProperty, value); }
        }
        #endregion

        #region Position
        /// <summary>
        /// Identifies the Position dependency property.
        /// </summary>
        public static readonly DependencyProperty PositionProperty = RegisterDependencyProperty<TimeSpan>("Position", (t, o, n) => t.OnPositionChanged(n), DefaultPosition);

        void OnPositionChanged(TimeSpan newValue)
        {
            _Position = newValue;
        }

        /// <summary>
        /// Gets or sets the current position of progress through the media's playback time (or the amount of time since the beginning of the media).
        /// </summary>
        [Category(Categories.Common)]
        public TimeSpan Position
        {
            get { return (TimeSpan)GetValue(PositionProperty); }
            set { SetValue(PositionProperty, value); }
        }
        #endregion

        #region CurrentState
        /// <summary>
        /// Identifies the CurrentState dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentStateProperty = RegisterDependencyProperty<MediaElementState>("CurrentState", DefaultCurrentState);

        /// <summary>
        /// Gets the status of the MediaElement.
        /// The state can be one of the following (as defined in the MediaElementState enumeration):
        /// Buffering, Closed, Opening, Paused, Playing, Stopped.
        /// </summary>
        [Category(Categories.Info)]
        public MediaElementState CurrentState
        {
            get { return (MediaElementState)GetValue(CurrentStateProperty); }
        }
        #endregion

        #region Source
        /// <summary>
        /// Identifies the Source dependency property.
        /// </summary>
        public static readonly DependencyProperty SourceProperty = RegisterDependencyProperty<Uri>("Source", (t, o, n) => t.OnSourceChanged(n), DefaultSource);

        /// <summary>
        /// Gets or sets a media source on the MediaElement.
        /// A string that specifies the source of the element, as a Uniform Resource Identifier (URI).
        /// </summary>
        [Category(Categories.Common)]
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        void OnSourceChanged(Uri newValue)
        {
            if (!IsInDesignMode)
            {
                SetSource(newValue);
            }
        }

        void SetSource(Uri newValue)
        {
            RegisterApplyTemplateAction(async () =>
            {
                if (AutoLoad || newValue == null)
                {
                    if (newValue == null || await OnMediaLoadingAsync())
                    {
                        if (IsMediaLoaded)
                        {
                            OnMediaClosed(new RoutedEventArgs());
                        }
                        _Source = newValue;
                        IsMediaLoaded = newValue != null;
                    }
                }
                else
                {
                    PendingLoadAction = () => SetSource(newValue);
                }
            });
        }

        private async Task<bool> OnMediaLoadingAsync()
        {
            SetValue(PlayerStateProperty, PlayerState.Loading);

            var deferrableOperation = new MediaPlayerDeferrableOperation(cts);
            if (MediaLoading != null) MediaLoading(this, new MediaPlayerDeferrableEventArgs(deferrableOperation));
            bool[] result;
            try
            {
                result = await deferrableOperation.Task;
            }
            catch (OperationCanceledException) { return false; }
            if (!result.Any() || result.All(r => r))
            {
                if (AllowMediaStartingDeferrals)
                {
                    _AutoPlay = false;
                }
                return true;
            }
            else
            {
                SetValue(PlayerStateProperty, PlayerState.Unloaded);
                return false;
            }
        }
        #endregion

        /// <summary>
        /// Gets or sets whether the MediaStarting event supports deferrals before playback begins. Note: without this, pre-roll ads will not work. 
        /// Interally, this causes MediaElement.AutoPlay to be set to false and Play to be called automatically from the MediaOpened event (if AutoPlay is true).
        /// </summary>
        public bool AllowMediaStartingDeferrals { get; set; }

        #region Volume
        /// <summary>
        /// Identifies the Volume dependency property.
        /// </summary>
        public static readonly DependencyProperty VolumeProperty = RegisterDependencyProperty<double>("Volume", (t, o, n) => t.OnVolumeChanged(o, n), DefaultVolume);

        void OnVolumeChanged(double oldValue, double newValue)
        {
            _Volume = newValue;
#if SILVERLIGHT
            OnVolumeChanged(new RoutedPropertyChangedEventArgs<double>(oldValue, newValue));
#endif
        }

        /// <summary>
        /// Gets or sets the media's volume.
        /// The media's volume represented on a linear scale between 0 and 1. The default is 0.5.
        /// </summary>
        [Category(Categories.Common)]
        public double Volume
        {
            get { return (double)GetValue(VolumeProperty); }
            set { SetValue(VolumeProperty, value); }
        }

        #endregion

        void OnInvokeCaptionSelection(RoutedEventArgs e)
        {
            if (CaptionsInvoked != null) CaptionsInvoked(this, e);
        }

        void OnInvokeAudioSelection(RoutedEventArgs e)
        {
            if (AudioSelectionInvoked != null) AudioSelectionInvoked(this, e);
        }

        void OnSeekToLive(RoutedEventArgs e)
        {
            if (GoLive != null) GoLive(this, e);
        }

        void OnMediaFailed(ExceptionRoutedEventArgs e)
        {
            UpdateTimer.Stop();
            if (MediaFailed != null) MediaFailed(this, e);

            SetValue(PlayerStateProperty, PlayerState.Failed);
        }

        async void OnMediaEnded(MediaPlayerActionEventArgs e)
        {
            UpdateTimer.Stop();

#if WINDOWS_PHONE
            MediaPlayerState playerState = null;
#endif

            SetValue(PlayerStateProperty, PlayerState.Ending);
            var deferrableOperation = new MediaPlayerDeferrableOperation(cts);
            if (MediaEnding != null)
            {
                MediaEnding(this, new MediaPlayerDeferrableEventArgs(deferrableOperation));
                if (deferrableOperation.DeferralsExist)
                {
#if WINDOWS_PHONE
                    playerState = GetState();
                    Disconnect();
#endif
                    try
                    {
                        await deferrableOperation.Task;
                    }
                    catch (OperationCanceledException) { return; }
                    // HACK: this lets all other operations awaiting this task finish first. Important for postrolls. Might be a better way to do this.
#if SILVERLIGHT
                    await Dispatcher.InvokeAsync(() => { });
#else
                    await Dispatcher.BeginInvoke(() => { });
#endif
                }
            }

#if WINDOWS_PHONE
            Reconnect(null);
#endif

            if (MediaEnded != null) MediaEnded(this, e);

            if (!e.Handled)
            {
#if WINDOWS_PHONE
                if (playerState != null)
                {
                    Disconnect();
                    playerState.IsPaused = !IsLooping;
                    if (IsLooping || MediaEndedBehavior != MediaEndedBehavior.Manual)
                    {
                        playerState.Position = TimeSpan.Zero;
                    }
                    await Reconnect(playerState);
                    SetValue(PlayerStateProperty, PlayerState.Started);
                    if (MediaEndedBehavior == MediaEndedBehavior.Stop)
                    {
                        Stop();
                    }
                }
                else
#endif
                {
                    SetValue(PlayerStateProperty, PlayerState.Started);
#if SILVERLIGHT
                    if (IsLooping)
                    {
                        Position = TimeSpan.Zero;
                        Play();
                    }
#endif
                    if (!IsLooping)
                    {
                        switch (MediaEndedBehavior)
                        {
                            case MediaEndedBehavior.Manual:
                                // do nothing
                                break;
                            case MediaEndedBehavior.Stop:
                                Stop();
                                break;
                            case MediaEndedBehavior.Reset:
                                Position = StartTime;
                                break;
                        }
                    }
                }
            }
        }

        void OnMarkerReached(TimelineMarkerRoutedEventArgs e)
        {
            if (MarkerReached != null) MarkerReached(this, e);
        }

        void OnDownloadProgressChanged(RoutedEventArgs e)
        {
            if (DownloadProgressChanged != null) DownloadProgressChanged(this, e);
        }

        void OnBufferingProgressChanged(RoutedEventArgs e)
        {
            if (BufferingProgressChanged != null) BufferingProgressChanged(this, e);
        }

        MediaElementState stateWithoutBufferg = MediaElementState.Closed;
        void OnCurrentStateChanged(RoutedEventArgs e)
        {
#if WINDOWS_PHONE
            // WP7 doesn't update these in the MediaOpened event
            SetValue(CanPauseProperty, _CanPause);
            SetValue(CanSeekProperty, _CanSeek);
#endif

#if NETFX_CORE
            if (CurrentState == MediaElementState.Opening)
            {
                SetValue(PlayToSourceProperty, _PlayToSource);
            }
            else if (_CurrentState == MediaElementState.Closed)
            {
                SetValue(PlayToSourceProperty, DefaultPlayToSource);
            }
#endif

            if (CurrentState == MediaElementState.Paused && UpdateTimer.IsEnabled)
            {
                UpdateTimer.Stop();
            }
            else if (CurrentState == MediaElementState.Playing && !UpdateTimer.IsEnabled)
            {
                UpdateTimer.Start();
            }

            if (CurrentState != MediaElementState.Buffering)
            {
                stateWithoutBufferg = CurrentState;
                NotifyIsPlayResumeAllowedChanged();
                NotifyIsPauseAllowedChanged();
                NotifyIsStopAllowedChanged();
                NotifyIsReplayAllowedChanged();
                NotifyIsRewindAllowedChanged();
                NotifyIsFastForwardAllowedChanged();
                NotifyIsSlowMotionAllowedChanged();
                if (CurrentStateChangedBufferingIgnored != null) CurrentStateChangedBufferingIgnored(this, e);
            }
            NotifyIsSeekAllowedChanged();
            NotifyIsSkipPreviousAllowedChanged();
            NotifyIsSkipNextAllowedChanged();
            NotifyIsSkipBackAllowedChanged();
            NotifyIsSkipAheadAllowedChanged();
            NotifyIsScrubbingAllowedChanged();
            if (CurrentStateChanged != null) CurrentStateChanged(this, e);
        }

#if SILVERLIGHT
        void OnLogReady(LogReadyRoutedEventArgs e)
        {
            if (LogReady != null) LogReady(this, e);
        }

        void OnStretchChanged(RoutedPropertyChangedEventArgs<Stretch> e)
        {
            if (StretchChanged != null) StretchChanged(this, e);
        }
#else
        void OnSeekCompleted(RoutedEventArgs e)
        {
            if (SeekCompleted != null) SeekCompleted(this, e);
        }

        /// <summary>
        /// Performs an async Seek. This can also be accomplished by setting the Position property and waiting for SeekCompleted to fire.
        /// </summary>
        /// <param name="position">The new position to seek to</param>
        /// <param name="timeout">The timeout to await before returning</param>
        /// <returns>The task to await on.</returns>
        public async Task SeekAsync(TimeSpan position, TimeSpan? timeout)
        {
            var tcs = new TaskCompletionSource<RoutedEventArgs>();
            RoutedEventHandler seekCompleted = null;
            seekCompleted = (s, e) => tcs.SetResult(e);
            this.SeekCompleted += seekCompleted;
            Position = position;
            if (timeout.HasValue)
            {
                await Task.WhenAny(tcs.Task, Task.Delay(timeout.Value));
            }
            else
            {
                await tcs.Task;
            }
            this.SeekCompleted -= seekCompleted;
        }
#endif

        /// <summary>
        /// Occurs when the timer updates
        /// </summary>
        protected virtual void OnUpdate()
        {
#if !POSITIONBINDING
            var oldValue = this.Position;
            var newValue = _Position;
            SetValueWithoutCallback(PositionProperty, newValue);
            OnPositionChanged(new RoutedPropertyChangedEventArgs<TimeSpan>(oldValue, newValue));
#endif
#if SILVERLIGHT
            SetValue(RenderedFramesPerSecondProperty, _RenderedFramesPerSecond);
            SetValue(DroppedFramesPerSecondProperty, _DroppedFramesPerSecond);
#endif
            if (UpdateCompleted != null) UpdateCompleted(this, new RoutedEventArgs());
        }

        async void OnMediaOpened(RoutedEventArgs e)
        {
            OnMediaOpened();

            SetValue(PlayerStateProperty, PlayerState.Opened);

            if (MediaOpened != null) MediaOpened(this, e);

            if (mediaRestorePosition.HasValue || (AutoPlay && await OnMediaStartingAsync()))
            {
                mediaRestorePosition = null;
                if (AllowMediaStartingDeferrals)
                {
                    _Play();
                }
                UpdateTimer.Start();
                OnUpdate();   // simulate the timer tick ASAP so everyone can update things.
            }
        }

        private async Task<bool> OnMediaStartingAsync()
        {
            if (PlayerState != PlayerState.Starting)
            {
                SetValue(PlayerStateProperty, PlayerState.Starting);
                var deferrableOperation = new MediaPlayerDeferrableOperation(cts);
                if (MediaStarting != null) MediaStarting(this, new MediaPlayerDeferrableEventArgs(deferrableOperation));
                bool[] result;
                try
                {
                    result = await deferrableOperation.Task;
                }
                catch (OperationCanceledException) { return false; }
                if (!result.Any() || result.All(r => r))
                {
                    SetValue(PlayerStateProperty, PlayerState.Started);
                    if (MediaStarted != null) MediaStarted(this, new RoutedEventArgs());
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Initializes the MediaPlayer once the media has opened but immediately before the MediaOpened event fires.
        /// </summary>
        protected virtual void OnMediaOpened()
        {
            TimeSpan? seekPosition = mediaRestorePosition.HasValue ? mediaRestorePosition : StartupPosition;
            if (seekPosition.HasValue)
            {
                Position = seekPosition.Value;
            }

#if SILVERLIGHT && !WINDOWS_PHONE
            SetValue(AttributesProperty, _Attributes);
            SetValue(IsDecodingOnGPUProperty, _IsDecodingOnGPU);
#elif NETFX_CORE
            SetValue(AspectRatioHeightProperty, _AspectRatioHeight);
            SetValue(AspectRatioWidthProperty, _AspectRatioWidth);
            SetValue(IsStereo3DVideoProperty, _IsStereo3DVideo);
            SetValue(ActualStereo3DVideoPackingModeProperty, _ActualStereo3DVideoPackingMode);
            SetValue(IsAudioOnlyProperty, _IsAudioOnly);
#endif
            SetValue(AudioStreamIndexProperty, _AudioStreamIndex);
            SetValue(AudioStreamCountProperty, _AudioStreamCount);
            SetValue(CanPauseProperty, _CanPause);
            SetValue(CanSeekProperty, _CanSeek);
            SetValue(NaturalDurationProperty, _NaturalDuration);
            SetValue(NaturalVideoHeightProperty, _NaturalVideoHeight);
            SetValue(NaturalVideoWidthProperty, _NaturalVideoWidth);
            SetValue(MediaQualityProperty, NaturalVideoHeight >= 720 ? MediaQuality.HighDefinition : MediaQuality.StandardDefinition);

            PopulateAvailableAudioStreams();
        }

        /// <summary>
        /// Populates the available audio streams from the MediaElement.
        /// </summary>
        protected virtual void PopulateAvailableAudioStreams()
        {
            // only add audio streams if it is empty. Otherwise, it implies the app or a plugin is taking care of this for us.
            if (!AvailableAudioStreams.Any())
            {
                for (int i = 0; i < AudioStreamCount; i++)
                {
#if !SILVERLIGHT
                    var language = GetAudioStreamLanguage(i);
                    string name;
                    if (!string.IsNullOrEmpty(language))
                    {
                        if (Windows.Globalization.Language.IsWellFormed(language))
                        {
                            name = new Windows.Globalization.Language(language).DisplayName;
                        }
                        else
                        {
                            name = language;
                        }
                    }
                    else
                    {
                        name = DefaultAudioStreamName;
                    }
                    var audioStream = new AudioStream(name, language);
#else
                    var audioStream = new AudioStream(DefaultAudioStreamName);
#endif
                    AvailableAudioStreams.Add(audioStream);
                    if (i == AudioStreamIndex.GetValueOrDefault(0))
                    {
                        SelectedAudioStream = audioStream;
                    }
                }
            }
        }

        void OnMediaClosed(RoutedEventArgs e)
        {
            cts.Cancel();
            cts = new CancellationTokenSource(); // reset the cancellation token
            OnMediaClosed();

            if (MediaClosed != null) MediaClosed(this, e);
        }

        /// <summary>
        /// Cleans up the MediaPlayer when the media has closed but immediately before the MediaClosed event fires.
        /// </summary>
        protected virtual void OnMediaClosed()
        {
            IsMediaLoaded = false;
#if SILVERLIGHT && !WINDOWS_PHONE
            SetValue(AttributesProperty, DefaultAttributes);
            SetValue(IsDecodingOnGPUProperty, DefaultIsDecodingOnGPU);
#elif NETFX_CORE
            SetValue(AspectRatioHeightProperty, DefaultAspectRatioHeight);
            SetValue(AspectRatioWidthProperty, DefaultAspectRatioWidth);
#endif
            SetValue(AudioStreamCountProperty, DefaultAudioStreamCount);
            // do not actually push value into MediaElement or it will throw since media is closed.
            SetValueWithoutCallback(AudioStreamIndexProperty, DefaultAudioStreamIndex);
            SetValue(CanPauseProperty, DefaultCanPause);
            SetValue(CanSeekProperty, DefaultCanSeek);
            SetValue(StartTimeProperty, TimeSpan.Zero);
            SetValue(LivePositionProperty, (TimeSpan?)null);
            SetValue(IsLiveProperty, false);
            SetValue(NaturalDurationProperty, DefaultNaturalDuration);
            SetValue(NaturalVideoHeightProperty, DefaultNaturalVideoHeight);
            SetValue(NaturalVideoWidthProperty, DefaultNaturalVideoWidth);
            SetValue(MediaQualityProperty, MediaQuality.StandardDefinition);
            SetValue(SignalStrengthProperty, 0.0);
        }

        void OnRateChanged(RateChangedRoutedEventArgs e)
        {
#if SILVERLIGHT
            IsSlowMotion = (e.NewRate == SlowMotionPlaybackRate);
#else
            IsSlowMotion = (_PlaybackRate == SlowMotionPlaybackRate);
#endif
            NotifyIsPlayResumeAllowedChanged();
            NotifyIsRewindAllowedChanged();
            NotifyIsFastForwardAllowedChanged();
            if (RateChanged != null) RateChanged(this, e);
        }

        void OnPositionChanged(RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            UpdateIsPositionLive();
            SetValue(TimeRemainingProperty, TimeSpanExtensions.Max(EndTime.Subtract(e.NewValue), TimeSpan.Zero));
            if (PositionChanged != null) PositionChanged(this, e);
        }

#if SILVERLIGHT
        void OnVolumeChanged(RoutedPropertyChangedEventArgs<double> e)
#else
        void OnVolumeChanged(RoutedEventArgs e)
#endif
        {
            if (VolumeChanged != null) VolumeChanged(this, e);
        }

        void OnIsMutedChanged(RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsMutedChanged != null) IsMutedChanged(this, e);
        }

        void OnIsLiveChanged(RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsLiveChanged != null) IsLiveChanged(this, e);
        }

        void OnIsCaptionsActiveChanged(RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsCaptionsActiveChanged != null) IsCaptionsActiveChanged(this, e);
        }

        void OnIsFullScreenChanged(RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsFullScreenChanged != null) IsFullScreenChanged(this, e);
        }

        void OnAdvertisingStateChanged(RoutedPropertyChangedEventArgs<AdvertisingState> e)
        {
            if (AdvertisingStateChanged != null) AdvertisingStateChanged(this, e);
        }

        void OnAudioStreamIndexChanged(RoutedPropertyChangedEventArgs<int?> e)
        {
            if (AudioStreamIndexChanged != null) AudioStreamIndexChanged(this, e);
        }

        void OnSeeked(SeekRoutedEventArgs e)
        {
            if (Seeked != null) Seeked(this, e);
        }

        void OnScrubbing(ScrubProgressRoutedEventArgs e)
        {
            if (Scrubbing != null) Scrubbing(this, e);
        }

        void OnScrubbingCompleted(ScrubProgressRoutedEventArgs e)
        {
            if (ScrubbingCompleted != null) ScrubbingCompleted(this, e);
        }

        void OnScrubbingStarted(ScrubRoutedEventArgs e)
        {
            if (ScrubbingStarted != null) ScrubbingStarted(this, e);
        }

        void OnPlayerStateChanged(RoutedPropertyChangedEventArgs<PlayerState> e)
        {
            if (PlayerStateChanged != null) PlayerStateChanged(this, e);
        }

        void OnSignalStrengthChanged(RoutedPropertyChangedEventArgs<double> e)
        {
            if (SignalStrengthChanged != null) SignalStrengthChanged(this, e);
        }

        void OnMediaQualityChanged(RoutedEventArgs e)
        {
            if (MediaQualityChanged != null) MediaQualityChanged(this, e);
        }

        void OnIsSlowMotionChanged(RoutedPropertyChangedEventArgs<bool> e)
        {
            if (IsSlowMotionChanged != null) IsSlowMotionChanged(this, e);
        }

        void OnDurationChanged(RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            if (DurationChanged != null) DurationChanged(this, e);
        }

        void OnStartTimeChanged(RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            if (StartTimeChanged != null) StartTimeChanged(this, e);
        }

        void OnEndTimeChanged(RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            if (EndTimeChanged != null) EndTimeChanged(this, e);
        }

        void OnTimeRemainingChanged(RoutedPropertyChangedEventArgs<TimeSpan> e)
        {
            if (TimeRemainingChanged != null) TimeRemainingChanged(this, e);
        }

        void OnLivePositionChanged(RoutedPropertyChangedEventArgs<TimeSpan?> e)
        {
            if (LivePositionChanged != null) LivePositionChanged(this, e);
        }

        void OnTimeFormatConverterChanged(RoutedPropertyChangedEventArgs<IValueConverter> e)
        {
            if (TimeFormatConverterChanged != null) TimeFormatConverterChanged(this, e);
        }

        void OnSelectedCaptionChanged(RoutedPropertyChangedEventArgs<Caption> e)
        {
            if (SelectedCaptionChanged != null) SelectedCaptionChanged(this, e);
        }

        void OnSelectedAudioStreamChanged(SelectedAudioStreamChangedEventArgs e)
        {
            if (SelectedAudioStreamChanged != null) SelectedAudioStreamChanged(this, e);
        }

        void OnSkipBackIntervalChanged(RoutedPropertyChangedEventArgs<TimeSpan?> e)
        {
            if (SkipBackIntervalChanged != null) SkipBackIntervalChanged(this, e);
        }

        void OnSkipAheadIntervalChanged(RoutedPropertyChangedEventArgs<TimeSpan?> e)
        {
            if (SkipAheadIntervalChanged != null) SkipAheadIntervalChanged(this, e);
        }


        #endregion

        #region Helpers

        /// <summary>
        /// Retrieves a resource string from the ResourceLoader
        /// </summary>
        /// <param name="resourceId">The ID of the resource</param>
        /// <returns>The resource string found.</returns>
        public static string GetResourceString(string resourceId)
#if SILVERLIGHT
        {
            return Microsoft.PlayerFramework.Resources.ResourceManager.GetString("CaptionsDisable", Microsoft.PlayerFramework.Resources.Culture);
        }
#else
        {
            string result = null;
            if (ResourceLoader != null)
            {
                result = ResourceLoader.GetString(string.Format("{0}", resourceId));
            }
            if (string.IsNullOrEmpty(result))
            {
                result = DefaultResourceLoader.GetString(string.Format("Resources/{0}", resourceId));
            }
            return result;
        }

        static ResourceLoader defaultResourceLoader;
        static ResourceLoader DefaultResourceLoader
        {
            get
            {
                if (defaultResourceLoader == null)
                {
                    defaultResourceLoader = new ResourceLoader("Microsoft.PlayerFramework");
                }
                return defaultResourceLoader;
            }
        }

        /// <summary>
        /// Gets or sets the ResourceLoader used to load all string resources.
        /// </summary>
        public static ResourceLoader ResourceLoader { get; set; }
#endif

        private static DependencyProperty RegisterDependencyProperty<T1, T2>(string propertyName, Action<T1, T2, T2> callback, T2 defaultValue) where T1 : DependencyObject
        {
            return DependencyProperty.Register(propertyName, typeof(T2), typeof(T1), new PropertyMetadata(defaultValue, (d, e) => callback((T1)d, (T2)e.OldValue, (T2)e.NewValue)));
        }

        private static DependencyProperty RegisterDependencyProperty<T1, T2>(string propertyName, Action<T1, T2, T2> callback) where T1 : DependencyObject
        {
#if SILVERLIGHT
            return DependencyProperty.Register(propertyName, typeof(T2), typeof(T1), new PropertyMetadata((d, e) => callback((T1)d, (T2)e.OldValue, (T2)e.NewValue)));
#else
            return DependencyProperty.Register(propertyName, typeof(T2), typeof(T1), new PropertyMetadata(default(T2), (d, e) => callback((T1)d, (T2)e.OldValue, (T2)e.NewValue)));
#endif
        }

        private static DependencyProperty RegisterDependencyProperty<T1, T2>(string propertyName, T2 defaultValue = default(T2)) where T1 : DependencyObject
        {
            return DependencyProperty.Register(propertyName, typeof(T2), typeof(T1), new PropertyMetadata(defaultValue));
        }

        private static DependencyProperty RegisterDependencyProperty<T1, T2>(string propertyName) where T1 : DependencyObject
        {
            return DependencyProperty.Register(propertyName, typeof(T2), typeof(T1), null);
        }

        static DependencyProperty RegisterDependencyProperty<T>(string propertyName)
        {
            return RegisterDependencyProperty<MediaPlayer, T>(propertyName);
        }

        static DependencyProperty RegisterDependencyProperty<T>(string propertyName, Action<MediaPlayer, T, T> callback)
        {
            return RegisterDependencyProperty<MediaPlayer, T>(propertyName, (t, o, n) =>
            {
                if (!t.ignoreCallback) callback(t, o, n);
            });
        }

        static DependencyProperty RegisterDependencyProperty<T>(string propertyName, Action<MediaPlayer, T, T> callback, T defaultValue)
        {
            return RegisterDependencyProperty<MediaPlayer, T>(propertyName, (t, o, n) =>
            {
                if (!t.ignoreCallback) callback(t, o, n);
            }, defaultValue);
        }

        static DependencyProperty RegisterDependencyProperty<T>(string propertyName, T defaultValue)
        {
            return RegisterDependencyProperty<MediaPlayer, T>(propertyName, defaultValue);
        }

        bool ignoreCallback;

        /// <summary>
        /// Sets the local value of a dependency property on a DependencyObject without invoking the callback.
        /// </summary>
        /// <param name="dp">The identifier of the dependency property to set.</param>
        /// <param name="value">The new local value.</param>
        protected void SetValueWithoutCallback(DependencyProperty dp, object value)
        {
            ignoreCallback = true;
            try
            {
                SetValue(dp, value);
            }
            finally
            {
                ignoreCallback = false;
            }
        }
        #endregion

        /// <summary>
        /// Disposes of the active session and frees up all memory associated with this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Use SuppressFinalize in case a subclass of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        private bool _disposed;
        /// <summary>
        /// Disposes of the active session and frees up all memory associated with this instance.
        /// </summary>
        /// <param name="disposing">Is called from the Dispose method.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                    LoadPlugins(null);
                    UninitializeTemplateChildren();
                    DestroyTemplateChildren();
#if !SILVERLIGHT
                    mediaExtensionManager = null;
#endif
                }

                // Indicate that the instance has been disposed.
                _disposed = true;
            }
        }
    }
}
