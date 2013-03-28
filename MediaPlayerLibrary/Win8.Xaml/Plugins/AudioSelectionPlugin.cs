﻿#define CODE_ANALYSIS

using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
#if SILVERLIGHT
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections.Generic;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
#endif

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// A plugin used to allow the user 
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "Correctly named architectural pattern")]
#if MEF
    [System.ComponentModel.Composition.PartCreationPolicy(System.ComponentModel.Composition.CreationPolicy.NonShared)]
    [System.ComponentModel.Composition.Export(typeof(IPlugin))]
#endif
    public sealed class AudioSelectionPlugin : PluginBase
    {
        /// <summary>
        /// Gets or sets the style to be used for the CaptionSelectorView
        /// </summary>
        public Style AudioSelectionViewStyle { get; set; }

        /// <inheritdoc /> 
        protected override bool OnActivate()
        {
            MediaPlayer.AudioSelectionInvoked += MediaPlayer_AudioSelectionInvoked;
            return true;
        }

        /// <inheritdoc /> 
        protected override void OnDeactivate()
        {
            MediaPlayer.AudioSelectionInvoked -= MediaPlayer_AudioSelectionInvoked;
        }

        Panel SettingsContainer
        {
            get
            {
                return MediaPlayer.Containers.OfType<Panel>().FirstOrDefault(c => c.Name == MediaPlayerTemplateParts.SettingsContainer);
            }
        }

        InteractionType deactivationMode;
        void MediaPlayer_AudioSelectionInvoked(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.AvailableAudioStreams.Any())
            {
                var selectorView = new AudioSelectionView()
                {
                    Style = AudioSelectionViewStyle
                };
                selectorView.SetBinding(FrameworkElement.DataContextProperty, new Binding() { Path = new PropertyPath("InteractiveViewModel"), Source = MediaPlayer });
                SettingsContainer.Visibility = Visibility.Visible;
                SettingsContainer.Children.Add(selectorView);
                selectorView.Close += SelectorView_Close;
                deactivationMode = MediaPlayer.InteractiveDeactivationMode;
                MediaPlayer.InteractiveDeactivationMode = InteractionType.None;
            }
        }

        void SelectorView_Close(object sender, EventArgs e)
        {
            var selectorView = sender as AudioSelectionView;
            selectorView.Close -= SelectorView_Close;
            selectorView.Visibility = Visibility.Collapsed;
            SettingsContainer.Children.Remove(selectorView);
            SettingsContainer.Visibility = Visibility.Collapsed;
            MediaPlayer.InteractiveDeactivationMode = deactivationMode;
        }
    }
}
