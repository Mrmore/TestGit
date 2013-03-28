using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.Model;
using RenRenWin8Radio.ViewModel;
using RenrenWin8RadioUI.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenrenWin8RadioUI.ViewModel.ForUI
{
    public class SearchVideoViewModel : PropertyChangedBase
    {
        /// <summary>
        /// 搜索关键词
        /// </summary>
        private string _keyword;

        /// <summary>
        /// 供绑定的数据
        /// </summary>
        private ObservableCollection<RadioItem> resultRadioDataList = null;
        public ObservableCollection<RadioItem> ResultRadioDataList
        {
            get
            {
                return resultRadioDataList;
            }
            set
            {
                resultRadioDataList = value;
                this.NotifyPropertyChanged(entity => entity.ResultRadioDataList);
            }
        }

        /// <summary>
        /// 获取所有电台列表
        /// </summary>
        public async Task GetAllRadioData(string keyword)
        {
            if (String.IsNullOrWhiteSpace(keyword))
            {
                return;
            }
            _keyword = keyword.Trim();

            //结果List
            if (ResultRadioDataList == null)
            {
                ResultRadioDataList = new ObservableCollection<RadioItem>();
            }
            else
            {
                ResultRadioDataList.Clear();
            }

            RenRenResponseArg<GetHomeEntity> getHomeEntity =
                await RadioHomeViewModel.Instance.RequestHomeByLoginMode(false);
            if (getHomeEntity.LocalError == null && getHomeEntity.RemoteError == null && getHomeEntity.Result != null)
            {
                InitRadioDataList(getHomeEntity.Result);
            }
        }

        private void InitRadioDataList(GetHomeEntity dataEntity)
        {
            if (dataEntity == null || dataEntity.Radios == null)
                return;

            //结果List
            if (ResultRadioDataList == null)
            {
                ResultRadioDataList = new ObservableCollection<RadioItem>();
            }
            else
            {
                ResultRadioDataList.Clear();
            }

            string folder = "/Resources/radios/radio";
            int imageIndex = 0;

            foreach (var item in dataEntity.Radios)
            {
                if (item != null)
                {
                    if(item.Name.Contains(_keyword))
                    {
                    RadioItem newItem = new RadioItem();
                    newItem.Name = item.Name;
                    newItem.Id = item.Id;
                    newItem.AlbumImg = folder + newItem.Id.ToString() + ".png";
                    ResultRadioDataList.Add(newItem);
                    }

                    imageIndex++;
                }
                                        
            }

        }

    }
}
