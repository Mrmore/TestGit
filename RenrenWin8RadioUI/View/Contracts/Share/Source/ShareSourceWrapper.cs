using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace RenrenWin8RadioUI.View.Contracts.Share
{
    // Summary:
    //     Represents the overall share source contract feature implementation
    //     following is one example, you can copy & paste the code
    class share_source_service_example_code
    {
        // Declare the share source service
        private ShareSourceWrapper _shareSource = null;

        // Register the share source intance
        public void RegisterShareSource()
        {
            if (_shareSource == null)
                _shareSource = new ShareSourceWrapper(SourceType.Image, this.SourceEntityRequest);
        }
        // Unregisger the overall current share source service
        public void UnRegisterShareSource()
        {
            _shareSource.Reset();
            _shareSource = null;
        }

        // delegate method to load the share source entity to be shared
        void SourceEntityRequest(SourceEntity entity)
        {
            {
                entity.Title = "from renren.com";
                entity.Description = "Desception";
                var streamRef = RandomAccessStreamReference.CreateFromUri(new Uri("http://xx.jpg"));
                entity.ImageStreamRef = streamRef;
                entity.Thumbnail = streamRef;
                entity.Uri = new Uri("http://xx.jpg");
            }
        }
    }

    public delegate void SourceEntityRequest(SourceEntity entity);
    public class ShareSourceWrapper
    {
        private IShareSourceService _target = null;
        private ISourceItem _item = null;
        private static ItemsTypeLoader _loader = null;

        public ShareSourceWrapper(SourceType type, SourceEntityRequest request)
        {
            lock (typeof(ItemsTypeLoader))
            {
                if (_loader == null)
                {
                    _loader = new ItemsTypeLoader();
                }
            }

            _item = (ISourceItem)Activator.CreateInstance(_loader.Types[type]);
            if (request != null) _item.SetRequest(request);
            _target = new ShareSourceServiceImpl(_item);
        }

        public void AddSource(SourceEntity entity)
        {
            _target.AddSource(entity);
        }

        public void Reset()
        {
            _target.Reset();
        }


        internal class ItemsTypeLoader
        {
            public IDictionary<SourceType, Type> Types { get; set; }
            public ItemsTypeLoader()
            {
                Types = new Dictionary<SourceType, Type>();
                // TODO: bellow codes should be refined using reflection type collection
                // or we use generic method to reflect the item type e.g. ShareSourceWrapper<ItemType>
                Types[SourceType.CustomData] = typeof(CustomDataSourceItem);
                Types[SourceType.CustomError] = typeof(CustomErrorSourceItem);
                Types[SourceType.HTML] = typeof(HTMLSourceItem);
                Types[SourceType.Image] = typeof(ImageSourceItem);
                Types[SourceType.ImageDelayedRendering] = typeof(ImageDelayedRenderingSourceItem);
                Types[SourceType.StorageItems] = typeof(StorageItemsSourceItem);
                Types[SourceType.Text] = typeof(TextSoureItem);
                Types[SourceType.Uri] = typeof(UriSourceItem);
            }
        }
    }

    public interface IShareSourceService
    {
        void AddSource(SourceEntity entity);
        void Reset();
    }

    public class ShareSourceServiceImpl : IShareSourceService
    {
        private DataTransferManager _datatransferManager = null;
        private ISourceItem _item = null;

        public ShareSourceServiceImpl(ISourceItem item)
        {
            _item = item;
            _datatransferManager = DataTransferManager.GetForCurrentView();
            if (_datatransferManager != null)
            {
                _datatransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
                _datatransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
            }
        }

        public void AddSource(SourceEntity entity)
        {
            _item.SetEntity(entity);
        }

        public void Reset()
        {
            _datatransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.DataRequested);
        }

        async void DataRequested(DataTransferManager sender, DataRequestedEventArgs e)
        {
            _item.DataRequest(sender, e);
        }
    }

    public abstract class ISourceItem
    {
        public abstract SourceType Type();
        public abstract void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e);

        public void DataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            if (DataRequestHanlder != null)
            {
                _entity = new SourceEntity();
                this.DataRequestHanlder(_entity);
            }
            
            if (_entity == null)
            {
                e.Request.FailWithDisplayText("There is no source to be shared!");
                return;
            }

            // Error message has high priority
            if (_entity.CustomErrorMessage != null)
            {
                e.Request.Data.Properties.Title = this._entity.Title;
                e.Request.Data.Properties.Description = this._entity.Description;
                if (this._entity.Thumbnail != null)
                {
                    e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
                }

                e.Request.FailWithDisplayText(this._entity.CustomErrorMessage);
                return;
            }

            DoDataRequest(sender, e);
        }

        protected event SourceEntityRequest DataRequestHanlder;
        public void SetRequest(SourceEntityRequest request)
        {
            DataRequestHanlder += request;
        }

        protected SourceEntity _entity = null;
        public void SetEntity(SourceEntity entity)
        {
            _entity = entity;
        }
    }

    public class SourceEntity
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public RandomAccessStreamReference Thumbnail { get; set; }
        public string CustomErrorMessage { get; set; }
        public string Text { get; set; }
        public Uri Uri { get; set; }
        public StorageFile ImageFile { get; set; }
        public RandomAccessStreamReference ImageStreamRef { get; set; }
        public IRandomAccessStream ImageStream { get; set; }
        public IReadOnlyList<StorageFile> StorageItems { get; set; }
        public string HtmlContent { get; set; }
        public object CustomData { get; set; }
        public string CustomDataFromat { get; set; }
    }

    public class TextSoureItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.Text;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetText(this._entity.Text);
        }
    }

    public class UriSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.Uri;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetUri(this._entity.Uri);
        }
    }

    public class ImageSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.Image;
        }

        public async override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            if (_entity.ImageStreamRef == null)
            {
                e.Request.FailWithDisplayText("Something wrong within share process");
                return;
            }

            if (!string.IsNullOrEmpty(_entity.Title))
                e.Request.Data.Properties.Title = this._entity.Title;

            if (!string.IsNullOrEmpty(_entity.Description))
                e.Request.Data.Properties.Description = this._entity.Description;

            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetBitmap(this._entity.ImageStreamRef);

            //StorageFile file = await StorageFile.CreateStreamedFileFromUriAsync("temp.jpg", _entity.Uri, null);
            //var list = new List<StorageFile>();
            //list.Add(file);
            //_entity.StorageItems = list;
            //e.Request.Data.SetStorageItems(this._entity.StorageItems);
        }
    }

    public class StorageItemsSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.StorageItems;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetStorageItems(this._entity.StorageItems);
        }
    }

    public class ImageDelayedRenderingSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.ImageDelayedRendering;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetDataProvider(StandardDataFormats.Bitmap, OnDeferredImageRequestedHandler);
        }

        async void OnDeferredImageRequestedHandler(DataProviderRequest request)
        {
            // Here we provide updated Bitmap data using delayed rendering
            if (this._entity.ImageStream != null)
            {
                DataProviderDeferral deferral = request.GetDeferral();
                InMemoryRandomAccessStream inMemoryStream = new InMemoryRandomAccessStream();

                // Decode the image
                BitmapDecoder imageDecoder = await BitmapDecoder.CreateAsync(this._entity.ImageStream);

                // Re-encode the image at 50% width and height
                BitmapEncoder imageEncoder = await BitmapEncoder.CreateForTranscodingAsync(inMemoryStream, imageDecoder);
                imageEncoder.BitmapTransform.ScaledWidth = (uint)(imageDecoder.OrientedPixelHeight * 0.5);
                imageEncoder.BitmapTransform.ScaledHeight = (uint)(imageDecoder.OrientedPixelHeight * 0.5);
                await imageEncoder.FlushAsync();

                request.SetData(RandomAccessStreamReference.CreateFromStream(inMemoryStream));
                deferral.Complete();
            }
        }
    }

    public class HTMLSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.HTML;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetData(StandardDataFormats.Html, this._entity.HtmlContent);
        }
    }

    public class CustomDataSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.CustomData;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.Data.SetData(this._entity.CustomDataFromat, this._entity.CustomData);
        }
    }

    public class CustomErrorSourceItem : ISourceItem
    {
        public override SourceType Type()
        {
            return SourceType.CustomError;
        }

        public override void DoDataRequest(DataTransferManager sender, DataRequestedEventArgs e)
        {
            e.Request.Data.Properties.Title = this._entity.Title;
            e.Request.Data.Properties.Description = this._entity.Description;
            if (this._entity.Thumbnail != null)
            {
                e.Request.Data.Properties.Thumbnail = this._entity.Thumbnail;
            }

            e.Request.FailWithDisplayText(this._entity.CustomErrorMessage);
        }
    }

    public enum SourceType
    {
        Text,
        Uri,
        Image,
        StorageItems,
        ImageDelayedRendering,
        HTML,
        CustomData,
        CustomError
    }
}
