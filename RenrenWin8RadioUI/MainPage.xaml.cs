using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.Helper;
using RenRenWin8Radio.Model;
using RenRenWin8Radio.ViewModel;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using RenrenWin8RadioUI.DataModel;
using RenrenWin8RadioUI.UserControls;
using RenrenWin8RadioUI.ViewModel;
using Windows.UI.ViewManagement;
using RenrenWin8RadioUI.Helper.Notifications;
using RenrenWin8RadioUI.Helper;
using Windows.UI.Xaml.Navigation;

namespace RenRenWin8Radio
{
    partial class MainPage
    {
        MainPageViewModel _viewModel;
        int playId = -1;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;
            this.SizeChanged += MainPage_SizeChanged;
        }

        /// <summary>
        /// 进入页面默认播放
        /// </summary>
        /// <param name="id"></param>
        async public void PlayRadio(int id)
        {
            if (_viewModel.RadioListViewModel.RadioDataList != null && _viewModel.RadioListViewModel.RadioDataList.Count > 0)
            {
                var item = _viewModel.RadioListViewModel.FindRadio(id);
                if (item == null)
                {
                    return;
                }

                if (item.Songs == null)
                {
                    await _viewModel.GetSongList(item);
                }
                Player.PlayCD(item);
            }
        }

        /// <summary>
        /// 页面进入，注册相关事件，根据输入参数做相应处理
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Player.DoLogin += DoLogin;
            Player.DoLogout += DoLogout;
            SystemSettingHelper.Instance.Init(this);
            if (e.Parameter != null && e.NavigationMode == NavigationMode.New)
            {
                playId = Convert.ToInt32(e.Parameter);
            }

        }

        /// <summary>
        /// 页面离开，清理内容
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedFrom(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            Player.DoLogin -= DoLogin;
            Player.DoLogout -= DoLogout;
            SystemSettingHelper.Instance.Reset();
        }

        /// <summary>
        /// 处理三分之一 等问题
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            LayoutPage();
        }

        void LayoutPage()
        {
            LoginContent.Width = this.ActualWidth;
            Loading.Width = this.ActualWidth;
            Loading.Height = this.ActualHeight;
            Player.ViewChanged();
            AllRadioList.ViewChanged();

            switch (ApplicationView.Value)
            {
                case ApplicationViewState.Filled:
                case ApplicationViewState.FullScreenLandscape:
                case ApplicationViewState.FullScreenPortrait:
                    {
                        FullLayout.ScrollToHorizontalOffset(0);
                        if (870 + 1130 > this.ActualWidth)
                        {
                            Player.Width = 1130;
                        }
                        else
                        {
                            Player.Width = this.ActualWidth - 870;
                        }
                        FullLayout.HorizontalScrollMode = ScrollMode.Enabled;
                        break;
                    }
                case ApplicationViewState.Snapped:
                    {
                        FullLayout.ScrollToHorizontalOffset(0);
                        Player.Width = this.ActualWidth;
                        FullLayout.HorizontalScrollMode = ScrollMode.Disabled;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel = MainPageViewModel.Instance;
            if (_viewModel.HomeData == null)
            {
                await _viewModel.GetRadioData();
                Player.UpdateUserInfo();
            }

            if (playId >= 0)
            {
                PlayRadio(playId);
            }
            Loading.IsIndeterminate = false;
            Loading.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// 选电台
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        async void RadioList_RadioItemClicked(object sender, RadioItem data)
        {
            if (ApplicationView.Value == ApplicationViewState.Snapped)
            {
                FullLayout.ScrollToHorizontalOffset(0);
            }

            if (data != null)
            {
                if (_viewModel.SetCurrentRadio(data))
                {
                    await _viewModel.GetSongList(data);
                    Player.PlayCD(data);
                }
            }
        }

        /// <summary>
        /// 登陆页面关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void loginEvent(object sender, bool isSuccess)
        {
            login.IsOpen = false;
            if (isSuccess)
            {
                _viewModel.RefreshAll();
            }
            Player.UpdateUserInfo(isSuccess);
        }

        /// <summary>
        /// 检查是否登陆，是否已经pin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TheTopBar_Loaded(object sender, RoutedEventArgs e)
        {
            bool hasPin = false;

            if (_viewModel == null || _viewModel.CurrentRadioData == null)
            {
                //还未取得电台数据
                unpinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }

            if (_viewModel.CurrentRadioData.Name.Contains("私人电台"))
            {
                //还未取得电台数据
                unpinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                return;
            }

            if (_viewModel != null && _viewModel.CurrentRadioData != null)
            {
                hasPin = NotificatoinWrapper.CheckIfExist((int)(_viewModel.CurrentRadioData.Id));
            }

            if (hasPin)
            {
                unpinBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                pinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                unpinBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                pinBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
        }

        /// <summary>
        /// 打开登录页面
        /// </summary>
        public void DoLogin()
        {
            login.IsOpen = true;
            LoginContent.Width = ActualWidth;
            LoginContent.Height = ActualHeight;
        }

        /// <summary>
        /// 打开登录页面
        /// </summary>
        public void DoLogout()
        {
            LoginViewModel.Instance.HasLogin = false;
            LoginViewModel.Instance.Reset();
            UserInfoList.Instance.Reset();
            _viewModel.RefreshAll();
            Player.UpdateUserInfo();
        }

        /// <summary>
        /// pin to start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pinBtn_Click(object sender, RoutedEventArgs e)
        {
            NotificatoinWrapper.CreatePinByEntity(TileTyle.SecondaryTile, 
                new Pin2Entity()
                {
                    User_id = (int)_viewModel.CurrentRadioData.Id,
                    Head_url = _viewModel.CurrentRadioData.AlbumImg,
                    Large_Header = _viewModel.CurrentRadioData.AlbumImg,
                    User_name = _viewModel.CurrentRadioData.Name
                }).Pin();
            BottomBar.IsOpen = false;
        }

        /// <summary>
        /// unpin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unpinBtn_Click(object sender, RoutedEventArgs e)
        {
            if (NotificatoinWrapper.CheckIfExist((int)(_viewModel.CurrentRadioData.Id)))
            {
                NotificatoinWrapper.CreatePinByEntity(TileTyle.SecondaryTile,
                    new Pin2Entity()
                    {
                        User_id = (int)_viewModel.CurrentRadioData.Id,
                        Head_url = _viewModel.CurrentRadioData.AlbumImg,
                        Large_Header = _viewModel.CurrentRadioData.AlbumImg,
                        User_name = _viewModel.CurrentRadioData.Name
                    }).UnPin();
            }
            BottomBar.IsOpen = false;
        }

        /// <summary>
        /// 分享
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shareBtn_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.IsOpen = false;
            Player.optionBtn_Click(sender, e);
        }

        private void fmBtn_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.IsOpen = false;
            if (ApplicationView.Value == ApplicationViewState.Snapped)
            {
                if (FullLayout.HorizontalOffset >= 300)
                {
                    FullLayout.ScrollToHorizontalOffset(0);
                }
                else
                {
                    FullLayout.ScrollToHorizontalOffset(320);
                }
            }
            else
            {
                double len = 0;
                if (870 + 1130 > this.ActualWidth)
                {
                    len = 870 + +1130 - this.ActualWidth;
                }

                if (FullLayout.HorizontalOffset >= len / 2)
                {
                    FullLayout.ScrollToHorizontalOffset(0);
                }
                else
                {
                    FullLayout.ScrollToHorizontalOffset(len);
                }
            }
        }

        private void refresh_Click(object sender, RoutedEventArgs e)
        {
            BottomBar.IsOpen = false;
            _viewModel.RefreshAll();
        }
    }
}
