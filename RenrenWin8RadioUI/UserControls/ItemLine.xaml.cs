using RenrenWin8RadioUI.DataModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RenrenWin8RadioUI.UserControls
{
    public sealed partial class ItemLine : UserControl
    {
        #region event
        public delegate void RadioItemEventHandler(object sender, RadioItem data);
        public event RadioItemEventHandler RadioItemClicked;
        #endregion

        public ItemLine()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as RadioButton;
            if (btn == null)
            {
                return;
            }

            var mode = btn.DataContext as RadioItem;
            if (mode != null)
            {
                if (RadioItemClicked != null)
                {
                    RadioItemClicked(this, mode);
                }
            }
        }
    }
}
