using NotificationsExtensions.TileContent;
using RenRenAPI.Entity;
﻿using LightSensorLibrary;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.ViewModel;
using RenrenWin8RadioUI.DataModel;
using RenrenWin8RadioUI.View.Contracts.Share;
using RenrenWin8RadioUI.ViewModel;
using ShakeGestures;
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
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.UI.Notifications;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;
using RenrenWin8RadioUI.Helper;
using RenRenWin8Radio.Util;
using System.Diagnostics;
using RenRenWin8Radio.Model;
using Windows.ApplicationModel.Background;
using RenrenWin8RadioUI.Helper.LockScreenBackground;
using RenrenWin8RadioUI.Helper.LyricsHelper;
using RenrenWin8RadioUI.DataModel.LyricsData;
using System.Collections.ObjectModel;
using RenrenWin8RadioUI.Model;
using Windows.UI;
using RenrenWin8RadioUI.Helper.Animation;
using RenRenWin8Client.Helper;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace RenrenWin8RadioUI.UserControls
{
    public sealed partial class RadioPalyUserControl : UserControl, IDisposable
    {
        #region member
        private Uri baseUri = new Uri("ms-appx://");
        private Uri baseUrims = new Uri("ms-appdata://");
        private Button clickOptionBtn = null;
        //设置当前专辑的播放的第几首歌
        private int currentIndex = -1;
        //play to
        private Windows.Media.PlayTo.PlayToManager ptm = null;
        //文件夹名
        private const string myFolder = "matMp3Pictures";
        //用户设置UserSetting对象
        private UserSetting userSetting = null;
        //歌词显示timer
        private DispatcherTimer _progressRefreshTimer = null;
        //正在演唱的歌词
        private Lyrics _currentLyrics = null;
        #endregion

        #region event
        public Action DoLogin = null;
        public Action DoLogout = null;
        #endregion

        /// <summary>
        /// 构造
        /// </summary>
        public RadioPalyUserControl()
        {
            this.InitializeComponent();
            Init();


            //LockScreen();

            Windows.UI.ApplicationSettings.SettingsPane settingsPane = Windows.UI.ApplicationSettings.SettingsPane.GetForCurrentView();
            settingsPane.CommandsRequested += settingsPane_CommandsRequested;
            Windows.ApplicationModel.Search.SearchPane searchPane = Windows.ApplicationModel.Search.SearchPane.GetForCurrentView();
            searchPane.VisibilityChanged += searchPane_VisibilityChanged;
            SystemSettingHelper.Instance.SettingsPaneClick += Instance_SettingsPaneClick;
        }

        void Instance_SettingsPaneClick()
        {
            Init(false);
        }

        void searchPane_VisibilityChanged(Windows.ApplicationModel.Search.SearchPane sender, Windows.ApplicationModel.Search.SearchPaneVisibilityChangedEventArgs args)
        {
            
        }

        void settingsPane_CommandsRequested(Windows.UI.ApplicationSettings.SettingsPane sender, Windows.UI.ApplicationSettings.SettingsPaneCommandsRequestedEventArgs args)
        {
            
        }

        //初始化方法
        private void Init(bool isDeleteLocalFolder = true)
        {
            //查看本地设置
            var user = UserSettingListSave.Instance.UserList();
            if (user != null)
            {
                if (user.Count < 1)
                {
                    /*
                    //重力感应
                    //注册摇晃事件
                    ShakeGesturesHelper.Instance.ShakeGesture -= new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);
                    ShakeGesturesHelper.Instance.ShakeGesture += new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);
                    //设置可选参数(摇晃的力度，分为X轴Y轴Z轴) 默认5
                    ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 5;
                    //开始震动检测
                    ShakeGesturesHelper.Instance.Active = true;

                    //光感应
                    LightSensorHelper.Instance.MeasuredValue = 50;
                    LightSensorHelper.Instance.IlluminanceInLuxChange -= new Action<Windows.Devices.Sensors.LightSensorReading>(Instance_IlluminanceInLuxChange);
                    LightSensorHelper.Instance.IlluminanceInLuxChange += new Action<Windows.Devices.Sensors.LightSensorReading>(Instance_IlluminanceInLuxChange);
                    */

                    //设置电台的音量
                    mediaPlayer.Volume = 1;
                }
                else
                {
                    //UserSetting对象 查询保存电台的ID
                    userSetting = user.ElementAt(0);
                    if (userSetting != null)
                    {
                        mediaPlayer.Volume = userSetting.Volume;
                        if (userSetting.Gravity)
                        {
                            //重力感应
                            //注册摇晃事件
                            ShakeGesturesHelper.Instance.ShakeGesture -= new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);
                            ShakeGesturesHelper.Instance.ShakeGesture += new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);
                            //设置可选参数(摇晃的力度，分为X轴Y轴Z轴) 默认5
                            ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 5;
                            //开始震动检测
                            ShakeGesturesHelper.Instance.Active = true;
                        }
                        if (userSetting.Sensitive)
                        {
                            //光感应
                            LightSensorHelper.Instance.MeasuredValue = 50;
                            LightSensorHelper.Instance.IlluminanceInLuxChange -= new Action<Windows.Devices.Sensors.LightSensorReading>(Instance_IlluminanceInLuxChange);
                            LightSensorHelper.Instance.IlluminanceInLuxChange += new Action<Windows.Devices.Sensors.LightSensorReading>(Instance_IlluminanceInLuxChange);
                        }
                    }
                }
            }

            if (isDeleteLocalFolder)
            {
                //删除系统本地文件夹的myFolder文件夹及其子文件夹
                DelectLocalFolder();
            }
        }

        //注册锁屏和解锁
        private async void LockScreen()
        {
            try
            {
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex.Message);
            }
            RegisterLockScreen();
            RegisterUnLockScreen();
        }

        //锁屏
        private void RegisterLockScreen()
        {
            var isCheck = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (task.Name == "LockScreenBackgroundTask")
                {
                    task.Completed -= OnCompleted;
                    task.Completed += OnCompleted;
                    isCheck = true;
                }
                isCheck = false;
            }
            if (!isCheck)
            {
                var task = LockScreenBackgroundHelper.RegisterBackgroundTask("BackgroundTasks.LockScreenBackgroundTask",
                                                           "LockScreenBackgroundTask",
                                                            //new TimeTrigger(1,false),
                                                           new SystemTrigger(SystemTriggerType.UserAway, false),
                                                           null);
                                                            //new SystemCondition(SystemConditionType.UserPresent));
                task.Completed += new BackgroundTaskCompletedEventHandler(OnCompleted);
            }
        }

        //解锁
        private void RegisterUnLockScreen()
        {
            var isCheck = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks.Values)
            {
                if (task.Name == "UnLockScreenBackgroundTask")
                {
                    task.Completed -= unTask_Completed;
                    task.Completed += unTask_Completed;
                    isCheck = true;
                }
                isCheck = false;
            }
            if (!isCheck)
            {
                var unTask = LockScreenBackgroundHelper.RegisterBackgroundTask("BackgroundTasks.UnLockScreenBackgroundTask",
                                                           "UnLockScreenBackgroundTask",
                                                            //new TimeTrigger(1,false),
                                                           new SystemTrigger(SystemTriggerType.UserPresent, false),
                                                           null);
                                                           //new SystemCondition(SystemConditionType.UserPresent));
                unTask.Completed += unTask_Completed;
            }
        }

        private void OnCompleted(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            PlayOrStop();
        }

        private void unTask_Completed(BackgroundTaskRegistration sender, BackgroundTaskCompletedEventArgs args)
        {
            PlayOrStop();
        }

        #region 播放特性PlayTo,重力感应,光感应

        private Windows.UI.Core.CoreDispatcher dispatcher = Window.Current.CoreWindow.Dispatcher;
        /// <summary>
        /// PlayTo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SourceRequested(Windows.Media.PlayTo.PlayToManager sender,
                                     Windows.Media.PlayTo.PlayToSourceRequestedEventArgs e)
        {
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                try
                {
                    Windows.Media.PlayTo.PlayToSourceRequest sr = e.SourceRequest;
                    Windows.Media.PlayTo.PlayToSource controller = null;
                    Windows.Media.PlayTo.PlayToSourceDeferral deferral =
                        e.SourceRequest.GetDeferral();

                    try
                    {
                        controller = mediaPlayer.PlayToSource;
                    }
                    catch (Exception)
                    {

                    }
                    sr.SetSource(controller);
                    deferral.Complete();
                }
                catch (Exception)
                {

                }
            });
        }
        
        //光感应
        private async void Instance_IlluminanceInLuxChange(Windows.Devices.Sensors.LightSensorReading obj)
        {
            var luxLevel = obj.IlluminanceInLux;
            DateTimeOffset dateTimeOffset = obj.Timestamp;
            //做静音操作
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   mediaPlayer.IsMuted = true;
               });
            if (mediaPlayer.IsMuted != true)
            {
                NotificationHelper.DisplayTextTost("已为您静音", "通过光感应为您静音~~"+luxLevel.ToString());
            }
        }

        //重力感应
        private async void Instance_ShakeGesture(object sender, ShakeGestureEventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   //水平为控制声音 +
                   if (e.ShakeType == ShakeType.X)
                   {
                       if (mediaPlayer.Volume < 1.0)
                       {
                           mediaPlayer.Volume += 0.1;
                       }
                       else
                       {
                           mediaPlayer.Volume = 1.0;
                       }
                   }
                   //垂直为控制声音 -
                   else if (e.ShakeType == ShakeType.Y)
                   {
                       if (mediaPlayer.Volume > 0)
                       {
                           mediaPlayer.Volume -= 0.1;
                       }
                       else
                       {
                           mediaPlayer.Volume = 0.0;
                       }
                   }
                   //Z轴为下一首
                   else
                   {
                       NextSong();
                   }
               });
        }
        #endregion

        #region 初始化页面的前后台播放器控件
        //初始化MediaControl
        private void InitMediaControl(bool isIDisposable = false)
        {
            MediaControl.SoundLevelChanged -= MediaControl_SoundLevelChanged;
            MediaControl.PlayPauseTogglePressed -= MediaControl_PlayPauseTogglePressed;
            MediaControl.PlayPressed -= MediaControl_PlayPressed;
            MediaControl.PausePressed -= MediaControl_PausePressed;
            MediaControl.StopPressed -= MediaControl_StopPressed;
            MediaControl.NextTrackPressed -= MediaControl_NextTrackPressed;
            //MediaControl.PreviousTrackPressed -= MediaControl_PreviousTrackPressed;

            if (!isIDisposable)
            {
                MediaControl.SoundLevelChanged += MediaControl_SoundLevelChanged;
                MediaControl.PlayPauseTogglePressed += MediaControl_PlayPauseTogglePressed;
                MediaControl.PlayPressed += MediaControl_PlayPressed;
                MediaControl.PausePressed += MediaControl_PausePressed;
                MediaControl.StopPressed += MediaControl_StopPressed;
                MediaControl.NextTrackPressed += MediaControl_NextTrackPressed;
                //MediaControl.PreviousTrackPressed += MediaControl_PreviousTrackPressed;
            }
        }

        //初始化MediaPlayer
        private void InitMediaPlayer(bool isIDisposable = false)
        {
            mediaPlayer.VolumeChanged -= mediaPlayer_VolumeChanged;
            mediaPlayer.IsMutedChanged -= mediaPlayer_IsMutedChanged; 
            mediaPlayer.MediaOpened -= mediaPlayer_MediaOpened;
            mediaPlayer.CurrentStateChanged -= mediaPlayer_CurrentStateChanged;
            mediaPlayer.MediaEnded -= mediaPlayer_MediaEnded;

            if (!isIDisposable)
            {
                mediaPlayer.VolumeChanged += mediaPlayer_VolumeChanged;
                mediaPlayer.IsMutedChanged += mediaPlayer_IsMutedChanged; 
                mediaPlayer.MediaOpened += mediaPlayer_MediaOpened;
                mediaPlayer.MediaEnded += mediaPlayer_MediaEnded;
                mediaPlayer.MediaFailed += mediaPlayer_MediaFailed;
            }
        }
        #endregion

        #region 页面开放方法
        //回收垃圾
        public void Dispose()
        {
            DelectLocalFolder();
            UnRegisterShareSource();
            mediaPlayer.Dispose();
            LightSensorHelper.Instance.Dispose();
            //重力感应内建垃圾回收
            ShakeGesturesHelper.Instance.Active = false;
            InitMediaControl(true);
            InitMediaPlayer(true);
            if (ptm != null)
            {
                ptm.SourceRequested -= SourceRequested;
            }
            MusicSb.Stop();
            CDContent.Stop();
            WaveformSb.Stop();
            playCD.Completed -= playCD_Completed;
            StopCD.Completed -= StopCD_Completed;
            _progressRefreshTimer.Tick -= _progressRefreshTimer_Tick;
            _progressRefreshTimer.Stop();
            _progressRefreshTimer = null;
        }

        /// <summary>
        /// 前台按钮处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void optionBtn_Click(object sender, RoutedEventArgs e)
        {
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData == null || CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
            {
                //没有数据
                return;
            }

            var btn = sender as Button;
            if (btn == null)
            {
                return;
            }

            if (LoginViewModel.Instance.HasLogin)
            {
                clickOptionBtn = null;
                DoOption(btn.Name);
            }
            else
            {
                if (DoLogin != null)
                {
                    DoLogin();
                    clickOptionBtn = btn;
                }
            }
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
                        column0.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        settingBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        break;
                    }
                case ApplicationViewState.Snapped:
                    {
                        column0.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        settingBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        #endregion

        #region 前台播放器MediaPlayer控件
        //下载失败要跳过，到下一首
        private void mediaPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MusicSb.Stop();
            CDContent.Stop();
            WaveformSb.Stop();
            StopCD.Begin();
            NextSong();
        }

        //播放结束要跳过，到下一首，自动轮训
        private void mediaPlayer_MediaEnded(object sender, Microsoft.PlayerFramework.MediaPlayerActionEventArgs e)
        {
            MusicSb.Stop();
            CDContent.Stop();
            WaveformSb.Stop();
            StopCD.Begin();
            NextSong();
        }

        //播放器加载完毕打开
        private void mediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            mediaPlayer.CurrentStateChanged -= mediaPlayer_CurrentStateChanged;
            mediaPlayer.CurrentStateChanged += mediaPlayer_CurrentStateChanged;
        }

        //播放器静音
        private void mediaPlayer_IsMutedChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            
        }

        //播发器音量变化
        private void mediaPlayer_VolumeChanged(object sender, RoutedEventArgs e)
        {
             //查看本地设置
            var user = UserSettingListSave.Instance.UserList();
            if (user != null)
            {
                if (user.Count < 1)
                {
                    userSetting = new UserSetting();
                    userSetting.Volume = mediaPlayer.Volume;
                    UserSettingListSave.Instance.AddXml(userSetting);
                }
                else
                {
                    var setting = user.ElementAt(0);
                    setting.Volume = mediaPlayer.Volume;
                    UserSettingListSave.Instance.EditXml(setting);
                }
            }
            else
            { 
                
            }
        }

        //因为MediaPlayer封装了播放暂停的按钮，内建了播放方法，所以不用手动编写mediaPlayer.Pause() mediaPlayer.Play()
        private void mediaPlayer_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            switch (mediaPlayer.CurrentState)
            {
                case MediaElementState.Paused:
                    {
                        MusicSb.Pause();
                        CDContent.Pause();
                        WaveformSb.Pause();
                        StopCD.Begin();
                        break;
                    }
                case MediaElementState.Playing:
                    {
                        MusicSb.Resume();
                        CDContent.Resume();
                        WaveformSb.Resume();
                        playCD.Begin();
                        break;
                    }
                case MediaElementState.Stopped:
                    {
                        MusicSb.Stop();
                        CDContent.Stop();
                        WaveformSb.Stop();
                        StopCD.Begin();
                        break;
                    }
                case MediaElementState.Closed:
                    {
                        MusicSb.Stop();
                        CDContent.Stop();
                        WaveformSb.Stop();
                        StopCD.Begin();
                        break;
                    }
            }
        }
        #endregion

        #region 后台播放器MediaControl控件
        private async void MediaControl_SoundLevelChanged(object sender, object e)
        {
            try
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    SoundLevelToStringForVolume(MediaControl.SoundLevel);
                });
            }
            catch
            {

            }
        }

        private async void MediaControl_NextTrackPressed(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    MusicSb.Stop();
                    CDContent.Stop();
                    WaveformSb.Stop();
                    NextSong();
                });
        }

        private async void MediaControl_PausePressed(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MusicSb.Pause();
                CDContent.Pause();
                WaveformSb.Pause();
                mediaPlayer.Pause();
            });      
        }

        private async void MediaControl_StopPressed(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                MusicSb.Stop();
                CDContent.Stop();
                WaveformSb.Stop();
                mediaPlayer.Stop();
                UnRegisterShareSource();
            });
        }

        private async void MediaControl_PlayPressed(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                //MusicSb.Begin();
                //CDContent.Begin();
                //WaveformSb.Begin();
                //mediaPlayer.Play();
                MusicSb.Resume();
                CDContent.Resume();
                WaveformSb.Resume();
                mediaPlayer.PlayResume();
            });   
        }

        private async void MediaControl_PlayPauseTogglePressed(object sender, object e)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (mediaPlayer.CurrentState == MediaElementState.Playing)
                {
                    //MusicSb.Resume();
                    //CDContent.Resume();
                    //WaveformSb.Resume();
                    MusicSb.Pause();
                    CDContent.Pause();
                    WaveformSb.Pause();
                    mediaPlayer.Pause();
                }
                else if (mediaPlayer.CurrentState == MediaElementState.Stopped)
                {
                    UnRegisterShareSource();
                    MusicSb.Begin();
                    CDContent.Begin();
                    WaveformSb.Begin();
                    mediaPlayer.Play();
                }
                else if (mediaPlayer.CurrentState == MediaElementState.Paused)
                {
                    //MusicSb.Begin();
                    //CDContent.Begin();
                    //WaveformSb.Begin();
                    //mediaPlayer.Play();
                    MusicSb.Resume();
                    CDContent.Resume();
                    WaveformSb.Resume();
                    mediaPlayer.PlayResume();
                }
            });
        }

        private string SoundLevelToStringForVolume(SoundLevel level)
        {
            string LevelString = string.Empty;
            switch (level)
            {
                case SoundLevel.Muted:
                    LevelString = "Muted";
                    mediaPlayer.IsMuted = true;
                    break;
                case SoundLevel.Low:
                    LevelString = "Low";
                    mediaPlayer.IsMuted = false;
                    mediaPlayer.Volume = 0.3;
                    break;
                case SoundLevel.Full:
                    LevelString = "Full";
                    mediaPlayer.IsMuted = false;
                    mediaPlayer.Volume = 1.0;
                    break;
                default:
                    LevelString = "Unknown";
                    mediaPlayer.IsMuted = false;
                    mediaPlayer.Volume = 1.0;
                    break;
            }
            return LevelString;
        }

        /// <summary>
        /// 把WebUri下完，存放到本地。返回本地的Uri的Uri
        /// </summary>
        /// <param name="WebUri"></param>
        /// <returns></returns>
        private async Task<Uri> GetlocalUri(string WebUri)
        {
            var fileName = string.Empty;
            StorageFolder mp3LocalFolder = null;
            StorageFolder tempLocalFolder = ApplicationData.Current.LocalFolder;
            bool ismp3LocalFolder = false;
            try
            {
                mp3LocalFolder = await tempLocalFolder.CreateFolderAsync(myFolder, CreationCollisionOption.FailIfExists);
            }
            catch
            {
                //mp3LocalFolder = await tempLocalFolder.GetFolderAsync(myFolder);
                //mp3LocalFolder = tempLocalFolder.GetFolderAsync(myFolder).GetResults();
                ismp3LocalFolder = true;
            }
            if (ismp3LocalFolder)
            {
                mp3LocalFolder = await tempLocalFolder.GetFolderAsync(myFolder);
            }
            if (mp3LocalFolder != null)
            {
                StorageItemThumbnail storageItemThumbnail = await mp3LocalFolder.GetThumbnailAsync(ThumbnailMode.SingleItem, 500, ThumbnailOptions.ResizeThumbnail);

                StorageFile storageFile = null;
                var rass = RandomAccessStreamReference.CreateFromUri(new Uri(WebUri, UriKind.RelativeOrAbsolute));
                IRandomAccessStreamWithContentType streamRandom = await rass.OpenReadAsync();
                using (Stream tempStream = streamRandom.GetInputStreamAt(0).AsStreamForRead())
                {
                    MemoryStream ms = new MemoryStream();
                    await tempStream.CopyToAsync(ms);
                    byte[] bytes = ms.ToArray();

                    string specialCharacters = @"[\/:*?\""-<>|]";
                    WebUri = Regex.Replace(WebUri, specialCharacters, "");
                    fileName = WebUri + ".jpg";
                    storageFile = await mp3LocalFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
                    await FileIO.WriteBytesAsync(storageFile, bytes);
                }

                string filePath = storageFile.Path;
                return new Uri("ms-appdata:///local/" + myFolder + "/" + fileName);
            }
            else
            {
                return new Uri("ms-appdata:///local/");
            }
        }
        #endregion

        #region 封装的播放歌曲，下一首，刷新的方法
        /// <summary>
        /// 播放cd，开放接口
        /// </summary>
        /// <param name="cd"></param>
        async public void PlayCD(RadioItem cd)
        {
            mediaPlayer.CurrentStateChanged -= mediaPlayer_CurrentStateChanged;
            if (cd.Songs == null || cd.Songs.Count == 0)
            {
                return;
            }

            //设置cd的封面
            if (!string.IsNullOrEmpty(cd.AlbumCD))
            {
                CDimage.Source = new BitmapImage(new Uri("ms-appx://" + cd.AlbumCD));
            }
            else
            {
                CDimage.Source = new BitmapImage(new Uri("ms-appx:///Resources/defaul.jpg"));
            }

            cd.IsCheck = true;
            currentIndex = 0;
            LyricPanel.Children.Clear();
            songTitle.Text = string.Empty;

            SongEntity firstSont = cd.Songs.ElementAt(0);
            if (MusicSb.GetCurrentState() == Windows.UI.Xaml.Media.Animation.ClockState.Active)
            {
                StopCD.Begin();
                mediaPlayer.Stop();
                MusicSb.Stop();
                CDContent.Stop();
                WaveformSb.Stop();
                await Task.Delay(1500);
            }
            PalySong(firstSont);
        }

        /// <summary>
        /// 播放歌曲
        /// </summary>
        /// <param name="song"></param>
        private async void PalySong(SongEntity song)
        {
            //bool needPause = MainPageViewModel.Instance.CaculateTimeSpan();
            //if (needPause == true)
            //{
            //    //暂停
            //    return;
            //}            


            RefreshUI(song);

            UnRegisterShareSource();
            RegisterShareSource();
            UpdateTile();

            mediaPlayer.Stop();
            MusicSb.Stop();
            CDContent.Stop();
            WaveformSb.Stop();
            mediaPlayer.Position = new TimeSpan(0);
            mediaPlayer.Source = new Uri(RadioHomeViewModel.UriDecode(song.Source, LoginViewModel.Instance.HasLogin), UriKind.Absolute);
            mediaPlayer.Play();
            //MediaProtectionManager protectionManager = mediaPlayer.ProtectionManager;
            MediaControl.TrackName = song.Name;
            MediaControl.ArtistName = song.ArtistName;
            try
            {
                Uri localUri = await GetlocalUri(song.AlbumImg);
                MediaControl.AlbumArt = localUri;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 刷新歌曲内容
        /// </summary>
        /// <param name="song"></param>
        private void RefreshUI(SongEntity song)
        {
            if (song == null)
                return;

            //显示歌曲信息
            songName.Text = song.AlbumName;
            playerName.Text = song.ArtistName;
            songTitle.Text = song.Name;
            songTitleContainer.Visibility = Windows.UI.Xaml.Visibility.Visible;

            FindLyrics(song.ArtistName, song.Name);


            //填充歌曲图片信息
            if (!string.IsNullOrEmpty(song.AlbumImg))
            {
                songImg.ImageSource = new BitmapImage(new Uri(song.AlbumImg, UriKind.RelativeOrAbsolute));
            }
            else
            {
                songImg.ImageSource = new BitmapImage(new Uri("ms-appx:///Resources/defaul.jpg"));
            }

            //设置喜欢信息
            if (song.Favorited == "true")
            {
                like.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                unlike.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            else
            {
                like.Visibility = Windows.UI.Xaml.Visibility.Visible;
                unlike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 更新当前用户信息
        /// </summary>
        public void UpdateUserInfo(bool isSuccess = true)
        {
            if (isSuccess)
            {
                if (LoginViewModel.Instance.HasLogin && !string.IsNullOrEmpty(LoginViewModel.Instance.Model.Head_url))
                {
                    header.Source = new BitmapImage(new Uri(LoginViewModel.Instance.Model.Head_url, UriKind.RelativeOrAbsolute));
                    userName.Text = LoginViewModel.Instance.Model.User_name;
                    loginBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    logoutBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    header.Source = new BitmapImage(new Uri("ms-appx:///Resources/defaultUser.png"));
                    userName.Text = "未登录";
                    loginBtn.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    logoutBtn.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }

                //通过功能按钮触发的登陆事件，登陆成功后继续处理相关流程
                if (clickOptionBtn != null)
                {
                    DoOption(clickOptionBtn.Name);
                    clickOptionBtn = null;
                }
            }
            else
            {
                clickOptionBtn = null;
            }
            currentIndex = 0;
        }

        /// <summary>
        /// 向下轮训播放一首歌
        /// </summary>
        private async void NextSong()
        {
            LyricPanel.Children.Clear();
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData == null)
                return;
            if (CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
            {
                //没有数据
                return;
            }

            if (currentIndex < 0 || currentIndex >= CurrentRadioData.Songs.Count)
            {
                //index出错
                return;
            }

            currentIndex++;
            if (CurrentRadioData.Songs.Count - currentIndex <= 2)
            {
                await MainPageViewModel.Instance.RefreshCurrentRadio();
                CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
                if (CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
                {
                    return;
                }

                currentIndex = 0;
                SongEntity firstSont = CurrentRadioData.Songs.ElementAt(0);
                PalySong(firstSont);
            }
            else
            {
                SongEntity nextSong = CurrentRadioData.Songs[currentIndex];
                PalySong(nextSong);
            }
        }
        #endregion

        #region 页面的私有方法
        /// <summary>
        /// loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitMediaControl();
            InitMediaPlayer();
            //mediaPlayer.Volume = 1;
            mediaPlayer.AudioCategory = AudioCategory.BackgroundCapableMedia;
            //playto
            ptm = Windows.Media.PlayTo.PlayToManager.GetForCurrentView();
            if (ptm != null)
            {
                ptm.SourceRequested -= SourceRequested;
                ptm.SourceRequested += SourceRequested;
            }

            playCD.Completed += playCD_Completed;
            StopCD.Completed += StopCD_Completed;
            songDetail.Visibility = Windows.UI.Xaml.Visibility.Visible;

            _progressRefreshTimer = new DispatcherTimer();
            _progressRefreshTimer.Interval = new TimeSpan(500000);
            _progressRefreshTimer.Tick += _progressRefreshTimer_Tick;
        }

        /// <summary>
        /// 控件被移除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioPalyUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        /// <summary>
        /// 删除本地文件夹的方法
        /// </summary>
        private async void DelectLocalFolder()
        {
            try
            {
                //系统的本地文件夹不能被删除，只能删除里面的子文件夹或者没被占用的文件
                //StorageFolder tempLocalFolder = ApplicationData.Current.LocalFolder;
                //tempLocalFolder.DeleteAsync(StorageDeleteOption.PermanentDelete).GetResults();

                //删除系统本地文件夹的myFolder文件夹及其子文件夹
                StorageFolder tempLocalFolder = ApplicationData.Current.LocalFolder;
                StorageFolder mp3LocalFolder = await tempLocalFolder.GetFolderAsync(myFolder);
                if (mp3LocalFolder != null)
                {
                    await mp3LocalFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 动画结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void playCD_Completed(object sender, object e)
        {
            MusicSb.Resume();
            WaveformSb.Resume();
            CDContent.Resume();
            if (MusicSb.GetCurrentState() == Windows.UI.Xaml.Media.Animation.ClockState.Stopped)
            {
                MusicSb.Begin();
                CDContent.Begin();
            }
            if (WaveformSb.GetCurrentState() == Windows.UI.Xaml.Media.Animation.ClockState.Stopped)
            {
                WaveformSb.Begin();
            }
            Paused.Visibility = Windows.UI.Xaml.Visibility.Visible;
            play.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// 停止动画结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void StopCD_Completed(object sender, object e)
        {
            play.Visibility = Windows.UI.Xaml.Visibility.Visible;
            Paused.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        /// <summary>
        /// 下一首
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void next_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }

        /// <summary>
        /// 开始播放（大的播放按钮被按下）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void play_Click(object sender, RoutedEventArgs e)
        {
            //playCD.Begin();
            PlayOrStop();
        }

        //MediaPlayer CurrentState方法(播放,停止,暂停,继续)
        private void PlayOrStop()
        {
            switch (mediaPlayer.CurrentState)
            {
                case MediaElementState.Paused:
                    {
                        mediaPlayer.PlayResume();
                        break;
                    }
                case MediaElementState.Playing:
                    {
                        mediaPlayer.Pause();
                        break;
                    }
                case MediaElementState.Stopped:
                    {
                        mediaPlayer.Play();
                        break;
                    }
                case MediaElementState.Closed:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// 登出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void logoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DoLogout != null)
            {
                DoLogout();
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DoLogin != null)
            {
                DoLogin();
            }
        }

        /// <summary>
        /// 系统设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void settingBtn_Click(object sender, RoutedEventArgs e)
        {
            SystemSettingHelper.Instance.OpenSeting();
        }

        /// <summary>
        /// 页面按钮的操作
        /// </summary>
        /// <param name="name"></param>
        private async void DoOption(string name)
        {
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData == null || CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
            {
                //没有数据
                return;
            }

            if (currentIndex < 0 || currentIndex >= CurrentRadioData.Songs.Count)
            {
                //index出错
                return;
            }
            SongEntity playSong = CurrentRadioData.Songs[currentIndex];

            switch (name)
            {
                case "share":
                    {
                        RenRenResponseArg<CommentReultEntity> ret = await RadioViewModel.ShareSong(playSong.Id, "我正在听这首歌");
                        if (ret.RemoteError == null && ret.LocalError == null && ret.Result != null && ret.Result.Result == "1")
                        {
                            NotificationHelper.DisplayTextTost("分享成功", "嗯，大家都知道我在听这首歌了");
                        }
                        else
                        {
                            NotificationHelper.DisplayTextTost("分享失败", "由于某种不可抗力，分享失败了");
                        }
                        break;
                    }
                case "like":
                    {
                        RenRenResponseArg<CommentReultEntity> ret = await RadioViewModel.AddFavorite(playSong.Id);
                        if (ret.RemoteError == null && ret.LocalError == null && ret.Result != null && ret.Result.Result == "1")
                        {
                            like.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                            unlike.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        }
                        else
                        {
                            NotificationHelper.DisplayTextTost("喜欢失败", "这应该算一次暗恋吧");
                        }
                        break;
                    }
                case "unlike":
                    {
                        RenRenResponseArg<CommentReultEntity> ret = await RadioViewModel.RemoveFavorite(playSong.Id);
                        if (ret.RemoteError == null && ret.LocalError == null && ret.Result != null && ret.Result.Result == "1")
                        {
                            like.Visibility = Windows.UI.Xaml.Visibility.Visible;
                            unlike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        }
                        else
                        {
                            NotificationHelper.DisplayTextTost("取消喜欢失败", "算了吧，这就是缘分");
                        }
                        break;
                    }
                case "delete":
                    {
                        RenRenResponseArg<SongEntity> ret = await RadioViewModel.DeleteSong(playSong.Id, CurrentRadioData.Id);
                        if (ret.RemoteError == null && ret.LocalError == null)
                        {
                            NextSong();
                        }
                        else
                        {
                            NotificationHelper.DisplayTextTost("删除失败", "哎，将就吧。。。");
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }
        #endregion

        #region 作为分享源
        private ShareSourceWrapper _shareSource;

        /// <summary>
        /// 注册一个向外分享照片的入口
        /// </summary>
        private void RegisterShareSource()
        {
            if (_shareSource == null)
                _shareSource = new ShareSourceWrapper(SourceType.Image, this.SourceEntityRequest);
        }

        /// <summary>
        /// 反注册向外分享照片的入口
        /// </summary>
        private void UnRegisterShareSource()
        {
            if (_shareSource != null)
            {
                _shareSource.Reset();
                _shareSource = null;
            }
        }

        /// <summary>
        /// 填充各外分享所需要的entity
        /// </summary>
        /// <param name="entity"></param>
        private void SourceEntityRequest(SourceEntity entity)
        {
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
            {
                //没有数据
                return;
            }

            if (currentIndex < 0 || currentIndex >= CurrentRadioData.Songs.Count)
            {
                //index出错
                return;
            }
            SongEntity playSong = CurrentRadioData.Songs[currentIndex];
            if (playSong != null)
            {
                entity.Title = "from renren.com";
                entity.Description = playSong.Name;
                var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri(playSong.AlbumImg));
                entity.ImageStreamRef = streamRef;
                entity.Thumbnail = streamRef;
            }
        }
        #endregion

        #region 更新Tile

        private void UpdateTile()
        {
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData.Songs == null || CurrentRadioData.Songs.Count == 0)
            {
                //没有数据
                return;
            }

            if (currentIndex < 0 || currentIndex >= CurrentRadioData.Songs.Count)
            {
                //index出错
                return;
            }
            SongEntity playSong = CurrentRadioData.Songs[currentIndex];
            //UpdateRadioTile();
            UpdateSongTile(playSong);
        }

        private void UpdateRadioTile()
        {
            RadioItem CurrentRadioData = MainPageViewModel.Instance.CurrentRadioData;
            if (CurrentRadioData == null)
                return;

            // Create notification content based on a visual template.
            ITileWideImageAndText01 tileContent = TileContentFactory.CreateTileWideImageAndText01();

            tileContent.TextCaptionWrap.Text = CurrentRadioData.Name;

            // !Important!
            // The Internet (Client) capability must be checked in the manifest in the Capabilities tab
            // to display web images in tiles (either the http:// or https:// protocols)
            if (MainPageViewModel.Instance.RadioListViewModel != null && MainPageViewModel.Instance.RadioListViewModel.RadioDataList != null)
            {
                int index = (int)CurrentRadioData.Id;
                string folder = "/Resources/radios/radio";
                string imageUrl = "ms-appx://" + folder + index.ToString() + ".png";
                tileContent.Image.Src = imageUrl;
                tileContent.Image.Alt = "Web image";

                // Create square notification content based on a visual template.
                ITileSquareImage squareContent = TileContentFactory.CreateTileSquareImage();

                squareContent.Image.Src = imageUrl;
                squareContent.Image.Alt = "Web image";

                // include the square template.
                tileContent.SquareContent = squareContent;

                // send the notification to the app's application tile
                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());
            }
        }

        private void UpdateSongTile(SongEntity song)
        {
            if (song == null)
                return;

            ITileWidePeekImage05 tileContent = TileContentFactory.CreateTileWidePeekImage05();

            tileContent.ImageMain.Src = song.AlbumImg;
            tileContent.ImageSecondary.Src = song.AlbumImg;
            tileContent.TextHeading.Text = song.Name;
            tileContent.TextBodyWrap.Text = song.ArtistName + ":" +song.AlbumName;

            //tileContent.ImageMain.Text = song.ArtistName +":"+ song.Name;
            //tileContent.TextCaption2.Text = song.AlbumName;

            //string imageUrl = song.AlbumImg;
            //tileContent.Image.Src = imageUrl;
            //tileContent.Image.Alt = "Web image";

            // Create square notification content based on a visual template.
            ITileSquareImage squareContent = TileContentFactory.CreateTileSquareImage();

            squareContent.Image.Src = song.AlbumImg;
            squareContent.Image.Alt = "Web image";

            // include the square template.
            tileContent.SquareContent = squareContent;

            // send the notification to the app's application tile
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileContent.CreateNotification());
        }
        #endregion

        #region 歌词显示控制

        async void FindLyrics(string ArtistName, string Name)
        {
            LyricPanel.Children.Clear();
            CompositeTransform transform = LyricPanel.RenderTransform as CompositeTransform;
            transform.TranslateY = 0;

            //获取歌曲的歌词
            _currentLyrics = await LyricsHelper.GetLyrics(ArtistName, Name);
            LyricPanel.Children.Clear();
            if (_currentLyrics != null)
            {
                bool ret = InitLyrics();
                if (ret)
                {
                    _progressRefreshTimer.Start();
                    return;
                }
            }
            LyricsData data = InitNormalLyricsData("没有为你找到合适的歌词哦");
            DataTemplate dt = this.Resources["LyricTemplate"] as DataTemplate;
            TextBlock tb = dt.LoadContent() as TextBlock;
            tb.DataContext = data;
            LyricPanel.Children.Add(tb);
        }

        LyricsData _currentLyricData = null;
        /// <summary>
        /// 初始化歌词数据
        /// </summary>
        bool InitLyrics()
        {
            if (mediaPlayer != null)
            {
                TimeSpan currentTime = mediaPlayer.Position;
                _currentLyrics.Refresh();
                TimeSpan span = _currentLyrics.CurrentTimeSpan(currentTime);

                if (span == null)
                {
                    return false;
                }
                _progressRefreshTimer.Interval = span;
            }

            if (_currentLyrics.SortedTimes != null && _currentLyrics.SortedTimes.Count > 0)
            {
                for (int i = 0; i < _currentLyrics.SortedTimes.Count; i++)
                {
                    TimeSpan key = _currentLyrics.SortedTimes[i];
                    if (_currentLyrics.TimeAndLyrics.ContainsKey(key))
                    {
                        string lyricsTmp = string.Empty;
                        _currentLyrics.TimeAndLyrics.TryGetValue(key, out lyricsTmp);

                        LyricsData data = null;
                        if (i == 0)
                        {
                            data = InitCurrentLyricsData(lyricsTmp);
                            _currentLyricData = data;
                        }
                        else
                        {
                            data = InitNormalLyricsData(lyricsTmp);
                        }


                        DataTemplate dt = this.Resources["LyricTemplate"] as DataTemplate;
                        TextBlock tb = dt.LoadContent() as TextBlock;
                        tb.DataContext = data;
                        LyricPanel.Children.Add(tb);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 创建普通item
        /// </summary>
        /// <param name="content"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        LyricsData InitNormalLyricsData(string content)
        {
            LyricsData data = new LyricsData();
            data.LyricsText = content;
            data.FontSize = 14;
            data.TextForeground = new SolidColorBrush(Color.FromArgb(0xff, 0x6d, 0x4e, 0x37));
            return data;
        }

        /// <summary>
        /// 创建高亮显示的item
        /// </summary>
        /// <param name="content"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        LyricsData InitCurrentLyricsData(string content)
        {
            LyricsData data = new LyricsData();
            data.LyricsText = content;
            data.FontSize = 18;
            data.TextForeground = new SolidColorBrush(Color.FromArgb(0xff, 0x5b, 0x2e, 0x10));
            return data;
        }

        void CurrentToNormal(ref LyricsData current)
        {
            current.FontSize = 14;
            current.TextForeground = new SolidColorBrush(Color.FromArgb(0xff, 0x6d, 0x4e, 0x37));
        }

        void NormalToCurrent(ref LyricsData normal)
        {
            normal.FontSize = 18;
            normal.TextForeground = new SolidColorBrush(Color.FromArgb(0xff, 0x5b, 0x2e, 0x10));
        }

        /// <summary>
        /// time到
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void _progressRefreshTimer_Tick(object sender, object e)
        {
            if (mediaPlayer != null && mediaPlayer.CurrentState == MediaElementState.Playing 
                && _currentLyrics != null)
            {
                TimeSpan currentTime = mediaPlayer.Position;
                _currentLyrics.Refresh();
                TimeSpan span = _currentLyrics.CurrentTimeSpan(currentTime);
                if (span == null)
                {
                    return;
                }
                //Debug.WriteLine("span" + span.ToString());
                //Debug.WriteLine("currentTime" + currentTime.ToString());

                _progressRefreshTimer.Interval = span;

                int index = _currentLyrics.CurrentIndex;
                if (index > 0 && index < LyricPanel.Children.Count)
                {
                    if (_currentLyricData != null)
                    {
                        CurrentToNormal(ref _currentLyricData);
                    }

                    //测试scale代码---start---
                    if (index > 0)
                    {
                        TextBlock tbOrigin = LyricPanel.Children.ElementAt(index - 1) as TextBlock;
                        if (tbOrigin != null)
                        {
                            tbOrigin.RenderTransformOrigin = new Point(0.5, 0.5); 
                            ScaleAnimation.ScaleTo(tbOrigin, 1, 1, TimeSpan.FromSeconds(0.2), null);
                        }
                    }
                    TextBlock tb = LyricPanel.Children.ElementAt(index) as TextBlock;
                    tb.RenderTransformOrigin = new Point(0.5, 0.5);

                    ScaleAnimation.ScaleTo(tb, 1.1, 1.1, TimeSpan.FromSeconds(0.2), null);

                    //测试scale代码---end---

                    //TextBlock tb = LyricPanel.Children.ElementAt(index) as TextBlock;


                    _currentLyricData = tb.DataContext as LyricsData;
                    NormalToCurrent(ref _currentLyricData);
                }

                Action<FrameworkElement> completed = null;
                if (completed == null)
                {
                    completed = delegate(FrameworkElement fe)
                    {
                    };
                }

                if (index > 3)
                {
                    Rect LyricScrollViewerRect = GetElementRect(LyricScrollViewer);
                    IEnumerable<UIElement> list =
                        VisualTreeHelper.FindElementsInHostCoordinates(LyricScrollViewerRect, LyricPanel, false);

                    List<UIElement> newList = list.ToList();
                    double height = 0;
                    if (newList != null && newList.Count > 1)
                    {
                        FrameworkElement item = newList[newList.Count - 2] as FrameworkElement;
                        if (item != null)
                        {
                            height = item.ActualHeight;
                        }
                        Debug.WriteLine("Move:" + height.ToString());
                        MoveAnimation.MoveBy(LyricPanel, 0.0, -height, TimeSpan.FromSeconds(0.3), completed);
                    }
                }
            }
        }

        public Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(LyricScrollViewer.ActualWidth, LyricScrollViewer.ActualHeight - 40));
        }
        #endregion
    }
}
