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
    class NewFeedsViewModel : ViewModelBase<int, FeedListEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static NewFeedsViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly NewFeedsViewModel _instance = new NewFeedsViewModel();

        /// <summary>
        /// Request and update the self all new feed list
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<FeedListEntity>> RequestMyFeedList()
        {
            RenRenResponseArg<FeedListEntity> resp = await Request();
            return resp;
        }

        /// <summary>
        /// Request and update the self new feed list by page size
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<FeedListEntity>> RequestMyFeedList(int page = 1, int pageSize = 10)
        {
            RenRenResponseArg<FeedListEntity> resp = await Request(page, pageSize);
            return resp;
        }

        /// <summary>
        /// Request and update the new feed list by uid
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<FeedListEntity>> RequestFeedListByUid(int uid, int page = 1, int pageSize = 10)
        {
            RenRenResponseArg<FeedListEntity> resp = await RequestById(uid, page, pageSize);
            return resp;
        }

        /// <summary>
        /// Request and update the self friends list
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<FeedListEntity>> DoRequest(params object[] args)
        { 
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<FeedListEntity> resp = null;
            if (args.Length > 0) // Get the page size list
            {
                int page = (int)args[0];
                int pageSize = args.Length > 1 ? (int)args[1] : 10; // by default, page size is 10
                resp = await App.RenRenService.GetFeedList(seesionKey, secrectKey, page, pageSize, -1);
            }
            else
            {
                resp = await App.RenRenService.GetFeedList(seesionKey, secrectKey);
            }
            return resp;
        }

        /// <summary>
        /// Request and update the friends list by uid
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<FeedListEntity>> DoRequestById(int id, params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            int page = args.Length > 0 ? (int)args[0] : 1; // by default, page is 1
            int pageSize = args.Length > 1 ? (int)args[1] : 10; // by default, page size is 10
            RenRenResponseArg<FeedListEntity> resp = await App.RenRenService.GetFeedList(seesionKey, secrectKey, page, pageSize, id);
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
        private NewFeedsViewModel() 
        { }
    }
}
