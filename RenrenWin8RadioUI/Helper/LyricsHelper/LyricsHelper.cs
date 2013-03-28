﻿using RenrenWin8RadioUI.DataModel.LyricsData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RenrenWin8RadioUI.Helper.LyricsHelper
{
    public static class LyricsHelper
    {
        /// <summary>
		/// 歌词服务器列表
		/// </summary>
		private static readonly string[] servers = new string[] { "ttlrcct.qianqian.com", "ttlrcct2.qianqian.com" };

        /// <summary>
        /// 获取歌词
        /// </summary>
        /// <param name="artist">表演者</param>
        /// <param name="title">标题</param>
        async public static Task<Lyrics> GetLyrics(string artist, string title)
        {
            if (string.IsNullOrEmpty(artist) && string.IsNullOrEmpty(title)) return null;
            if (title.ToLower().Contains("instrumental")) return null;

            //获取所有可能的歌词
            foreach (var server in servers)
            {
                Uri requestUrl = new Uri("http://" + server + "/dll/lyricsvr.dll?sh", UriKind.RelativeOrAbsolute);
                string file = await App.RenRenService.GetLyricsCode(Encode(artist), Encode(title), "2", requestUrl);

                //分析返回的XML文件
                LyricsResult result = null;
                try
                {
                    using (MemoryStream stream = new MemoryStream())
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(file);
                        writer.Flush();
                        XmlSerializer serializer = new XmlSerializer(typeof(LyricsResult));
                        stream.Position = 0;
                        result = (LyricsResult)serializer.Deserialize(stream);
                    }
                }
                catch { }
                if (result == null || result.Count == 0) continue;

                //选出最合适的歌词文件
                LyricsItem selected = result[0];
                double dist = double.MaxValue;
                string lArtist = artist.ToLower();
                string lTitle = title.ToLower();
                foreach (var item in result)
                {
                    string iArtist = item.Artist.ToLower();
                    string iTitle = item.Title.ToLower();
                    if (lArtist == iArtist && lTitle == iTitle)
                    {
                        selected = item;
                        break;
                    }
                    else if (lArtist.Length < 100 && lTitle.Length < 100 && iArtist.Length < 100 && iTitle.Length < 100)
                    {
                        int dist1 = Distance(lArtist, iArtist);
                        int dist2 = Distance(lTitle, iTitle);
                        double temp = ((double)(dist1 + dist2)) / (lArtist.Length + lTitle.Length);
                        if (temp < dist)
                        {
                            dist = temp;
                            selected = item;
                        }
                    }
                }

                //下载歌词文件
                Uri requestUrl2 = new Uri("http://" + server + "/dll/lyricsvr.dll?dl", UriKind.RelativeOrAbsolute);
                string file2 = await App.RenRenService.GetLyricsData(selected.Id.ToString(), VerifyCode(selected.Artist, selected.Title, selected.Id), requestUrl2);

                //生成Lyrics的实例
                if (string.IsNullOrEmpty(file2)) continue;
                try
                {
                    return new Lyrics(file2);
                }
                catch
                {
                    continue;
                }
            }

            return null;
        }

		/// <summary>
		/// 对数据编码
		/// </summary>
		static string Encode(string data)
		{
			if (data == null) return "";
            var index = data.IndexOfAny(new char[] { '(', '[', '{', '（' });
            if (index != -1)
            {
                data = data.Substring(0, index);
            }
           
            //data = new string(data.Where(ch => char.IsLetterOrDigit(ch)).ToArray());
            var bytes = Encoding.Unicode.GetBytes(data);
            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
		}

		/// <summary>
		/// 生成校验码
		/// </summary>
		static string VerifyCode(string artist, string title, int lrcId)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(artist + title);
			int[] song = new int[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
				song[i] = bytes[i] & 0xff;
			int intVal1 = 0, intVal2 = 0, intVal3 = 0;
			intVal1 = (lrcId & 0xFF00) >> 8;
			if ((lrcId & 0xFF0000) == 0)
			{
				intVal3 = 0xFF & ~intVal1;
			}
			else
			{
				intVal3 = 0xFF & ((lrcId & 0x00FF0000) >> 16);
			}
			intVal3 = intVal3 | ((0xFF & lrcId) << 8);
			intVal3 = intVal3 << 8;
			intVal3 = intVal3 | (0xFF & intVal1);
			intVal3 = intVal3 << 8;
			if ((lrcId & 0xFF000000) == 0)
			{
				intVal3 = intVal3 | (0xFF & (~lrcId));
			}
			else
			{
				intVal3 = intVal3 | (0xFF & (lrcId >> 24));
			}
			int uBound = bytes.Length - 1;
			while (uBound >= 0)
			{
				int c = song[uBound];
				if (c >= 0x80)
					c = c - 0x100;
				intVal1 = c + intVal2;
				intVal2 = intVal2 << (uBound % 2 + 4);
				intVal2 = intVal1 + intVal2;
				uBound -= 1;
			}
			uBound = 0;
			intVal1 = 0;
			while (uBound <= bytes.Length - 1)
			{
				int c = song[uBound];
				if (c >= 128)
					c = c - 256;
				int intVal4 = c + intVal1;
				intVal1 = intVal1 << (uBound % 2 + 3);
				intVal1 = intVal1 + intVal4;
				uBound += 1;
			}
			int intVal5 = intVal2 ^ intVal3;
			intVal5 = intVal5 + (intVal1 | lrcId);
			intVal5 = intVal5 * (intVal1 | intVal3);
			intVal5 = intVal5 * (intVal2 ^ lrcId);
			return intVal5.ToString();
		}

		/// <summary>
		/// Levenshtein Distance算法，计算两个字符串之间的差异
		/// </summary>
		static int Distance(string a, string b)
		{
			if (string.IsNullOrEmpty(a)) return b.Length;
			if (string.IsNullOrEmpty(a)) return a.Length;
			int[][] d = new int[a.Length + 1][];
			for (int i = 0; i < d.Length; ++i)
				d[i] = new int[b.Length + 1];

			for (int i = 0; i <= a.Length; ++i)
				d[i][0] = i;
			for (int j = 0; j <= b.Length; ++j)
				d[0][j] = j;
			for (int i = 1; i <= a.Length; ++i)
				for (int j = 1; j <= b.Length; ++j)
				{
					d[i][j] = int.MaxValue;
					if (d[i - 1][j] + 1 < d[i][j]) d[i][j] = d[i - 1][j] + 1;
					if (d[i][j - 1] + 1 < d[i][j]) d[i][j] = d[i][j - 1] + 1;
					if (d[i - 1][j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1) < d[i][j])
						d[i][j] = d[i - 1][j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1);
				}
			return d[a.Length][b.Length];
		}
	}
}
