using DataLayerWrapper.Downloader;
using RenRenAPI.Helper;
using RenRenWin8Radio.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace RenrenWin8RadioUI.Helper.Notifications
{
    public enum TileTyle
    {
        AppTile,
        SecondaryTile
    }

    public class Pin2Entity
    {
        public string Head_url { get; set; }
        public string Large_Header { get; set; }
        public string User_name { get; set; }
        public int User_id { get; set; }

        public string BuildScndryTlId()
        {
            return NotificatoinWrapper.BuildScndryTlId(User_id);
        }
    }

    public interface IPin2Start
    {
        void Init(object source);
        Task<bool> Pin();
        Task<bool> UnPin();
        void ClearTile();
    }

    public class AppTilePin2Impl : IPin2Start
    {
        public async Task<bool> Pin()
        {
            return true;
        }

        public async Task<bool> UnPin()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
            return true;
        }

        public void ClearTile()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }

        public void Init(object source)
        {
            throw new NotImplementedException();
        }
    }

    public class SecondaryTilePin2Impl : IPin2Start
    {
        private Pin2Entity _entity = null;
        private string _tileId = string.Empty;
        public async Task<bool> Pin()
        {
            bool result = false;
            if (NotificatoinWrapper.CheckIfExist(_tileId)) return true;
            if (string.IsNullOrEmpty(_entity.User_name))
            { throw new ArgumentException("No enough parameter to pin 2 start"); }

            try
            {
                bool fetchHead = false;
                Uri logo = null;
                Uri smallLogo = null;

                if (!string.IsNullOrEmpty(_entity.Head_url) && !string.IsNullOrEmpty(_entity.Large_Header))
                {
                    try
                    {

                        logo = new Uri("ms-appx://"+_entity.Head_url);
                        smallLogo = new Uri("ms-appx:///Assets/SmallLogo.png");
                        fetchHead = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Pin2Start fetch head failed!");
                        Debug.WriteLine(ex.Message);
                        fetchHead = false;
                    }
                }

                if (false == fetchHead)
                {
                    logo = new Uri("ms-appx:///Assets/blue_squ.png");
                }
                smallLogo = new Uri("ms-appx:///Assets/Logo.png");
                // During creation of secondary tile, an application may set additional arguments on the tile that will be passed in during activation. 
                // These arguments should be meaningful to the application. In this sample, we'll pass in the date and time the secondary tile was pinned.
                string tileActivationArguments = _entity.User_id.ToString();

                // Create a 1x1 Secondary tile
                SecondaryTile secondaryTile = new SecondaryTile(_tileId,
                                                                _entity.User_name,
                                                                _entity.User_name,
                                                                tileActivationArguments,
                                                                TileOptions.ShowNameOnLogo | TileOptions.ShowNameOnWideLogo | TileOptions.CopyOnDeployment,
                                                                logo, logo);

                result = await secondaryTile.RequestCreateAsync();

                Debug.WriteLine(string.Format("create {0}", result));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

            return result;
        }

        public async Task<bool> UnPin()
        {
            bool result = false;
            if (NotificatoinWrapper.CheckIfExist(_tileId))
            {
                // First prepare the tile to be unpinned
                SecondaryTile secondaryTile = new SecondaryTile(_tileId);
                // Now make the delete request.
                result = await secondaryTile.RequestDeleteAsync();
            }
            return result;
        }

        public void Init(object source)
        {
            if (source is Pin2Entity)
            {
                _entity = source as Pin2Entity;
                _tileId = _entity.BuildScndryTlId();
            }
            else if (source is int)
            {
                _entity = new Pin2Entity() { User_id = (int)source };
                _tileId = _entity.BuildScndryTlId();
            }
            else
            {
                throw new ArgumentException("entity is not a Pin2Entity type!");
            }
        }

        public void ClearTile()
        {
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(_tileId).Clear();
        }
    }
}
