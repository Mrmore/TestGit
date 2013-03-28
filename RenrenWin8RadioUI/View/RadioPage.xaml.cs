using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.ViewModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using RenrenWin8RadioUI;

namespace RenRenWin8Radio
{
    public sealed partial class RadioPage
    {
        uint radiosId;
        BitmapImage bitmapImage;
        RenRenResponseArg<SongListEntity> songList;
        BitmapImage tmpBitmap;
        int index = 0;
        bool _first = true;
        //加在封面
        BitmapImage loadBitmap;
        bool isload = false;

        public RadioPage(uint _radiosId)
        {
            InitializeComponent();
            radiosId = _radiosId;
            bitmapImage = new BitmapImage();
            this.Loaded -= new RoutedEventHandler(RadioPage_Loaded);
            this.Loaded += new RoutedEventHandler(RadioPage_Loaded);
            //单曲播放结束
            //mediaPlayer.MediaEnded += new RoutedEventHandler(mediaPlayer_MediaEnded);
            //touchR.ManipulationStarting += new Windows.UI.Xaml.Input.ManipulationStartingEventHandler(touchR_ManipulationStarting);
            //touchR.ManipulationStarted += new Windows.UI.Xaml.Input.ManipulationStartedEventHandler(touchR_ManipulationStarted);
            //touchR.ManipulationDelta += new Windows.UI.Xaml.Input.ManipulationDeltaEventHandler(touchR_ManipulationDelta);
            //touchR.ManipulationCompleted += new Windows.UI.Xaml.Input.ManipulationCompletedEventHandler(touchR_ManipulationCompleted);
            listBoxM.SelectionChanged += new SelectionChangedEventHandler(listBoxM_SelectionChanged);
        }

        async void listBoxM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            index = listBoxM.SelectedIndex;
            await Song();
        }

        //private void touchR_ManipulationCompleted(object sender, Windows.UI.Xaml.Input.ManipulationCompletedEventArgs e)
        //{
        //    e.Handled = true;
        //}

        //async void touchR_ManipulationDelta(object sender, Windows.UI.Xaml.Input.ManipulationDeltaEventArgs e)
        //{
        //    double original = e.ManipulationOrigin.X;
        //    if (e.DeltaManipulation.Translation.X > 0 || e.DeltaManipulation.Translation.Y > 0)
        //    {
        //        if (isload)
        //        {
        //            index++;
        //            if (index < RadioViewModel.Instance[radiosId].Songs.Count)
        //            {
        //                await Song();
        //            }
        //            else
        //            {
        //                index = 0;
        //                await Song();
        //            }
        //        }
        //        e.Handled = true;
        //        return;
        //    }

        //    if (e.DeltaManipulation.Translation.X < 0 || e.DeltaManipulation.Translation.Y < 0)
        //    {
        //        if (isload)
        //        {
        //            index--;
        //            if (index >= 0)
        //            {
        //                await Song();
        //            }
        //        }
        //        e.Handled = true;
        //        return;
        //    }
        //}

        //void touchR_ManipulationStarted(object sender, Windows.UI.Xaml.Input.ManipulationStartedEventArgs e)
        //{
        //    e.Handled = true;
        //}

        //void touchR_ManipulationStarting(object sender, Windows.UI.Xaml.Input.ManipulationStartingEventArgs e)
        //{
        //    e.Handled = true;
        //}

        async void mediaPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            index++;
            if (index < RadioViewModel.Instance[radiosId].Songs.Count)
            {
                await Song();
            }
            else
            {
                index = 0;
                await Song();
            }
        }

        private async void RadioPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_first)
            {
                _first = false;
                MusicSb.Begin();
                bitmapImage.UriSource = new Uri(UserInfoViewModel.Instance.Model.Head_url, UriKind.Absolute);
                imageUserInfo.Source = bitmapImage;
                tbUserInfo.Text = UserInfoViewModel.Instance.Model.User_name;
                
                await Song();
            }
        }

        private async Task Song()
        {
            if (RadioViewModel.Instance[radiosId] == null)
            {
                songList = await RadioViewModel.Instance.RequestSongListByRadioId(radiosId, true);
                if (songList.LocalError == null && songList.RemoteError == null)
                {
                    loadBitmap = new BitmapImage(new Uri(songList.Result.Songs.ElementAt(index).AlbumImg, UriKind.Absolute));
                    imageCover.Source = loadBitmap;
                    TbSongName.Text = songList.Result.Songs.ElementAt(index).Name;
                    TbSongWiter.Text = songList.Result.Songs.ElementAt(index).ArtistName;
                    Tbsongs.Text = songList.Result.Songs.ElementAt(index).AlbumInfo;
                    imageShare.Source = loadBitmap;
                    tbSSongName.Text = songList.Result.Songs.ElementAt(index).Name;
                    tbSSongWiter.Text = songList.Result.Songs.ElementAt(index).ArtistName;
                    sp.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    if (songList.Result.Songs.ElementAt(index).Favorited == "true")
                    {
                        btNoLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
                        btLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                    }
                    else
                    {
                        btNoLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                        btLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
                    }
                    //mediaPlayer.Volume = 1;
                    //mediaPlayer.Stop();
                    //mediaPlayer.Position = new TimeSpan(0);
                    ////mediaPlayer.Source = new Uri(mediaPlayer.BaseUri, RadioHomeViewModel.UriDecode(songList.Result.Songs.ElementAt(index).Source, true));
                    //mediaPlayer.Source = new Uri(RadioHomeViewModel.UriDecode(songList.Result.Songs.ElementAt(index).Source, true), UriKind.Absolute);
                    //mediaPlayer.Play();
                    isload = true;

                    listBoxM.ItemsSource = songList.Result.Songs;
                }
            }
            else
            {
                //RandomImageAnimation(index);
                isload = false;
                ImageClosedSb.Completed += ImageClosedSb_Completed;
                loadBitmap = new BitmapImage(new Uri(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).AlbumImg, UriKind.Absolute));
                ImageClosedSb.Begin();
                
            }
        }

        void ImageClosedSb_Completed(object sender, object e)
        {
            imageCover.Source = loadBitmap;
            ImageOpenSb.Completed += ImageOpenSb_Completed;
            ImageOpenSb.Begin();
            TbSongName.Text = RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Name;
            TbSongWiter.Text = RadioViewModel.Instance[radiosId].Songs.ElementAt(index).ArtistName;
            Tbsongs.Text = RadioViewModel.Instance[radiosId].Songs.ElementAt(index).AlbumInfo;
            imageShare.Source = loadBitmap;
            tbSSongName.Text = RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Name;
            tbSSongWiter.Text = RadioViewModel.Instance[radiosId].Songs.ElementAt(index).ArtistName;
            sp.Visibility = Windows.UI.Xaml.Visibility.Visible;
            if (RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Favorited == "true")
            {
                btNoLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
                btLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                btNoLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                btLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            //mediaPlayer.Volume = 1;
            //mediaPlayer.Stop();
            //mediaPlayer.Position = new TimeSpan(0);
            //mediaPlayer.Source = new Uri(RadioHomeViewModel.UriDecode(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Source, true), UriKind.Absolute);
            //mediaPlayer.Play();
        }

        void ImageOpenSb_Completed(object sender, object e)
        {
            isload = true;
        }

        //喜欢
        private async void btLike_Click(object sender, RoutedEventArgs e)
        {
            btLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            btNoLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
            //RadioViewModel.AddFavorite(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id);
            RenRenResponseArg<CommentReultEntity> result = await RadioViewModel.AddFavorite(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id);
        }

        //分享
        private void btShare_Click(object sender, RoutedEventArgs e)
        {
            //gridShare.Visibility = Windows.UI.Xaml.Visibility.Visible;
            gridShareOpen.Begin();
        }

        //前进
        private async void btPlayBackNext_Click(object sender, RoutedEventArgs e)
        {
            if (isload)
            {
                index++;
                if (index < RadioViewModel.Instance[radiosId].Songs.Count)
                {
                    await Song();
                }
                else
                {
                    index = 0;
                    await Song();
                }
            }
        }

        //后退
        private async void btPlayBackPrev_Click(object sender, RoutedEventArgs e)
        {
            if (isload)
            {
                index--;
                if (index >= 0)
                {
                    await Song();
                }            
            }
        }

        //不喜欢
        private async void btNoLike_Click(object sender, RoutedEventArgs e)
        {
            btLike.Visibility = Windows.UI.Xaml.Visibility.Visible;
            btNoLike.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            //RadioViewModel.RemoveFavorite(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id);
            RenRenResponseArg<CommentReultEntity> result = await RadioViewModel.RemoveFavorite(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id);
        }

        //确定分享
        private async void btSure_Click(object sender, RoutedEventArgs e)
        {
            //RadioViewModel.ShareSong(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id);
            //gridShare.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            RenRenResponseArg<CommentReultEntity> result = await RadioViewModel.ShareSong(RadioViewModel.Instance[radiosId].Songs.ElementAt(index).Id, "good study");
            //if (result.Result.Result == "1")
            //{
            //    ShareSb.Completed += new Windows.UI.Xaml.EventHandler(ShareSb_Completed);
            //    ShareSb.Begin();
            //}
            ShareSb.Completed += ShareSb_Completed;
            ShareSb.Begin();
        }

        void ShareSb_Completed(object sender, object e)
        {
            ShareSb.Stop();
        }

        //取消分享
        private void btCancel_Click(object sender, RoutedEventArgs e)
        {
            //gridShare.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            gridShareClosed.Begin();
        }

        #region 更换歌曲图片代码

        //随机切换图片效果
        private void RandomImageAnimation(int nextcurrent)
        {
            //零时存放下一张图片，用于实现动画切换效果
            Image tmpImage = new Image();
            tmpImage.Width = 350;
            tmpImage.Height = 320;
            tmpImage.Stretch = Stretch.Fill;
            tmpImage.Margin = new Thickness { Left = 0, Top = 0, Right = 0, Bottom = 0 };
            tmpBitmap = new BitmapImage(new Uri(RadioViewModel.Instance[radiosId].Songs.ElementAt(index + 1).AlbumImg, UriKind.Absolute)); 
            tmpImage.Source = tmpBitmap;
            temp.Children.Add(tmpImage);
            Random sbRandom = new Random();
            switch (sbRandom.Next(1, 4))
            {
                case 1:
                    temp.Transitions = new TransitionCollection()
                    {
                        new EntranceThemeTransition()
                    };
                    ImageStoryboard_Completed();
                    break;
                case 2:
                    temp.Transitions = new TransitionCollection()
                    {
                        new RepositionThemeTransition()
                    };
                    ImageStoryboard_Completed();
                    break;
                case 3:
                    temp.Transitions = new TransitionCollection()
                    {
                        new AddDeleteThemeTransition()
                    };
                    ImageStoryboard_Completed();
                    break;
            }
        }

        private void ImageStoryboard_Completed()
        {
            imageCover.Source = tmpBitmap;
            temp.Children.RemoveAt(0);
        }

        #endregion

        //回首页
        private void btGoHome_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = new Frame();

            rootFrame.Navigate(typeof(MainPage), null);
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        private void imageClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit(); 
        }

        private void cbList_Checked(object sender, RoutedEventArgs e)
        {
            spbt.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
            gridListCloseSb.Stop();
            gridListOpenSb.Begin();
        }

        private void cbList_Unchecked(object sender, RoutedEventArgs e)
        {
            gridListOpenSb.Stop();
            gridListCloseSb.Begin();
            spbt.Visibility = Windows.UI.Xaml.Visibility.Visible;
        }
    }
}
