using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI;
using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenRenWin8Radio.Model;
using RenrenWin8RadioUI;

namespace RenRenWin8Radio.ViewModel
{
    class UserInfoViewModel : ViewModelBase<int, UserInfoEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static UserInfoViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly UserInfoViewModel _instance = new UserInfoViewModel();

        /// <summary>
        /// Request and update self user info
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns>The info result</returns>
        public async Task<RenRenResponseArg<UserInfoEntity>> RequestMyUserInfo()
        { 
            RenRenResponseArg<UserInfoEntity> resp = await Request();
            return resp;
        }

        /// <summary>
        /// Request and update the user info by uid
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<UserInfoEntity>> RequestUserInfoByUid(int uid)
        {
            RenRenResponseArg<UserInfoEntity> resp = await RequestById(uid);
            return resp;
        }

        protected async override Task<RenRenResponseArg<UserInfoEntity>> DoRequest(params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<UserInfoEntity> resp = await App.RenRenService.GetUserInfo(seesionKey, secrectKey);
            return resp;
        }

        protected async override Task<RenRenResponseArg<UserInfoEntity>> DoRequestById(int id, params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<UserInfoEntity> resp = await App.RenRenService.GetUserInfo(seesionKey, secrectKey, id);
            return resp;
        }

        protected override void DoReset()
        {
        }

        protected override void DoResetById(int id)
        {
        }

        /// <summary>
        /// private construct to protected against the outside create
        /// </summary>
        private UserInfoViewModel() 
        { }
    }
}
