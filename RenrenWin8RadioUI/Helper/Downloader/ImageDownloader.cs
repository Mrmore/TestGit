using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace DataLayerWrapper.Downloader
{
    public class ImageDownloaderImpl : IDownloader<BitmapImage>
    {
        public async Task<BitmapImage> Download(string url, string fileName, StorageFolder folder)
        {
            BitmapImage bitmapImage = null;
            try
            {
                IDownloader<StorageFile> impl = new StorageFileDownloader();

                StorageFile file = await impl.Download(url, fileName, folder);

                using (IRandomAccessStreamWithContentType stream = await file.OpenReadAsync())
                {
                    bitmapImage = new BitmapImage();
                    bitmapImage.SetSource(stream);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("##Download image failed: " + url.ToString() + "_");
                Debug.WriteLine(ex.Message);
            }

            return bitmapImage;
        }

        public async Task<BitmapImage> Download(string url, string fileName, string folderName)
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            var folder = await localFolder.CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);

            return await Download(url, fileName, folder);
        }
    }
}
