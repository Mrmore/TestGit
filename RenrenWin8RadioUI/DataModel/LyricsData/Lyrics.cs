using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RenrenWin8RadioUI.DataModel.LyricsData
{
    public class Lyrics
    {
        #region values

        /// <summary>
        /// 获取LRC歌词代码
        /// </summary>
        public string LrcCode { get; private set; }
        /// <summary>
        /// 获取原始字典
        /// </summary>
        public Dictionary<string, string> Dictionary { get; private set; }
        /// <summary>
        /// 时间、歌词字典
        /// </summary>
        public Dictionary<TimeSpan, string> TimeAndLyrics { get; set; }
        /// <summary>
        /// 排过序的时间列表
        /// </summary>
        public List<TimeSpan> SortedTimes { get; private set; }
        /// <summary>
        /// 返回歌词的标题
        /// </summary>
        public string Title { get; private set; }
        /// <summary>
        /// 返回歌词的专辑名称
        /// </summary>
        public string Album { get; private set; }
        /// <summary>
        /// 返回歌词的表演者
        /// </summary>
        public string Artist { get; private set; }
        /// <summary>
        /// 返回歌词的制作者
        /// </summary>
        public string LyricsMaker { get; private set; }
        /// <summary>
        /// 获取LRC歌词的偏移
        /// </summary>
        public TimeSpan Offset { get; private set; }

        /// <summary>
        /// 返回当前的歌词,使用前请先调用Refresh()函数
        /// </summary>
        public string CurrentLyrics { get; private set; }
        /// <summary>
        /// 返回下一个歌词,使用前请先调用Refresh()函数
        /// </summary>
        public string NextLyrics { get; private set; }
        /// <summary>
        /// 返回下二个歌词,使用前请先调用Refresh()函数
        /// </summary>
        public string NextLyrics2 { get; private set; }
        /// <summary>
        /// 返回上一个歌词,使用前请先调用Refresh()函数
        /// </summary>
        public string PreviousLyrics { get; private set; }
        /// <summary>
        /// 返回上二个歌词,使用前请先调用Refresh()函数
        /// </summary>
        public string PreviousLyrics2 { get; private set; }
        /// <summary>
        /// 返回当前歌词的Index,使用前请先调用Refresh()函数
        /// </summary>
        public int CurrentIndex { get; private set; }
		

        #endregion


        #region build

        /// <summary>
        /// 通过指定的Lrc代码初始化LyricParser实例
        /// </summary>
        /// <param name="code">Lrc代码</param>
        public Lyrics(string code)
        {
            Debug.WriteLine(code);
            LrcCode = code;
            LrcCodeParse();
            DictionaryParse();
            GetSortedTimes();
            CurrentIndex = -1;
            Debug.WriteLine("Lyrics initial" + CurrentIndex.ToString());
        }

        #endregion

        #region protected functions

        /// <summary>
        /// 第一次处理，生成原始字典
        /// </summary>
        protected void LrcCodeParse()
        {
            Dictionary = new Dictionary<string, string>();
            string[] lines = LrcCode.Replace(@"\'", "'").Split(new char[2] { '\r', '\n' });
            int i;
            for (i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                {
                    Match mc = Regex.Match(lines[i], @"((?'titles'\[.*?\])+)(?'content'.*)", RegexOptions.None);
                    if (mc.Success)
                    {
                        string content = mc.Groups["content"].Value;
                        foreach (Capture title in mc.Groups["titles"].Captures)
                            Dictionary[title.Value] = content;		//不要用Add方法，有可能有重复项
                    }
                }
            }
        }
        /// <summary>
        /// 第二次处理，生成时间、歌词字典，找到歌词的作者等属性
        /// </summary>
        protected void DictionaryParse()
        {
            TimeAndLyrics = new Dictionary<TimeSpan, string>();
            foreach (var keyvalue in Dictionary)
            {
                {
                    //分析时间
                    Match mc = Regex.Match(keyvalue.Key, @"\[(?'minutes'\d+):(?'seconds'\d+(\.\d+)?)\]", RegexOptions.None);
                    if (mc.Success)
                    {
                        int minutes = int.Parse(mc.Groups["minutes"].Value);
                        double seconds = double.Parse(mc.Groups["seconds"].Value);
                        TimeSpan key = new TimeSpan(0, 0, minutes, (int)Math.Floor(seconds), (int)((seconds - Math.Floor(seconds)) * 1000));
                        string value = keyvalue.Value;

                        //去空格
                        if (value.Length > 0)
                        {
                            TimeAndLyrics[key] = value;
                        }
                    }
                }
                {
                    //分析歌词的附带属性
                    Match mc = Regex.Match(keyvalue.Key, @"\[(?'title'.+?):(?'content'.*)\]", RegexOptions.None);
                    if (mc.Success)
                    {
                        string title = mc.Groups["title"].Value.ToLower();
                        string content = mc.Groups["content"].Value;
                        if (title == "ti") Title = content;
                        if (title == "ar") Artist = content;
                        if (title == "al") Album = content;
                        if (title == "by") LyricsMaker = content;
                        if (title == "offset") Offset = new TimeSpan(10000 * int.Parse(content));
                    }
                }
            }
        }
        /// <summary>
        /// 从时间、歌词字典中返回排过序的时间列表
        /// </summary>
        protected void GetSortedTimes()
        {
            TimeSpan[] timesArray = new TimeSpan[TimeAndLyrics.Count];
            TimeAndLyrics.Keys.CopyTo(timesArray, 0);
            SortedTimes = new List<TimeSpan>(timesArray);
            SortedTimes.Sort();
        }

        #endregion

        #region refresh functions

        /// <summary>
        /// 使用指定的时间刷新实例的当前歌词
        /// 使用场景：前台固定timer
        /// </summary>
        /// <param name="time">时间</param>
        public void Refresh(TimeSpan time)
        {
            if (SortedTimes.Count == 0)
            {
                CurrentIndex = -1;
                PreviousLyrics = null;
                PreviousLyrics2 = null;
                CurrentLyrics = null;
                NextLyrics = null;
                NextLyrics2 = null;
            }
            else
            {
                TimeSpan time2 = time + Offset;
                while (true)
                {
                    if (CurrentIndex >= 0 && CurrentIndex < SortedTimes.Count && SortedTimes[CurrentIndex] > time2)
                        --CurrentIndex;
                    else if (CurrentIndex + 1 < SortedTimes.Count && SortedTimes[CurrentIndex + 1] <= time2)
                        ++CurrentIndex;
                    else break;
                }
                if (CurrentIndex - 1 >= 0 && CurrentIndex - 1 < SortedTimes.Count)
                {
                    PreviousLyrics = TimeAndLyrics[SortedTimes[CurrentIndex - 1]];
                    if (CurrentIndex - 2 >= 0 && CurrentIndex - 2 < SortedTimes.Count)
                    {
                        PreviousLyrics2 = TimeAndLyrics[SortedTimes[CurrentIndex - 2]];
                    }
                    else
                    {
                        PreviousLyrics2 = null;
                    }
                }
                else
                {
                    PreviousLyrics = null;
                    PreviousLyrics2 = null;
                }
                if (CurrentIndex >= 0 && CurrentIndex < SortedTimes.Count)
                {
                    CurrentLyrics = TimeAndLyrics[SortedTimes[CurrentIndex]];
                }
                else
                {
                    CurrentLyrics = null;
                }
                if (CurrentIndex + 1 >= 0 && CurrentIndex + 1 < SortedTimes.Count)
                {
                    NextLyrics = TimeAndLyrics[SortedTimes[CurrentIndex + 1]];
                    if (CurrentIndex + 2 >= 0 && CurrentIndex + 2 < SortedTimes.Count)
                    {
                        NextLyrics2 = TimeAndLyrics[SortedTimes[CurrentIndex + 2]];
                    }
                    else
                    {
                        NextLyrics2 = null;
                    }
                }
                else
                {
                    NextLyrics = null;
                    NextLyrics2 = null;
                }
            }
        }

        /// <summary>
        /// 使用指定的时间刷新实例的当前歌词
        /// 使用场景：前台非固定timer
        /// </summary>
        public void Refresh()
        {
            if (SortedTimes.Count == 0)
            {
                CurrentIndex = -1;
                PreviousLyrics = null;
                PreviousLyrics2 = null;
                CurrentLyrics = null;
                NextLyrics = null;
                NextLyrics2 = null;
            }
            else
            {
                CurrentIndex++;
                Debug.WriteLine("Lyrics Refresh" + CurrentIndex.ToString());
            }
        }

        public TimeSpan CurrentTimeSpan(TimeSpan time)
        {
            TimeSpan returnTimeSpan = TimeSpan.Zero;
            if (CurrentIndex >= 0 && CurrentIndex < SortedTimes.Count-1)
            {
                TimeSpan nextTime = SortedTimes[CurrentIndex+1];
                if (time != null && nextTime != null)
                {
                    returnTimeSpan = nextTime - time;
                }

                ////精确到秒
                //if ((int)returnTimeSpan.TotalSeconds == 0)
                //{
                //    if (CurrentIndex < SortedTimes.Count - 2)
                //    {
                //        nextTime = SortedTimes[CurrentIndex + 2];
                //        if (time != null && nextTime != null)
                //        {
                //            returnTimeSpan = nextTime - time;
                //        }
                //    }
                //    else if (CurrentIndex == SortedTimes.Count - 2)
                //    {
                //        returnTimeSpan = TimeSpan.FromSeconds(30);
                //    }
                //    CurrentIndex++;
                //}
            }
            else if (CurrentIndex == SortedTimes.Count - 1)
            {
                returnTimeSpan = TimeSpan.FromSeconds(30);
            }
            return returnTimeSpan;
               
        }

        #endregion
    }
}
