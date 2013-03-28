using DataLayerWrapper.Downloader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using RenRenAPI.Helper;
using Windows.UI.Notifications;

namespace RenrenWin8Radio.Helper.LiveTile
{
    public class SecondaryLiveTile : ILiveTile
    {
        private PinEntity _pinEntity = null;

        public static ILiveTile GetExistOne(string id)
        {
            if (string.IsNullOrEmpty(id) || !SecondaryTile.Exists(id)) return null;
            else
            {
                var tile = new SecondaryTile(id);
                return new SecondaryLiveTile(
                    new PinEntity() { 
                        BackgroundImage = tile.Logo.AbsoluteUri, DisplayName = tile.DisplayName, Id = tile.TileId, ShortName = tile.ShortName });
            }
        }

        public SecondaryLiveTile() { }
        public SecondaryLiveTile(PinEntity entity)
        {
            this._pinEntity = entity;
        }

        public async Task<bool> PinToStartAsync(PinEntity entity)
        {
            this._pinEntity = entity;

            bool result = false;
            if (SecondaryTile.Exists(_pinEntity.Id)) return true;

            try
            {
                bool fetchHead = false;
                Uri logo = null;
                Uri smallLogo = null;

                try
                {
                    string extend = Path.HasExtension(this._pinEntity.BackgroundImage) ? Path.GetExtension(this._pinEntity.BackgroundImage) : ".jpg";
                    string fileName = ApiHelper.ComputeMD5(this._pinEntity.BackgroundImage) + extend;
                    var file = await new StreamDownloader().Download(this._pinEntity.BackgroundImage, fileName, "Pin2Start");

                    logo = new Uri(string.Format("ms-appdata:///local/Pin2Start/{0}", file.Name));
                    smallLogo = new Uri("ms-appx:///Assets/SmallLogo.png");
                    fetchHead = true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Pin2Start fetch head failed!");
                    Debug.WriteLine(ex.Message);
                    fetchHead = false;
                }

                if (false == fetchHead)
                {
                    logo = new Uri("ms-appx:///Assets/DefaultLiveTile.png");
                }

                smallLogo = new Uri("ms-appx:///Assets/Logo.png");

                // During creation of secondary tile, an application may set additional arguments on the tile that will be passed in during activation. 
                // These arguments should be meaningful to the application. In this sample, we'll pass in the date and time the secondary tile was pinned.
                string tileActivationArguments = _pinEntity.Id;

                // Create a wide tile
                SecondaryTile secondaryTile = new SecondaryTile(_pinEntity.Id,
                                                                this._pinEntity.ShortName,
                                                                this._pinEntity.DisplayName,
                                                                tileActivationArguments,
                                                                TileOptions.ShowNameOnLogo | TileOptions.ShowNameOnWideLogo | TileOptions.CopyOnDeployment,
                                                                logo, logo);

                // Create a 1*1 small tile
                //SecondaryTile secondaryTile = new SecondaryTile(_pinEntity.Id,
                //                                                this._pinEntity.ShortName,
                //                                                this._pinEntity.DisplayName,
                //                                                tileActivationArguments,
                //                                                TileOptions.ShowNameOnLogo | TileOptions.ShowNameOnWideLogo | TileOptions.CopyOnDeployment,
                //                                                logo);

                // Specify a foreground text value. 
                // The tile background color is inherited from the parent unless a separate value is specified.
                //secondaryTile.ForegroundText = ForegroundText.Light;
                //secondaryTile.BackgroundColor = Color.FromArgb(0xFF, 0x00, 0xB9, 0xEF);// "#00B9EF";

                // Like the background color, the small tile logo is inherited from the parent application tile by default. Let's override it, just to see how that's done.
                //secondaryTile.SmallLogo = smallLogo;
                // OK, the tile is created and we can now attempt to pin the tile. 
                // Note that the status message is updated when the async operation to pin the tile completes. 
                result = await secondaryTile.RequestCreateAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }

        public async Task<bool> UnPinAsync()
        {
            bool result = false;
            if (_pinEntity != null && SecondaryTile.Exists(_pinEntity.Id))
            {
                // First prepare the tile to be unpinned
                SecondaryTile secondaryTile = new SecondaryTile(_pinEntity.Id);
                // Now make the delete request.
                result = await secondaryTile.RequestDeleteAsync();
            }

            return result;
        }

        public void ClearTileContent()
        {
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(_pinEntity.Id).Clear();
        }

        public void ClearBadgeContent()
        {
            BadgeUpdateManager.CreateBadgeUpdaterForSecondaryTile(_pinEntity.Id).Clear();
        }

        public void ClearOveralTileContent()
        {
            this.ClearBadgeContent();
            this.ClearTileContent();
        }

        public void StartPullingContent(Windows.UI.Notifications.PeriodicUpdateRecurrence period, string tileUrl, string badgeUrl)
        {
            throw new NotImplementedException();
        }

        public void StopPullingContent()
        {
            throw new NotImplementedException();
        }


        public string Id
        {
            get { throw new NotImplementedException(); }
        }

        public string ShortName
        {
            get { throw new NotImplementedException(); }
        }

        public string DisplayName
        {
            get { throw new NotImplementedException(); }
        }
    }
}
