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
    class AlbumViewModel : ViewModelBase<long, PhotoListEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static AlbumViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly AlbumViewModel _instance = new AlbumViewModel();

        public async Task<RenRenResponseArg<PhotoListEntity>> RequestPhotoListByAlbumId(int userId, long albumId, int page, int pageSize, string password)
        {
            RenRenResponseArg<PhotoListEntity> resp = await RequestById(albumId, userId, page, pageSize, password);
            return resp;
        }

        protected override Task<RenRenResponseArg<PhotoListEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<PhotoListEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            int page = args.Length > 1 ? (int)args[1] : -1;
            int pageSize = args.Length > 2 ? (int)args[2] : -1;
            string password = args.Length > 3 ? (string)args[3] : string.Empty;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<PhotoListEntity> resp = await App.RenRenService.GetPhotoList(seesionKey, secrectKey, uid, id, page, pageSize, password);
            return resp;
        }

        /// <summary>
        /// Reset overall models
        /// </summary>
        protected override void DoReset()
        {
        }

        protected override void DoResetById(long id)
        {
        }

        /// <summary>
        /// private construct to protected against the outside create
        /// </summary>
        private AlbumViewModel() 
        { }
    }
}
