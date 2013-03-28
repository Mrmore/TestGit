using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace RenrenWin8Radio.Helper.LiveTile
{
    public enum TileTyle
    {
        AppTile,
        SecondaryTile
    }

    public class PinEntity
    {
        public string Id { get; set; }
        public string BackgroundImage { get; set; }
        public string ShortName { get; set; }
        public string DisplayName { get; set; }
    }

    public interface ILiveTile
    {
        Task<bool> PinToStartAsync(PinEntity entity);
        Task<bool> UnPinAsync();
        void ClearTileContent();
        void ClearBadgeContent();
        void ClearOveralTileContent();
        void StartPullingContent(PeriodicUpdateRecurrence period, string tileUrl, string badgeUrl);
        void StopPullingContent();

        string Id { get; }
        string ShortName { get; }
        string DisplayName { get; }
    }
}
