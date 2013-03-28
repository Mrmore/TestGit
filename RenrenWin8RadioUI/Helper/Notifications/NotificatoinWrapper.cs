using RenRenWin8Radio.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.PushNotifications;
using Windows.UI.StartScreen;

namespace RenrenWin8RadioUI.Helper.Notifications
{
    public class NotificatoinWrapper
    {

        public static IPin2Start CreatePinByEntity(TileTyle type, Pin2Entity entity)
        {
            IPin2Start instance = null;
            switch (type)
            {
                case TileTyle.AppTile:
                    instance = new AppTilePin2Impl();
                    break;
                case TileTyle.SecondaryTile:
                    instance = new SecondaryTilePin2Impl();
                    instance.Init(entity);
                    break;
                default:
                    break;
            }
            return instance;
        }

        public static IPin2Start CreatePinByUserId(TileTyle type, int uid)
        {
            IPin2Start instance = null;
            switch (type)
            {
                case TileTyle.AppTile:
                    instance = new AppTilePin2Impl();
                    break;
                case TileTyle.SecondaryTile:
                    instance = new SecondaryTilePin2Impl();
                    instance.Init(uid);
                    break;
                default:
                    break;
            }

            return instance;
        }

        public static string BuildScndryTlId(int uid)
        {
            return string.Format("SecondaryTile.{0}", uid);
        }

        public static int GetUserId(string tileId)
        {
            return Convert.ToInt32(tileId.Split('.').Last());
        }

        public async static Task<string> GetUserNameFromTile(int uid)
        {
            return await GetUserNameFromTile(BuildScndryTlId(uid));
        }

        public async static Task<string> GetUserNameFromTile(string tileId)
        {
            IReadOnlyList<SecondaryTile> tiles = await SecondaryTile.FindAllAsync();
            string userName = string.Empty;

            if (tiles.Count > 0)
            {
                foreach (var item in tiles)
                {
                    if (item.TileId == tileId)
                    {
                        userName = item.ShortName;
                        break;
                    }
                }
            }
            return userName;
        }

        public static bool CheckIfExist(int uid)
        {
            return SecondaryTile.Exists(BuildScndryTlId(uid));
        }

        public static bool CheckIfExist(string tileId)
        {
            return SecondaryTile.Exists(tileId);
        }

        public async static Task RemoveAllSecondarys()
        {
            try
            {
                IReadOnlyList<SecondaryTile> tiles = await SecondaryTile.FindAllAsync();

                if (tiles.Count > 0)
                {
                    foreach (var item in tiles)
                    {
                        await item.RequestDeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }

        }
    }

}
