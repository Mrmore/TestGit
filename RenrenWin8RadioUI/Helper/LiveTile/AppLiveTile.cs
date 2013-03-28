using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace RenrenWin8Radio.Helper.LiveTile
{
    public class AppLiveTile : ILiveTile
    {
        public Task<bool> PinToStartAsync(PinEntity entity)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnPinAsync()
        {
            throw new NotImplementedException();
        }

        public void ClearTileContent()
        {
            TileUpdateManager.CreateTileUpdaterForApplication().Clear();
        }

        public void ClearBadgeContent()
        {
            BadgeUpdateManager.CreateBadgeUpdaterForApplication().Clear();
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
            TileUpdateManager.CreateTileUpdaterForApplication().StopPeriodicUpdate();
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
