using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
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
using Windows.UI.Xaml.Controls;

namespace RenrenWin8RadioUI.ViewModel
{
    public class MainPageViewModel
    {
        #region Singleton
        /// <summary>
        /// Singleton property
        /// </summary>
        public static MainPageViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly MainPageViewModel _instance = new MainPageViewModel();
        private MainPageViewModel()
        {
            RadioListViewModel = new RadioListViewModel();
        }
        #endregion

        #region property
        public RadioListViewModel RadioListViewModel = null;
        public RadioItem CurrentRadioData = null;
        public GetHomeEntity HomeData = null;
        #endregion

        private DateTime _startTime = DateTime.Now;

        /// <summary>
        /// 开始获取网络数据
        /// </summary>
        public async Task GetRadioData()
        {
            await AutoLogin();

            RenRenResponseArg<GetHomeEntity> getHomeEntity = 
                await RadioHomeViewModel.Instance.RequestHomeByLoginMode(LoginViewModel.Instance.HasLogin);
            if (getHomeEntity.LocalError == null && getHomeEntity.RemoteError == null)
            {
                HomeData = getHomeEntity.Result;
                RadioListViewModel.InitRadioDataList(getHomeEntity.Result);
            }
        }

        async Task AutoLogin()
        {
            ObservableCollection<UserInfo> UserList = UserInfoList.Instance.UserList();
            if (UserList != null && UserList.Count > 0)
            {
                RenRenResponseArg<UserEntity> resp = await LoginViewModel.Instance.Login(
                    UserList[0].UserName, UserList[0].PassWord);
                if (resp.LocalError == null && resp.RemoteError == null)
                {
                    LoginViewModel.Instance.HasLogin = true;
                }
                else
                {
                    LoginViewModel.Instance.HasLogin = false;
                }
            }
            else
            {
                LoginViewModel.Instance.HasLogin = false;
            }
        }

        /// <summary>
        /// 获取全部歌曲列表
        /// </summary>
        public async Task GetAllSongList()
        {
            if (RadioListViewModel.RadioDataList == null || RadioListViewModel.RadioDataList.Count <= 0)
            {
                return;
            }

            foreach (var item in RadioListViewModel.RadioDataList)
            {
                await GetSongList(item);
            }
        }

        /// <summary>
        /// 设置当前播放电台，重复设置返回false
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool SetCurrentRadio(RadioItem data)
        {
            if (data == null || CurrentRadioData == data)
            {
                return false;
            }
            CurrentRadioData = data;
            return true;
        }

        /// <summary>
        /// 更新所有列表
        /// </summary>
        async public void RefreshAll()
        {
            bool hasPaly = false;
            uint lastRadioId = 0;
            if (CurrentRadioData != null)
            {
                hasPaly = true;
                lastRadioId = CurrentRadioData.Id;
            }
            CurrentRadioData = null;

            RenRenResponseArg<GetHomeEntity> getHomeEntity =
                await RadioHomeViewModel.Instance.RequestHomeByLoginMode(LoginViewModel.Instance.HasLogin);
            if (getHomeEntity.LocalError == null && getHomeEntity.RemoteError == null)
            {
                HomeData = getHomeEntity.Result;
                RadioListViewModel.InitRadioDataList(getHomeEntity.Result);
            }
            else
            {
                return;
            }

            int i = 0;
            for (i = 0; i < RadioListViewModel.RadioDataList.Count; i++)
            {
                var item = RadioListViewModel.RadioDataList[i];
                if (item != null)
                {
                    RenRenResponseArg<SongListEntity> ret = await RadioViewModel.Instance.RequestSongListByRadioId(item.Id,
                    LoginViewModel.Instance.HasLogin);
                    if (ret.LocalError == null && ret.RemoteError == null)
                    {
                        if (hasPaly && item.Id == lastRadioId)
                        {
                            CurrentRadioData = item;
                        }
                        item.Songs = ret.Result.Songs;
                    }
                }
            }

            //foreach (var item in RadioListViewModel.RadioDataList)
            //{
            //    RenRenResponseArg<SongListEntity> ret = await RadioViewModel.Instance.RequestSongListByRadioId(item.Id,
            //        LoginViewModel.Instance.HasLogin);
            //    if (ret.LocalError == null && ret.RemoteError == null)
            //    {
            //        if (hasPaly && item.Id == lastRadioId)
            //        {
            //            CurrentRadioData = item;
            //        }
            //        item.Songs = ret.Result.Songs;
            //    }
            //}
        }

        /// <summary>
        /// 更新当前列表
        /// </summary>
        async public Task RefreshCurrentRadio()
        {
            RenRenResponseArg<SongListEntity> ret = await RadioViewModel.Instance.RequestSongListByRadioId(CurrentRadioData.Id,
                LoginViewModel.Instance.HasLogin);
            if (ret.LocalError == null && ret.RemoteError == null)
            {
                CurrentRadioData.Songs = ret.Result.Songs;
            }
        }

        /// <summary>
        /// 获取指定歌曲列表
        /// </summary>
        /// <param name="item"></param>
        public async Task GetSongList(RadioItem item)
        {
            if (item.Songs != null)
                return;

            RenRenResponseArg<SongListEntity> ret = await RadioViewModel.Instance.RequestSongListByRadioId(item.Id, 
                LoginViewModel.Instance.HasLogin);
            if (ret.LocalError == null && ret.RemoteError == null)
            {
                item.Songs = ret.Result.Songs;
            }
        }

        #region  //唤醒时间设置

        public void RefreshStartTime()
        {
            //_startTime = DateTime.Now;
        }

        public bool CaculateTimeSpan()
        {
            //DateTime nowTime = DateTime.Now;
            //TimeSpan timeSpan = nowTime.Subtract(_startTime);
            //if (timeSpan.Hours >= 2)
            //{
            //    NotificationHelper.DisplayTextTost("亲，你还在听吗？", "你已经有很长时间没有做任何操作了...");
            //    return true;
            //}
            return false;
        }

        #endregion
    }
}
