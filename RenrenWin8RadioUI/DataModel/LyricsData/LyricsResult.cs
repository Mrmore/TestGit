using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace RenrenWin8RadioUI.DataModel.LyricsData
{
    /// <summary>
    /// 表示歌词搜索结果
    /// </summary>
    [XmlRootAttribute("result")]
    public class LyricsResult : List<LyricsItem>
    {
    }
}
