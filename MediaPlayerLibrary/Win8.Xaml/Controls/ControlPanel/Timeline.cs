using System;
using System.Windows.Input;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endif

namespace Microsoft.PlayerFramework
{
    internal static class TimelineTemplateParts
    {
        public const string PositionedItemsControl = "PositionedItemsControl";
        public const string DownloadProgressBarElement = "DownloadProgressBar";
        public const string ProgressSliderElement = "ProgressSlider";
    }

    /// <summary>
    /// Provides a Timeline control that can be easily bound to an InteractiveViewModel (e.g. MediaPlayer.InteractiveViewModel)
    /// </summary>
    [TemplatePart(Name = TimelineTemplateParts.DownloadProgressBarElement, Type = typeof(ProgressBar))]
    [TemplatePart(Name = TimelineTemplateParts.ProgressSliderElement, Type = typeof(SeekableSlider))]
    [TemplatePart(Name = TimelineTemplateParts.PositionedItemsControl, Type = typeof(PositionedItemsControl))]
    public class Timeline : Control
    {
        /// <summary>
        /// The download progress bar for non-adaptive video.
        /// </summary>
        protected ProgressBar DownloadProgressBarElement { get; private set; }
        /// <summary>
        /// The timeline.
        /// </summary>
        protected SeekableSlider ProgressSliderElement { get; private set; }
        /// <summary>
        /// The marker container.
        /// </summary>
        protected PositionedItemsControl PositionedItemsControl { get; private set; }

        /// <summary>
        /// Creates a new instance of Timeline
        /// </summary>
        public Timeline()
        {
            this.DefaultStyleKey = typeof(Timeline);
            AutomationProperties.SetName(this, MediaPlayer.GetResourceString("TimelineLabel"));
        }

        /// <inheritdoc /> 
#if SILVERLIGHT
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            // unwire existing event handlers if template was already applied
            if (ProgressSliderElement != null)
            {
                ProgressSliderElement.Seeked -= ProgressSliderElement_Seeked;
                ProgressSliderElement.ScrubbingStarted -= ProgressSliderElement_ScrubbingStarted;
                ProgressSliderElement.Scrubbing -= ProgressSliderElement_Scrubbing;
                ProgressSliderElement.ScrubbingCompleted -= ProgressSliderElement_ScrubbingCompleted;
            }

            if (PositionedItemsControl != null)
            {
                PositionedItemsControl.ItemLoaded -= PositionedItemsControl_ItemLoaded;
                PositionedItemsControl.ItemUnloaded -= PositionedItemsControl_ItemUnloaded;
            }

            PositionedItemsControl = GetTemplateChild(TimelineTemplateParts.PositionedItemsControl) as PositionedItemsControl;
            DownloadProgressBarElement = GetTemplateChild(TimelineTemplateParts.DownloadProgressBarElement) as ProgressBar;
            ProgressSliderElement = GetTemplateChild(TimelineTemplateParts.ProgressSliderElement) as SeekableSlider;

            // wire up events to bubble
            if (ProgressSliderElement != null)
            {
                ProgressSliderElement.Style = SliderStyle;
                ProgressSliderElement.Seeked += ProgressSliderElement_Seeked;
                ProgressSliderElement.ScrubbingStarted += ProgressSliderElement_ScrubbingStarted;
                ProgressSliderElement.Scrubbing += ProgressSliderElement_Scrubbing;
                ProgressSliderElement.ScrubbingCompleted += ProgressSliderElement_ScrubbingCompleted;
            }

            if (PositionedItemsControl != null)
            {
                PositionedItemsControl.ItemLoaded += PositionedItemsControl_ItemLoaded;
                PositionedItemsControl.ItemUnloaded += PositionedItemsControl_ItemUnloaded;
            }
        }

        void PositionedItemsControl_ItemUnloaded(object sender, FrameworkElementEventArgs args)
        {
            if (args.Element is ButtonBase)
            {
                ((ButtonBase)args.Element).Click -= Timeline_Click;
            }
        }

        void PositionedItemsControl_ItemLoaded(object sender, FrameworkElementEventArgs args)
        {
            if (args.Element is ButtonBase)
            {
                ((ButtonBase)args.Element).Click += Timeline_Click;
            }
        }

        void Timeline_Click(object sender, RoutedEventArgs e)
        {
            var marker = ((ButtonBase)sender).DataContext as VisualMarker;
            ViewModel.Seek(marker.Time);
        }

        void ProgressSliderElement_Seeked(object sender, ValueRoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Seek(TimeSpan.FromSeconds(e.Value));
            }
        }

        void ProgressSliderElement_Scrubbing(object sender, ValueRoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var vm = ViewModel; // hold onto this in case the ViewModel changes because of this action. This way we can ensure we're calling the same one.
                bool canceled = false;
                vm.Scrub(TimeSpan.FromSeconds(e.Value), out canceled);
                if (canceled)
                {
                    ProgressSliderElement.CancelScrub();
                    vm.CompleteScrub(TimeSpan.FromSeconds(e.Value), ref canceled);
                }
            }
        }

        void ProgressSliderElement_ScrubbingCompleted(object sender, ValueRoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                bool canceled = false;
                ViewModel.CompleteScrub(TimeSpan.FromSeconds(e.Value), ref canceled);
            }
        }

        void ProgressSliderElement_ScrubbingStarted(object sender, ValueRoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                var vm = ViewModel; // hold onto this in case the ViewModel changes because of this action. This way we can ensure we're calling the same one.
                bool canceled = false;
                vm.StartScrub(TimeSpan.FromSeconds(e.Value), out canceled);
                if (canceled)
                {
                    ProgressSliderElement.CancelScrub();
                    vm.CompleteScrub(TimeSpan.FromSeconds(e.Value), ref canceled);
                }
            }
        }

        /// <summary>
        /// Identifies the MediaPlayer dependency property.
        /// </summary>
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(IInteractiveViewModel), typeof(Timeline), null);
        
        /// <summary>
        /// The InteractiveMediaPlayer object used to provide state updates and serve user interaction requests.
        /// This property is usually bound to MediaPlayer.InteractiveViewModel but could be a custom implementation to support unique interaction such as in the case of advertising.
        /// </summary>
        public IInteractiveViewModel ViewModel
        {
            get { return GetValue(ViewModelProperty) as IInteractiveViewModel; }
            set { SetValue(ViewModelProperty, value); }
        }
        
        /// <summary>
        /// Identifies the MediaPlayer dependency property.
        /// </summary>
        public static readonly DependencyProperty SliderStyleProperty = DependencyProperty.Register("SliderStyle", typeof(Style), typeof(Timeline), new PropertyMetadata(null, (d, e) => ((Timeline)d).OnSliderStyleChanged(e.NewValue as Style)));

        void OnSliderStyleChanged(Style newValue)
        {
            if (ProgressSliderElement != null)
            {
                ProgressSliderElement.Style = newValue;
            }
        }

        /// <summary>
        /// The InteractiveMediaPlayer object used to provide state updates and serve user interaction requests.
        /// This is usually an instance of the MediaPlayer but could be a custom implementation to support unique interaction such as in the case of advertising.
        /// </summary>
        public Style SliderStyle
        {
            get { return GetValue(SliderStyleProperty) as Style; }
            set { SetValue(SliderStyleProperty, value); }
        }
    }
}
