using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.PlayerFramework.TimedText
{
    internal static class Extensions
    {
        public static async Task<Stream> LoadToStream(this Uri source)
        {
#if SILVERLIGHT
            var client = new HttpClient();
            return await client.GetStreamAsync(source);
#else
            switch (source.Scheme.ToLowerInvariant())
            {
                case "ms-appx":
                case "ms-appdata":
                    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(source);
                    return await file.OpenStreamForReadAsync();
                default:
                    var client = new HttpClient();
                    return await client.GetStreamAsync(source);
            }
#endif
        }

        public static async Task<string> LoadToString(this Uri source)
        {
            using (var stream = await source.LoadToStream())
            {
                return new StreamReader(stream).ReadToEnd();
            }
        }
    }
}
