using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenrenWin8Radio.Helper.LiveTile
{
    public class LiveTileManager
    {
        public static ILiveTile CreateNewLiveTile(TileTyle type)
        {
            ILiveTile instance = null;
            switch (type)
            {
                case TileTyle.AppTile:
                    instance = new AppLiveTile();
                    break;
                case TileTyle.SecondaryTile:
                    instance = new SecondaryLiveTile();
                    break;
                default:
                    break;
            }
            return instance;
        }

        public static ILiveTile GetLiveTile(TileTyle type, string tileId = null)
        {
            if (type == TileTyle.AppTile)
                return new AppLiveTile();
            else
            {
                return SecondaryLiveTile.GetExistOne(tileId);
            }
        }
    }
}
