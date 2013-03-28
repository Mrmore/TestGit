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
    class AlbumListViewModel : ViewModelBase<int, AlbumListEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static AlbumListViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly AlbumListViewModel _instance = new AlbumListViewModel();

        public async Task<RenRenResponseArg<AlbumListEntity>> RequestAlbumListByUid(int uid, int page, int pageSize)
        {
            RenRenResponseArg<AlbumListEntity> resp = await RequestById(uid);
            return resp;
        }

        protected override Task<RenRenResponseArg<AlbumListEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<AlbumListEntity>> DoRequestById(int id, params object[] args)
        {
            int page = args.Length > 0 ? (int)args[0] : -1;
            int pageSize = args.Length > 1 ? (int)args[1] : -1;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<AlbumListEntity> resp = await App.RenRenService.GetAlbumList(seesionKey, secrectKey, id, page, pageSize);
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
        private AlbumListViewModel() 
        { }
 
    }
}
