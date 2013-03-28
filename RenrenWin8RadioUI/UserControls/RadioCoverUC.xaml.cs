using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using RenRenWin8Radio.Helper;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace RenrenWin8RadioUI.UserControls
{
    public sealed partial class RadioCoverUC
    {
        public RadioCoverUC()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Uri形式给图片赋值
        /// </summary>
        private string uri = string.Empty;
        public string Uri
        {
            get
            {
                return uri;
            }
            set
            {
                uri = value;
                if (uri != string.Empty)
                {
                    BitmapImage bitmapImage = new BitmapImage(new System.Uri(uri));
                    image.Source = bitmapImage;
                }
            }
        }

        /// <summary>
        /// BitmapImage图片源的时候给图片赋值
        /// </summary>
        private BitmapImage bitmap = null;
        public BitmapImage Bitmap
        {
            get
            {
                return bitmap;
            }
            set
            {
                bitmap = value;
                if (bitmap != null)
                {
                    image.Source = bitmap;
                }
            }
        }

        /// <summary>
        /// TitleTxt图片描述
        /// </summary>
        private string titleTxt = string.Empty;
        public string TitleTxt
        {
            get
            {
                return titleTxt;
            }
            set
            {
                titleTxt = value;
                if (titleTxt != string.Empty)
                {
                    tbTitle.Text = titleTxt;
                    grid.Visibility = Windows.UI.Xaml.Visibility.Visible;
                }
                else
                {
                    grid.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
        }

        private async Task<IRandomAccessStream> DownloadStream(string uri)
        {
            HttpClient getHc = new HttpClient();
            HttpResponseMessage result = await getHc.GetAsync(new Uri(uri));
            return MicrosoftStreamExtensions.AsRandomAccessStream(await result.Content.ReadAsStreamAsync());
        }

        private async void LoadImage()
        {
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.SetSource(await DownloadStream(Uri));
        }
    }
}
