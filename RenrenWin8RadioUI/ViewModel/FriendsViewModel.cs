using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI;
using RenRenAPI.Entity;
using RenRenAPI.Model;
using RenRenWin83GSdk.CustomEventArgs;
using RenrenWin8RadioUI;

namespace RenRenWin8Radio.ViewModel
{
    class FriendsViewModel : ViewModelBase<int, FriendListEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static FriendsViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly FriendsViewModel _instance = new FriendsViewModel();

        /// <summary>
        /// Request and update the self friend list
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<FriendListEntity>> RequestMyFriendList()
        {
            RenRenResponseArg<FriendListEntity> resp = await Request();
            return resp;
        }

        /// <summary>
        /// Request and update the friend list by uid
        /// The wrapped convient method provided for outside call
        /// \note: if the uid is null means requesting self friends list
        /// </summary>
        /// <param name="uid">user id</param>
        /// <param name="page">page count</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<FriendListEntity>> RequestFriendListByUid(int uid, int page = 1)
        {
            RenRenResponseArg<FriendListEntity> resp = await RequestById(uid, page);
            return resp;
        }

        /// <summary>
        /// Request and update the self friends list
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<FriendListEntity>> DoRequest(params object[] args)
        { 
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<FriendListEntity> resp = await App.RenRenService.GetFriendList(seesionKey, secrectKey);
            return resp;
        }

        /// <summary>
        /// Request and update the friends list by uid
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<FriendListEntity>> DoRequestById(int id, params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            int page = args.Length > 0 ? (int)args[0] : 1;
            RenRenResponseArg<FriendListEntity> resp = await App.RenRenService.GetFriendList(seesionKey, secrectKey, id, page);
            return resp;
        }

        /// <summary>
        /// Reset overall models
        /// </summary>
        protected override void DoReset()
        {
        }

        protected override void DoResetById(int id)
        {
        }

        /// <summary>
        /// private construct to protected against the outside create
        /// </summary>
        private FriendsViewModel() 
        { }
    }
}
