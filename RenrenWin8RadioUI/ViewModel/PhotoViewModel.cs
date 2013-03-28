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
    class PhotoViewModel : ViewModelBase<long, PhotoEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static PhotoViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly PhotoViewModel _instance = new PhotoViewModel();

        public async Task<RenRenResponseArg<PhotoEntity>> RequestPhotoByPid(int userId, long photoId, string password)
        {
            RenRenResponseArg<PhotoEntity> resp = await RequestById(photoId, userId, password);
            return resp;
        }

        protected override Task<RenRenResponseArg<PhotoEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<PhotoEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            string password = args.Length > 1 ? (string)args[1] : string.Empty;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<PhotoEntity> resp = await App.RenRenService.GetPhoto(seesionKey, secrectKey, uid, id, password);
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
        private PhotoViewModel() 
        { }
    }
}
