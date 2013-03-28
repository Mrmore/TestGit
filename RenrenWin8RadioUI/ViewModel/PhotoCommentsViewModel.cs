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
    class PhotoCommentsViewModel : ViewModelBase<long, PhotoCommentEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static PhotoCommentsViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly PhotoCommentsViewModel _instance = new PhotoCommentsViewModel();

        public async Task<RenRenResponseArg<PhotoCommentEntity>> RequestPhotoCommentsByPid(int userId, long albumId, long picId)
        {
            RenRenResponseArg<PhotoCommentEntity> resp = await RequestById(picId, userId, albumId);
            return resp;
        }

        protected override Task<RenRenResponseArg<PhotoCommentEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<PhotoCommentEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            long albumId = args.Length > 1 ? (long)args[1] : -1;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<PhotoCommentEntity> resp = await App.RenRenService.GetPhotoComments(seesionKey, secrectKey, uid, albumId, id);
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
        private PhotoCommentsViewModel() 
        { }
    }
}
