using System;
using System.Collections.Generic;
#if SILVERLIGHT
using System.Windows;
#else
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#endif

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// Represents a caption or subtitle track.
    /// </summary>
#if SILVERLIGHT
    public class Caption : DependencyObject
#else
    public class Caption : FrameworkElement
#endif
    {
        /// <summary>
        /// Indicates that the Payload property has changed
        /// </summary>
        public event EventHandler PayloadChanged;

        /// <summary>
        /// Invokes the PayloadChanged event
        /// </summary>
        protected void OnPayloadChanged()
        {
            if (PayloadChanged != null) PayloadChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Description DependencyProperty definition.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(Caption), null);

        /// <summary>
        /// Gets or sets the description of the caption track.
        /// </summary>
        public string Description
        {
            get { return GetValue(DescriptionProperty) as string; }
            set { SetValue(DescriptionProperty, value); }
        }

        /// <summary>
        /// Payload DependencyProperty definition.
        /// </summary>
        public static readonly DependencyProperty PayloadProperty = DependencyProperty.Register("Payload", typeof(object), typeof(Caption), new PropertyMetadata(null, (d, o) => ((Caption)d).OnPayloadChanged()));

        /// <summary>
        /// Gets or sets the payload of the caption track. This can be any object.
        /// </summary>
        public object Payload
        {
            get { return GetValue(PayloadProperty) as object; }
            set { SetValue(PayloadProperty, value); }
        }

        /// <summary>
        /// Gets or sets the source Uri for the timed text. Useful for Xaml binding
        /// </summary>
        public Uri Source
        {
            get { return Payload as Uri; }
            set { Payload = value; }
        }

        /// <inheritdoc /> 
        public override string ToString()
        {
            return Description;
        }
    }
}
