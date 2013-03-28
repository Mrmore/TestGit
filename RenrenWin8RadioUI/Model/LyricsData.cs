using RenRenWin8Radio.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace RenrenWin8RadioUI.Model
{
    public class LyricsData : PropertyChangedBase
    {
        /// <summary>
        /// 歌词
        /// </summary>
        private string lyricsText;
        public string LyricsText
        {
            get
            {
                return lyricsText;
            }
            set
            {
                lyricsText = value;
                this.NotifyPropertyChanged(LyricsData => LyricsData.LyricsText);
            }
        }

        /// <summary>
        /// 时间
        /// </summary>
        private TimeSpan lyricsTime;
        public TimeSpan LyricsTime
        {
            get
            {
                return lyricsTime;
            }
            set
            {
                lyricsTime = value;
                this.NotifyPropertyChanged(LyricsData => LyricsData.LyricsTime);
            }
        }

        /// <summary>
        /// 文字颜色
        /// </summary>
        private SolidColorBrush textForeground;
        public SolidColorBrush TextForeground
        {
            get
            {
                return textForeground;
            }
            set
            {
                textForeground = value;
                this.NotifyPropertyChanged(LyricsData => LyricsData.TextForeground);
            }
        }

        /// <summary>
        /// 字号
        /// </summary>
        private int fontSize;
        public int FontSize
        {
            get
            {
                return fontSize;
            }
            set
            {
                fontSize = value;
                this.NotifyPropertyChanged(LyricsData => LyricsData.FontSize);
            }
        }
    }
}
