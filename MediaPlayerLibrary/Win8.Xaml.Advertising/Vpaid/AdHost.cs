using System;
#if SILVERLIGHT
using System.Windows.Controls;
using System.Windows;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
#endif

namespace Microsoft.PlayerFramework.Advertising
{
    /// <summary>
    /// Provides a base class to host an ad player in.
    /// This is essentially a ContentControl with a hyperlink on top.
    /// </summary>
    public class AdHost : ContentControl
    {
        /// <summary>
        /// Creates a new instance of AdHost.
        /// </summary>
        public AdHost()
        {
            this.DefaultStyleKey = typeof(AdHost);
        }

        /// <summary>
        /// Raised after nativation occurs.
        /// </summary>
        public event RoutedEventHandler Navigated;

        /// <summary>
        /// Gets the HyperlinkButton control.
        /// </summary>
        protected HyperlinkButton ClickThroughButton { get; private set; }

        /// <summary>
        /// Indicates that the template has been loaded.
        /// </summary>
        protected bool IsTemplateLoaded { get; private set; }

        /// <inheritdoc /> 
#if SILVERLIGHT
        public override void OnApplyTemplate()
#else
        protected override void OnApplyTemplate()
#endif
        {
            base.OnApplyTemplate();

            ClickThroughButton = base.GetTemplateChild("ClickThroughButton") as HyperlinkButton;
            if (ClickThroughButton != null)
            {
                ClickThroughButton.Visibility = navigateUri != null ? Visibility.Visible : Visibility.Collapsed;
                ClickThroughButton.Click += ClickThroughButton_Click;
                ClickThroughButton.NavigateUri = navigateUri;
#if SILVERLIGHT
                ClickThroughButton.TargetName = "_blank";
#endif
            }

            IsTemplateLoaded = true;
            AdLinear = adLinear;
        }

        void ClickThroughButton_Click(object sender, RoutedEventArgs e)
        {
            if (Navigated != null) Navigated(this, e);
        }

        Uri navigateUri;
        /// <summary>
        /// Gets or sets the Uri to navigate to when the hyperlink button is clicked.
        /// </summary>
        public Uri NavigateUri
        {
            get { return navigateUri; }
            set
            {
                navigateUri = value;
                if (ClickThroughButton != null)
                {
                    ClickThroughButton.Visibility = navigateUri != null ? Visibility.Visible : Visibility.Collapsed;
                    ClickThroughButton.NavigateUri = navigateUri;
                }
            }
        }

        private bool adLinear;
        /// <summary>
        /// Gets or sets if the ad is linear or nonlinear
        /// </summary>
        public bool AdLinear
        {
            get { return adLinear; }
            set
            {
                adLinear = value;
                if (IsTemplateLoaded)
                {
                    if (adLinear)
                    {
                        VisualStateManager.GoToState(this, "Linear", true);
                    }
                    else
                    {
                        VisualStateManager.GoToState(this, "Nonlinear", true);
                    }
                }
            }
        }
    }
}
