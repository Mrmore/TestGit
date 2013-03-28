using RenRenAPI.Entity;
using RenRenWin8Radio.Model;
using RenRenWin8Radio.Util;
using RenRenWin8Radio.ViewModel;
using RenrenWin8RadioUI.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenrenWin8RadioUI.ViewModel
{
    public class RadioListViewModel : PropertyChangedBase
    {
        int _itemInLineNum = 6;
        const int _itemInLineNumSnap = 2;
        List<uint> radioIdList = null;

        public RadioListViewModel()
        {
            radioIdList = new List<uint>();
            radioIdList.Add(0);
            radioIdList.Add(363);
            radioIdList.Add(300);
            radioIdList.Add(349);
            radioIdList.Add(142);
            radioIdList.Add(346);
            radioIdList.Add(267);
            radioIdList.Add(266);
            radioIdList.Add(361);
            radioIdList.Add(365);
            radioIdList.Add(341);
            radioIdList.Add(265);
            radioIdList.Add(259);
            radioIdList.Add(350);
            radioIdList.Add(108);
            radioIdList.Add(288);
            radioIdList.Add(354);
            radioIdList.Add(337);
            radioIdList.Add(260);
            radioIdList.Add(261);
            radioIdList.Add(263);
            radioIdList.Add(264);
            radioIdList.Add(345);
            radioIdList.Add(353);
            radioIdList.Add(366);
            radioIdList.Add(367);
            radioIdList.Add(368);
            radioIdList.Add(369);
            radioIdList.Add(371);
            radioIdList.Add(372);
            radioIdList.Add(373);
            radioIdList.Add(377);
            radioIdList.Add(378);
            radioIdList.Add(379);
            radioIdList.Add(380);
            radioIdList.Add(381);
            radioIdList.Add(382);
            radioIdList.Add(383);
            radioIdList.Add(384);
            radioIdList.Add(385);
            radioIdList.Add(386);
            radioIdList.Add(393);
            radioIdList.Add(389);
            radioIdList.Add(511);
        }


        private ObservableCollection<RadioItem> radioDataList = null;
        public ObservableCollection<RadioItem> RadioDataList
        {
            get
            {
                return radioDataList;
            }
            set
            {
                radioDataList = value;
                this.NotifyPropertyChanged(entity => entity.RadioDataList);
            }
        }

        /// <summary>
        /// 书架做的数据
        /// </summary>
        private ObservableCollection<RadioItemForUI> radioDataListForUI = null;
        public ObservableCollection<RadioItemForUI> RadioDataListForUI
        {
            get
            {
                return radioDataListForUI;
            }
            set
            {
                radioDataListForUI = value;
                this.NotifyPropertyChanged(entity => entity.RadioDataListForUI);
            }
        }

        /// <summary>
        /// 为书架做的数据
        /// </summary>
        private ObservableCollection<RadioItemForUI> radioDataListForUISnap = null;
        public ObservableCollection<RadioItemForUI> RadioDataListForUISnap
        {
            get
            {
                return radioDataListForUISnap;
            }
            set
            {
                radioDataListForUISnap = value;
                this.NotifyPropertyChanged(entity => entity.RadioDataListForUISnap);
            }
        }


        public RadioItem FindRadio(int radioId)
        {
            foreach (var item in RadioDataList)
            {
                if (item.Id == radioId)
                {
                    return item;
                }
            }
            return null;
        }

        public void InitRadioDataList(GetHomeEntity dataEntity)
        {
            if (dataEntity == null || dataEntity.Radios == null || dataEntity.Radios.Count <= 0)
            {
                NotificationHelper.DisplayTextTost("网络出现问题啦", "试试刷新看看。。。");
                return;
            }

            if (RadioDataList == null)
            {
                RadioDataList = new ObservableCollection<RadioItem>();
            }
            else
            {
                RadioDataList.Clear();
            }

            if (RadioDataListForUI == null)
            {
                RadioDataListForUI = new ObservableCollection<RadioItemForUI>();
            }
            else
            {
                RadioDataListForUI.Clear();
            }

            if (RadioDataListForUISnap == null)
            {
                RadioDataListForUISnap = new ObservableCollection<RadioItemForUI>();
            }
            else
            {
                RadioDataListForUISnap.Clear();
            }

            if (dataEntity.Radios.Count > 24)
            {
                _itemInLineNum = dataEntity.Radios.Count / 4;
                if (dataEntity.Radios.Count % 4 > 0)
                {
                    _itemInLineNum += 1;
                }
            }

            string folderRadio = "/Resources/radios/radio";
            string folderCD = "/Resources/cd/cd";
            int imageIndex = 0;
            if (!LoginViewModel.Instance.HasLogin)
            {
                imageIndex = 1;
            }

            foreach (var item in dataEntity.Radios)
            {
                if (item != null)
                {
                    RadioItem newItem = new RadioItem();
                    newItem.Name = item.Name;
                    newItem.Id = item.Id;
                    if (radioIdList.Contains(item.Id))
                    {
                        newItem.AlbumImg = folderRadio + newItem.Id.ToString() + ".png";
                        newItem.AlbumCD = folderCD + newItem.Id.ToString() + ".png";
                    }
                    else
                    {
                        newItem.AlbumImg = "/Resources/radios/radio-defalut.png";
                        newItem.AlbumCD = "/Resources/cd/cd-default.png";
                    }

                    if (RadioDataList.Count % _itemInLineNum == _itemInLineNum || 
                        RadioDataList.Count % _itemInLineNum == 0)
                    {
                        RadioItemForUI uiItem = new RadioItemForUI();
                        RadioDataListForUI.Add(uiItem);
                        uiItem.Item.Add(newItem);
                    }
                    else
                    {
                        int index = RadioDataListForUI.Count  - 1;
                        RadioDataListForUI[index].Item.Add(newItem);
                    }

                    if (RadioDataList.Count % _itemInLineNumSnap == _itemInLineNumSnap ||
                        RadioDataList.Count % _itemInLineNumSnap == 0)
                    {
                        RadioItemForUI uiItem = new RadioItemForUI();
                        RadioDataListForUISnap.Add(uiItem);
                        uiItem.Item.Add(newItem);
                    }
                    else
                    {
                        int index = RadioDataListForUISnap.Count - 1;
                        RadioDataListForUISnap[index].Item.Add(newItem);
                    }
                    imageIndex++;
                    RadioDataList.Add(newItem);
                }
            }
        }
    }
}
