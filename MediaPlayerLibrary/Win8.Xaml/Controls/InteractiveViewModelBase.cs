﻿using System;
using System.Linq;
using System.Net;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Collections.Generic;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
#endif

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// Provides a base class to help implement IInteractiveViewModel
    /// </summary>
    public abstract class InteractiveViewModelBase : IInteractiveViewModel, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes InteractiveViewModelBase
        /// </summary>
        protected InteractiveViewModelBase()
        {
            SkipPreviousThreshold = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Gets or sets how far away from the previous marker you should be for it to be recognized when skipping previous.
        /// Default is 2 seconds.
        /// </summary>
        public TimeSpan SkipPreviousThreshold { get; set; }

        /// <inheritdoc /> 
        public void OnInteracting(InteractionType interactionType = InteractionType.Hard)
        {
            if (Interacting != null) Interacting(this, new InteractionEventArgs(interactionType));
        }

        /// <summary>
        /// Invokes the CurrentStateChanged event.
        /// </summary>
        /// <param name="e">The event args to pass</param>
        protected void OnCurrentStateChanged(RoutedEventArgs e)
        {
            if (CurrentStateChanged != null) CurrentStateChanged(this, e);
        }

        /// <inheritdoc /> 
        public event EventHandler<InteractionEventArgs> Interacting;

        #region Methods

        /// <inheritdoc /> 
        public void Stop()
        {
            OnInteracting();
            OnStop();
        }

        /// <summary>
        /// Notifies the subclass to stop the media
        /// </summary>
        protected abstract void OnStop();

        /// <inheritdoc /> 
        public void Pause()
        {
            OnInteracting();
            OnPause();
        }

        /// <summary>
        /// Notifies the subclass to pause the media
        /// </summary>
        protected abstract void OnPause();

        /// <inheritdoc /> 
        public void InvokeCaptionSelection()
        {
            OnInteracting();
            OnInvokeCaptionSelection();
        }

        /// <summary>
        /// Notifies the subclass to show captions or a caption selector dialog.
        /// </summary>
        protected abstract void OnInvokeCaptionSelection();

        /// <inheritdoc /> 
        public void InvokeAudioSelection()
        {
            OnInteracting();
            OnInvokeAudioSelection();
        }

        /// <summary>
        /// Notifies the subclass to show audio selection dialog.
        /// </summary>
        protected abstract void OnInvokeAudioSelection();

        /// <inheritdoc /> 
        public void SkipPrevious()
        {
            OnInteracting();

            VisualMarker marker = VisualMarkers
                .Where(m => m.IsSeekable && m.Time.Add(SkipPreviousThreshold) < Position && m.Time < MaxPosition)
                .OrderByDescending(m => m.Time).FirstOrDefault();

            OnSkipPrevious(marker);
        }

        /// <summary>
        /// Notifies the subclass to skip back to the previous chapter/marker
        /// </summary>
        /// <param name="marker">The marker to seek to. Null if to the beginning.</param>
        protected abstract void OnSkipPrevious(VisualMarker marker);

        /// <inheritdoc /> 
        public void SkipNext()
        {
            OnInteracting();

            VisualMarker marker = VisualMarkers
                .Where(m => m.IsSeekable && m.Time > Position && m.Time < MaxPosition)
                .OrderBy(m => m.Time).FirstOrDefault();

            OnSkipNext(marker);
        }

        /// <summary>
        /// Notifies the subclass to skip ahead to the next chapter/marker
        /// </summary>
        /// <param name="marker">The marker to seek to. Null if to the end.</param>
        protected abstract void OnSkipNext(VisualMarker marker);

        /// <inheritdoc /> 
        public void SkipBack()
        {
            OnInteracting();
            TimeSpan position = SkipBackInterval.HasValue ? TimeSpanExtensions.Max(Position.Subtract(SkipBackInterval.Value), StartTime) : StartTime;
            OnSkipBack(position);
        }

        /// <summary>
        /// Notifies the subclass to seek back in the media.
        /// </summary>
        /// <param name="position">The position to seek to.</param>
        protected abstract void OnSkipBack(TimeSpan position);

        /// <inheritdoc /> 
        public void SkipAhead()
        {
            OnInteracting();
            TimeSpan position = SkipAheadInterval.HasValue ? TimeSpanExtensions.Min(Position.Add(SkipAheadInterval.Value), MaxPosition) : MaxPosition;
            OnSkipAhead(position);
        }

        /// <summary>
        /// Notifies the subclass to seek ahead.
        /// </summary>
        /// <param name="position">The position to seek to.</param>
        protected abstract void OnSkipAhead(TimeSpan position);

        /// <inheritdoc /> 
        public void Seek(TimeSpan position)
        {
            OnInteracting();
            OnSeek(position);
        }

        /// <summary>
        /// Allows the subclass to learn when a seek occurred.
        /// </summary>
        /// <param name="position">The position seeked to.</param>
        protected abstract void OnSeek(TimeSpan position);

        /// <inheritdoc /> 
        public void CompleteScrub(TimeSpan position, ref bool canceled)
        {
            OnInteracting();
            OnCompleteScrub(position, ref canceled);
        }

        /// <summary>
        /// Allows the subclass to learn when a scrub is complete.
        /// </summary>
        /// <param name="position">The position at which scrubbing ended.</param>
        /// <param name="canceled">Gets or sets whether the operation should be cancelled</param>
        protected abstract void OnCompleteScrub(TimeSpan position, ref bool canceled);

        /// <inheritdoc /> 
        public void StartScrub(TimeSpan position, out bool canceled)
        {
            OnInteracting();
            OnStartScrub(position, out canceled);
        }

        /// <summary>
        /// Allows the subclass to learn when a scrub is strarting.
        /// </summary>
        /// <param name="position">The position at which scrubbing started.</param>
        /// <param name="canceled">Indicates that the scrub should be canceled</param>
        protected abstract void OnStartScrub(TimeSpan position, out bool canceled);

        /// <inheritdoc /> 
        public void Scrub(TimeSpan position, out bool canceled)
        {
            OnInteracting();
            OnScrub(position, out canceled);
        }

        /// <summary>
        /// Allows the subclass to learn where the user is scrub to.
        /// </summary>
        /// <param name="position">The current position of scrubbing.</param>
        /// <param name="canceled">Indicates that the scrub should be canceled</param>
        protected abstract void OnScrub(TimeSpan position, out bool canceled);

        /// <inheritdoc /> 
        public void GoLive()
        {
            OnInteracting();
            OnGoLive();
        }

        /// <summary>
        /// Notifies the subclass that the user is attempting to seek to the live position.
        /// </summary>
        protected abstract void OnGoLive();

        /// <inheritdoc /> 
        public void PlayResume()
        {
            OnInteracting();
            OnPlayResume();
        }

        /// <summary>
        /// Notifies the subclass that the user has chosen to reset the playback rate.
        /// </summary>
        protected abstract void OnPlayResume();

        /// <inheritdoc /> 
        public void Replay()
        {
            OnInteracting();
            OnReplay();
        }

        /// <summary>
        /// Notifies the subclass that the user has chosen the instant replay feature
        /// </summary>
        protected abstract void OnReplay();

        /// <inheritdoc /> 
        public void DecreasePlaybackRate()
        {
            OnInteracting();
            OnDecreasePlaybackRate();
        }

        /// <summary>
        /// Notifies the subclass that the user has chosen the rewind feature
        /// </summary>
        protected abstract void OnDecreasePlaybackRate();

        /// <inheritdoc /> 
        public void IncreasePlaybackRate()
        {
            OnInteracting();
            OnIncreasePlaybackRate();
        }

        /// <summary>
        /// Notifies the subclass that the user has chosen the fast forward feature
        /// </summary>
        protected abstract void OnIncreasePlaybackRate();

#if SILVERLIGHT
        /// <inheritdoc /> 
        public void CycleDisplayMode()
        {
            OnInteracting();
            OnCycleDisplayMode();
        }

        /// <summary>
        /// Notifies the subclass that the user has chosen the cycle display mode feature to change the stretch state of the player.
        /// </summary>
        protected abstract void OnCycleDisplayMode();
#endif
        #endregion

        /// <inheritdoc /> 
        public bool IsMuted
        {
            get
            {
                return this._IsMuted;
            }
            set
            {
                OnInteracting();
                this._IsMuted = value;
            }
        }

        /// <inheritdoc /> 
        public bool IsFullScreen
        {
            get
            {
                return this._IsFullScreen;
            }
            set
            {
                OnInteracting();
                this._IsFullScreen = value;
            }
        }

        /// <inheritdoc /> 
        public bool IsSlowMotion
        {
            get
            {
                return this._IsSlowMotion;
            }
            set
            {
                OnInteracting();
                this._IsSlowMotion = value;
            }
        }

        /// <inheritdoc /> 
        public double Volume
        {
            get
            {
                return this._Volume;
            }
            set
            {
                OnInteracting();
                this._Volume = value;
            }
        }
        
        /// <inheritdoc /> 
        public abstract IEnumerable<Caption> AvailableCaptions { get; }

        /// <inheritdoc /> 
        public abstract Caption SelectedCaption { get; set; }

        /// <inheritdoc /> 
        public abstract IEnumerable<AudioStream> AvailableAudioStreams { get; }

        /// <inheritdoc /> 
        public abstract AudioStream SelectedAudioStream { get; set; }

        /// <inheritdoc /> 
        public abstract IEnumerable<VisualMarker> VisualMarkers { get; }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsGoLiveEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsGoLiveEnabled { get; }

        /// <summary>
        /// Indicates that the go live enabled state may have changed.
        /// </summary>
        protected void NotifyIsGoLiveEnabledChanged()
        {
            OnPropertyChanged(() => IsGoLiveEnabled);
            if (IsGoLiveEnabledChanged != null) IsGoLiveEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsPlayResumeEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsPlayResumeEnabled { get; }

        /// <summary>
        /// Indicates that the play resume enabled state may have changed.
        /// </summary>
        protected void NotifyIsPlayResumeEnabledChanged()
        {
            OnPropertyChanged(() => IsPlayResumeEnabled);
            if (IsPlayResumeEnabledChanged != null) IsPlayResumeEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsPauseEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsPauseEnabled { get; }

        /// <summary>
        /// Indicates that the pause enabled state may have changed.
        /// </summary>
        protected void NotifyIsPauseEnabledChanged()
        {
            OnPropertyChanged(() => IsPauseEnabled);
            if (IsPauseEnabledChanged != null) IsPauseEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsStopEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsStopEnabled { get; }

        /// <summary>
        /// Indicates that the stop enabled state may have changed.
        /// </summary>
        protected void NotifyIsStopEnabledChanged()
        {
            OnPropertyChanged(() => IsStopEnabled);
            if (IsStopEnabledChanged != null) IsStopEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsReplayEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsReplayEnabled { get; }

        /// <summary>
        /// Indicates that the replay enabled state may have changed.
        /// </summary>
        protected void NotifyIsReplayEnabledChanged()
        {
            OnPropertyChanged(() => IsReplayEnabled);
            if (IsReplayEnabledChanged != null) IsReplayEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsAudioSelectionEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsAudioSelectionEnabled { get; }

        /// <summary>
        /// Indicates that the audio stream selection enabled state may have changed.
        /// </summary>
        protected void NotifyIsAudioSelectionEnabledChanged()
        {
            OnPropertyChanged(() => IsAudioSelectionEnabled);
            if (IsAudioSelectionEnabledChanged != null) IsAudioSelectionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsCaptionSelectionEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsCaptionSelectionEnabled { get; }

        /// <summary>
        /// Indicates that the audio stream selection enabled state may have changed.
        /// </summary>
        protected void NotifyIsCaptionSelectionEnabledChanged()
        {
            OnPropertyChanged(() => IsCaptionSelectionEnabled);
            if (IsCaptionSelectionEnabledChanged != null) IsCaptionSelectionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsRewindEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsRewindEnabled { get; }

        /// <summary>
        /// Indicates that the rewind enabled state may have changed.
        /// </summary>
        protected void NotifyIsRewindEnabledChanged()
        {
            OnPropertyChanged(() => IsRewindEnabled);
            if (IsRewindEnabledChanged != null) IsRewindEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsFastForwardEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsFastForwardEnabled { get; }

        /// <summary>
        /// Indicates that the fast forward enabled state may have changed.
        /// </summary>
        protected void NotifyIsFastForwardEnabledChanged()
        {
            OnPropertyChanged(() => IsFastForwardEnabled);
            if (IsFastForwardEnabledChanged != null) IsFastForwardEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSlowMotionEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSlowMotionEnabled { get; }

        /// <summary>
        /// Indicates that the slow motion enabled state may have changed.
        /// </summary>
        protected void NotifyIsSlowMotionEnabledChanged()
        {
            OnPropertyChanged(() => IsSlowMotionEnabled);
            if (IsSlowMotionEnabledChanged != null) IsSlowMotionEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSeekEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSeekEnabled { get; }

        /// <summary>
        /// Indicates that the seek enabled state may have changed.
        /// </summary>
        protected void NotifyIsSeekEnabledChanged()
        {
            OnPropertyChanged(() => IsSeekEnabled);
            if (IsSeekEnabledChanged != null) IsSeekEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSkipPreviousEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSkipPreviousEnabled { get; }

        /// <summary>
        /// Indicates that the skip Previous enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipPreviousEnabledChanged()
        {
            OnPropertyChanged(() => IsSkipPreviousEnabled);
            if (IsSkipPreviousEnabledChanged != null) IsSkipPreviousEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSkipNextEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSkipNextEnabled { get; }

        /// <summary>
        /// Indicates that the skip next enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipNextEnabledChanged()
        {
            OnPropertyChanged(() => IsSkipNextEnabled);
            if (IsSkipNextEnabledChanged != null) IsSkipNextEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSkipBackEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSkipBackEnabled { get; }

        /// <summary>
        /// Indicates that the skip back enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipBackEnabledChanged()
        {
            OnPropertyChanged(() => IsSkipBackEnabled);
            if (IsSkipBackEnabledChanged != null) IsSkipBackEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsSkipAheadEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsSkipAheadEnabled { get; }

        /// <summary>
        /// Indicates that the skip Ahead enabled state may have changed.
        /// </summary>
        protected void NotifyIsSkipAheadEnabledChanged()
        {
            OnPropertyChanged(() => IsSkipAheadEnabled);
            if (IsSkipAheadEnabledChanged != null) IsSkipAheadEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler IsScrubbingEnabledChanged;

        /// <inheritdoc /> 
        public abstract bool IsScrubbingEnabled { get; }

        /// <summary>
        /// Indicates that the scrubbing enabled state may have changed.
        /// </summary>
        protected void NotifyIsScrubbingEnabledChanged()
        {
            OnPropertyChanged(() => IsScrubbingEnabled);
            if (IsScrubbingEnabledChanged != null) IsScrubbingEnabledChanged(this, new RoutedEventArgs());
        }

        /// <inheritdoc /> 
        public event RoutedEventHandler CurrentStateChanged;


        /// <summary>
        /// Gets or sets the Volume
        /// </summary>
        protected abstract double _Volume { get; set; }

        /// <summary>
        /// Gets or sets the IsMuted flag
        /// </summary>
        protected abstract bool _IsMuted { get; set; }

        /// <summary>
        /// Gets or sets the IsFullScreen flag
        /// </summary>
        protected abstract bool _IsFullScreen { get; set; }
        
        /// <summary>
        /// Gets or sets the IsSlowMotion flag
        /// </summary>
        protected abstract bool _IsSlowMotion { get; set; }

        /// <inheritdoc /> 
        public abstract double BufferingProgress { get; }
        /// <inheritdoc /> 
        public abstract double DownloadProgress { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan StartTime { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan EndTime { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan Duration { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan TimeRemaining { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan Position { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan MaxPosition { get; }
        /// <inheritdoc /> 
        public abstract MediaElementState CurrentState { get; }
        /// <inheritdoc /> 
        public abstract IValueConverter TimeFormatConverter { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan? SkipBackInterval { get; }
        /// <inheritdoc /> 
        public abstract TimeSpan? SkipAheadInterval { get; }
        /// <inheritdoc /> 
        public abstract double SignalStrength { get; }
        /// <inheritdoc /> 
        public abstract MediaQuality MediaQuality { get; }

        #region PropertyChanged

        /// <inheritdoc /> 
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invokes the property changed event.
        /// </summary>
        /// <param name="PropertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string PropertyName)
        {
            try
            {
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
            }
            catch (NullReferenceException)
            {
                // HACK: This will throw an exception on Win8 CPU2 sometimes.
            }
        }

        /// <summary>
        /// Invokes the property changed event.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <param name="property">A lambda expression returning the property</param>
        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            OnPropertyChanged(GetPropertyName(property));
        }

        static string GetPropertyName<T>(Expression<Func<T>> property)
        {
            return (property.Body as MemberExpression).Member.Name;
        }

        #endregion
    }
}
