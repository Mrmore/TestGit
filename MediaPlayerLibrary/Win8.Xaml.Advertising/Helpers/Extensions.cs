﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
#if SILVERLIGHT
using System.Windows;
#else

#endif

namespace Microsoft.PlayerFramework.Advertising
{
    internal static class Extensions
    {
        public static async Task<Stream> GetStreamAsync(this WebRequest request)
        {
            var response = await Task.Factory.FromAsync<WebResponse>(request.BeginGetResponse, request.EndGetResponse, null);
            return response.GetResponseStream();
        }

        public static async Task<Stream> LoadStreamAsync(Uri source)
        {
            switch (source.Scheme.ToLowerInvariant())
            {
                case "ms-appx":
                case "ms-appdata":
                    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(source);
                    return await file.OpenStreamForReadAsync();
                default:
                    return await DownloadStreamAsync(source);
            }
        }

        public static async Task<Stream> DownloadStreamAsync(Uri source)
        {
#if SILVERLIGHT
            var client = new HttpClient();
            return await client.GetStreamAsync(source);
#else
            var client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, source);
            if (Microsoft.VideoAdvertising.Extensions.DefaultUserAgent != null)
            {
                request.Headers.UserAgent.ParseAdd(Microsoft.VideoAdvertising.Extensions.DefaultUserAgent);
            }
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStreamAsync();
#endif
        }

        public static void AddRange<T>(this IList<T> source, IEnumerable<T> collection)
        {
            foreach (var item in collection)
            {
                source.Add(item);
            }
        }

        public static IEnumerable<string> GetLines(this string source)
        {
            using (var stringReader = new StringReader(source))
            {
                string line;
                do
                {
                    line = stringReader.ReadLine();
                    if (line == null) break;
                    yield return line;
                } while (true);
            }
        }

        public static int? ToInt(this string source)
        {
            int result;
            if (int.TryParse(source, out result))
                return result;
            else
                return null;
        }

        public static long? ToInt64(this string source)
        {
            long result;
            if (long.TryParse(source, out result))
                return result;
            else
                return null;
        }

        public static double? ToDouble(this string source)
        {
            double result;
            if (double.TryParse(source, out result))
                return result;
            else
                return null;
        }

        public static short? ToSingle(this string source)
        {
            short result;
            if (short.TryParse(source, out result))
                return result;
            else
                return null;
        }
    }
}
