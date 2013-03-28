using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Protection;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.FileProperties;
using RenrenWin8RadioUI.ViewModel;
using RenrenWin8RadioUI.DataModel;
using Windows.UI.ViewManagement;


namespace RenrenWin8RadioUI.UserControls
{
    public sealed partial class RadioList : UserControl
    {
        RadioListViewModel _viewModel = null;
        #region event
        public delegate void RadioItemEventHandler(object sender, RadioItem data);
        public event RadioItemEventHandler RadioItemClicked;
        #endregion

        public RadioList()
        {
            this.InitializeComponent();
            _viewModel = MainPageViewModel.Instance.RadioListViewModel;
            DataContext = _viewModel;
        }

        /// <summary>
        /// 处理三分之一的问题
        /// </summary>
        public void ViewChanged()
        {
            switch (ApplicationView.Value)
            {
                case ApplicationViewState.Filled:
                case ApplicationViewState.FullScreenLandscape:
                case ApplicationViewState.FullScreenPortrait:
                    {
                        snapMenu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        menu.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    }
                case ApplicationViewState.Snapped:
                    {
                        snapMenu.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        menu.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }


        void RadioList_RadioItemClicked(object sender, RadioItem data)
        {
            if (RadioItemClicked != null)
            {
                RadioItemClicked(this, data);
            }
        }
    }
}
