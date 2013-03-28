using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.Model;
using RenRenWin8Radio.ViewModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace RenrenWin8RadioUI.UserControls
{
    public sealed partial class Login : UserControl
    {
        #region event
        public delegate void LgoinEventHandler(object sender, bool isSuccess);
        public event LgoinEventHandler LgoinEvent;
        #endregion

        UserInfoList userInfo = null;

        public Login()
        {
            this.InitializeComponent();
            userInfo = UserInfoList.Instance;
            AssertKey.Height = 0;
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            switch (ApplicationView.Value)
            {
                case ApplicationViewState.Filled:
                case ApplicationViewState.FullScreenLandscape:
                case ApplicationViewState.FullScreenPortrait:
                    {
                        content.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                        content.Width = 400;
                        break;
                    }
                case ApplicationViewState.Snapped:
                    {
                        content.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                        content.Width = 280;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        private void PasswordTbx_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                LoginBtn_Click(null, null);
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrEmpty(UsernameTbx.Text.Trim()) || 
                string.IsNullOrEmpty(PasswordTbx.Password.Trim()))
            {
                ErrorTbk.Text = "请先输入用户名/密码";
            }
            else
            {
                login(UsernameTbx.Text, PasswordTbx.Password, AssertText.Text);
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            UsernameTbx.Text = string.Empty;
            PasswordTbx.Password = string.Empty;
            ErrorTbk.Text = string.Empty;
            if (LgoinEvent != null)
            {
                LgoinEvent(this, false);
            }
            AssertKey.Height = 0;
        }

        private async void login(string userName, string password, string assertKey)
        {
            ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Visible;
            RenRenResponseArg<UserEntity> resp = await LoginViewModel.Instance.Login(userName, password, assertKey);
            ProgressBar.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            if (resp.LocalError == null && resp.RemoteError == null)
            {
                UserInfo info = new UserInfo();
                userInfo.Reset();
                info.UserName = userName;
                info.PassWord = password;
                userInfo.AddXml(info);
                LoginViewModel.Instance.HasLogin = true;
                if (LgoinEvent != null)
                {
                    LgoinEvent(this, LoginViewModel.Instance.HasLogin);
                }
            }
            else
            {
                if (resp.RemoteError != null && resp.RemoteError.Error_code == 10006 && resp.RemoteError.Error_msg != null)
                {
                    BitmapImage image = new BitmapImage(new Uri(resp.RemoteError.Error_msg, UriKind.RelativeOrAbsolute));
                    AssertImage.Source = image;
                    AssertKey.Height = 30;
                    return;
                }

                string errorMsg = string.Empty;
                if (resp.LocalError != null)
                {
                    errorMsg = resp.LocalError.Message;
                }
                else
                {
                    errorMsg = resp.RemoteError.Error_msg;
                }
                ErrorTbk.Text = errorMsg;
                LoginViewModel.Instance.HasLogin = false;
            }
        }

        private void UsernameTbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            ErrorTbk.Text = string.Empty;
        }

        private void PasswordTbx_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ErrorTbk.Text = string.Empty;
        }

        private void AssertText_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}
