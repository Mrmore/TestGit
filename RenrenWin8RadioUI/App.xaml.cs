using RenRenWin8Radio;
using RenrenWin8RadioUI.View.Contracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace RenrenWin8RadioUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static string TileId = string.Empty;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

            }

            if (!string.IsNullOrEmpty(args.TileId) && System.Text.RegularExpressions.Regex.IsMatch(args.TileId, "^SecondaryTile"))
            {
                //应用程序在三分之一处运行
                TileId = args.TileId.Split('.').Last();

                int uid = Convert.ToInt32(TileId);
                if (rootFrame == null || rootFrame.Content == null)
                {
                    rootFrame.Navigate(typeof(MainPage), uid);
                    Window.Current.Content = rootFrame;
                }
                else
                {
                    //设置uid啊
                    var main = rootFrame.Content as MainPage;
                    if (main == null)
                    {
                        return;
                    }
                    else
                    {
                        main.PlayRadio(uid);
                    }
                }
                Window.Current.Activate();
                return;
            }

            rootFrame.Navigate(typeof(MainPage), null);
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        private static RenRenAPI.API _renrenService = null;
        public static RenRenAPI.API RenRenService
        {
            get
            {
                lock (typeof(RenRenAPI.API))
                {
                    if (_renrenService == null)
                    {
                        _renrenService = new RenRenAPI.API();
                    }
                }
                return _renrenService;
            }
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        /// <summary>
        /// 在激活应用程序以显示搜索结果时调用。
        /// </summary>
        /// <param name="args">有关激活请求的详细信息。</param>
        protected override void OnSearchActivated(Windows.ApplicationModel.Activation.SearchActivatedEventArgs args)
        {
            // TODO: 在 OnWindowCreated 中注册 Windows.ApplicationModel.Search.SearchPane.GetForCurrentView().QuerySubmitted
            // 事件，以在应用程序运行后加快搜索

            // 如果窗口尚未使用框架导航，则插入我们自己的框架
            var previousContent = Window.Current.Content;
            var frame = previousContent as Frame;

            // 如果应用程序不包含顶级框架，则可能表示这是
            // 初次启动应用程序。一般而言，此方法和 App.xaml.cs 中的 OnLaunched 
            // 可调用公共方法。
            if (frame == null)
            {
                // 创建要充当导航上下文的框架，并将其与
                // SuspensionManager 键关联
                frame = new Frame();
            }

            frame.Navigate(typeof(SearchResultsPage), args.QueryText);
            Window.Current.Content = frame;

            // 确保当前窗口处于活动状态
            Window.Current.Activate();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);
        }
    }
}
