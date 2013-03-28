using LightSensorLibrary;
using RenRenWin8Radio;
using RenRenWin8Radio.Model;
using RenRenWin8Radio.ViewModel;
using RenrenWin8RadioUI.UserControls;
using RenrenWin8RadioUI.ViewModel;
using ShakeGestures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.UI.ApplicationSettings;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;

namespace RenrenWin8RadioUI.Helper
{
    public class SystemSettingHelper 
    {
        SettingsPane _appSettings;
        private static SystemSettingHelper _settingInstance = null;
        MainPage _mainPage = null;
        UserSetting setting = null;

        public delegate void SettingsPaneHandler();
        public event SettingsPaneHandler SettingsPaneClick;

        public static SystemSettingHelper Instance
        {
            get
            {
                lock (typeof(SystemSettingHelper))
                {
                    if (_settingInstance == null)
                    {
                        _settingInstance = new SystemSettingHelper();
                    }
                }
                return _settingInstance;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init(Page mainPage)
        {
            _appSettings = SettingsPane.GetForCurrentView();
            _appSettings.CommandsRequested -= appSettings_CommandsRequested;
            _appSettings.CommandsRequested += appSettings_CommandsRequested;
            _mainPage = mainPage as MainPage;
        }

        public void Reset()
        {
            _appSettings.CommandsRequested -= appSettings_CommandsRequested;
        }

        public void OpenSeting()
        {
            SettingsPane.Show();
        }

        /// <summary>
        /// 事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void appSettings_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var vector = args.Request.ApplicationCommands;
           
            if (LoginViewModel.Instance.HasLogin)
            {
                var handler = new UICommandInvokedHandler(OnLogoutSetting);
                vector.Add(new SettingsCommand("general.logoutsetting", "注销", handler));
            }
            else
            {
                var handler = new UICommandInvokedHandler(OnLoginSetting);
                vector.Add(new SettingsCommand("general.loginsetting", "登录", handler));
            }

            ObservableCollection<UserSetting> list = UserSettingListSave.Instance.UserList(); 
            if (list != null && list.Count > 0)
            {
                setting = UserSettingListSave.Instance.UserList()[0];
                var handler = new UICommandInvokedHandler(OnGravitySetting);
                if (setting.Gravity)
                {
                    vector.Add(new SettingsCommand("general.GravityClosesetting", "关闭重力感应", handler));
                }
                else
                {
                    vector.Add(new SettingsCommand("general.GravityOpensetting", "打开重力感应", handler));
                }


                //handler = new UICommandInvokedHandler(OnSensitiveSetting);
                //if (setting.Sensitive)
                //{
                //    vector.Add(new SettingsCommand("general.SensitiveClosesetting", "关闭光感", handler));
                //}
                //else
                //{
                //    vector.Add(new SettingsCommand("general.SensitiveOpensetting", "打开光感", handler));
                //}
            }
            else
            {
                setting = new UserSetting();
                UserSettingListSave.Instance.AddXml(setting);
                var handler = new UICommandInvokedHandler(OnGravitySetting);
                vector.Add(new SettingsCommand("general.GravityOpensetting", "打开重力感应", handler));

                //handler = new UICommandInvokedHandler(OnSensitiveSetting);
                //vector.Add(new SettingsCommand("general.SensitiveOpensetting", "打开光感", handler));
            }

            var hand = new UICommandInvokedHandler(OnShowSetting);
            vector.Add(new SettingsCommand("general.Showsetting", "隐私声明", hand));
        }

        public void OnGravitySetting(object command)
        {
            var settingCommand = command as SettingsCommand;
            if (settingCommand.Id.ToString() == "general.GravityClosesetting")
            {
                setting.Gravity = false;
                ShakeGesturesHelper.Instance.Active = false;
            }
            else
            {
                setting.Gravity = true;
                ShakeGesturesHelper.Instance.Active = true;
            }
            UserSettingListSave.Instance.EditXml(setting);
            if (SettingsPaneClick != null)
            {
                this.SettingsPaneClick();
            }
        }

        public void OnSensitiveSetting(object command)
        {
            //var settingCommand = command as SettingsCommand;
            //if ("general.SensitiveClosesetting" == settingCommand.Id.ToString())
            //{
            //    setting.Sensitive = false;
            //    LightSensorHelper.Instance.MeasuredValue = -50;
            //}
            //else
            //{
            //    setting.Sensitive = true;
            //    LightSensorHelper.Instance.MeasuredValue = 50;
            //}
            //UserSettingListSave.Instance.EditXml(setting);
            //if (SettingsPaneClick != null)
            //{
            //    this.SettingsPaneClick();
            //}
        }

        public void OnLoginSetting(object command)
        {
            _mainPage.DoLogin();
        }


        private Popup settingsPopup;
        private double settingsWidth = 340;
        private Rect windowBounds;

        public void OnShowSetting(object command)
        {
            // Create a Popup window which will contain our flyout.
            settingsPopup = new Popup();
            settingsPopup.Closed += OnPopupClosed;
            Window.Current.Activated += OnWindowActivated;
            settingsPopup.IsLightDismissEnabled = true;
            settingsPopup.Width = settingsWidth;
            settingsPopup.Height = windowBounds.Height;

            // Add the proper animation for the panel.
            settingsPopup.ChildTransitions = new TransitionCollection();
            settingsPopup.ChildTransitions.Add(new PaneThemeTransition()
            {
                Edge = (SettingsPane.Edge == SettingsEdgeLocation.Right) ?
                       EdgeTransitionLocation.Right :
                       EdgeTransitionLocation.Left
            });

            // Create a SettingsFlyout the same dimenssions as the Popup.
            SettingFlyout mypane = new SettingFlyout();
            mypane.Width = settingsWidth;
            mypane.Height = windowBounds.Height;

            // Place the SettingsFlyout inside our Popup window.
            settingsPopup.Child = mypane;

            // Let's define the location of our Popup.
            settingsPopup.SetValue(Canvas.LeftProperty, SettingsPane.Edge == SettingsEdgeLocation.Right ? (windowBounds.Width - settingsWidth) : 0);
            settingsPopup.SetValue(Canvas.TopProperty, 0);
            settingsPopup.IsOpen = true;
        }

        //注销
        public void OnLogoutSetting(object command = null)
        {
            _mainPage.DoLogout();
        }

        /// <summary>
        /// We use the window's activated event to force closing the Popup since a user maybe interacted with
        /// something that didn't normally trigger an obvious dismiss.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private void OnWindowActivated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
            {
                settingsPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// When the Popup closes we no longer need to monitor activation changes.
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        void OnPopupClosed(object sender, object e)
        {
            Window.Current.Activated -= OnWindowActivated;
        }

        // protected against outside creating
        private SystemSettingHelper()
        {
            windowBounds = Window.Current.Bounds;
        }
    }
}
