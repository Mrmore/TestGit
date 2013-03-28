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
    /// <summary>
    /// id means the radio id
    /// </summary>
    class RadioViewModel : ViewModelBase<uint, SongListEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static RadioViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly RadioViewModel _instance = new RadioViewModel();

        public async Task<RenRenResponseArg<SongListEntity>> RequestSongListByRadioId(uint radioId, bool loginMode)
        {
            RenRenResponseArg<SongListEntity> resp = await RequestById(radioId, loginMode);
            return resp;
        }

        public static async Task<RenRenResponseArg<SongEntity>> RequestNextSong(uint radioId, bool loginMode, uint songId, uint duration)
        {
            RenRenResponseArg<SongEntity> resp = null;
            if (loginMode)
            {
                string seesionKey = LoginViewModel.Instance.Model.Session_key;
                string secrectKey = LoginViewModel.Instance.Model.Secret_key;
                string uid = LoginViewModel.Instance.Model.Uid.ToString();
                string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
                resp = await App.RenRenService.GetNextSong(seesionKey, secrectKey,access_token, uid, radioId, songId, duration);
            }
            else
            {
                resp = await App.RenRenService.GetNextSong(null, null,null, "7f2c43fc-e0f3-4968-b0e0-adab417cf0d9", radioId, songId, duration);
            }
            return resp;
        }

        public static async Task<RenRenResponseArg<CommentReultEntity>> AddFavorite(uint songId)
        {
            RenRenResponseArg<CommentReultEntity> resp = null;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            string uid = LoginViewModel.Instance.Model.Uid.ToString();
            string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
            resp = await App.RenRenService.RadioAddFavorite(seesionKey, secrectKey, access_token, uid, songId);
            return resp;
        }

        public static async Task<RenRenResponseArg<CommentReultEntity>> RemoveFavorite(uint songId)
        {
            RenRenResponseArg<CommentReultEntity> resp = null;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            string uid = LoginViewModel.Instance.Model.Uid.ToString();
            string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
            resp = await App.RenRenService.RadioRemoveFavorite(seesionKey, secrectKey, access_token, uid, songId);
            return resp;
        }

        public static async Task<RenRenResponseArg<CommentReultEntity>> ShareSong(uint songId, string comment)
        {
            RenRenResponseArg<CommentReultEntity> resp = null;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            string uid = LoginViewModel.Instance.Model.Uid.ToString();
            string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
            resp = await App.RenRenService.RadioShareSong(seesionKey, secrectKey, access_token, uid, songId, comment);
            return resp;
        }

        public static async Task<RenRenResponseArg<SongEntity>> DeleteSong(uint songId, uint radioId)
        {
            RenRenResponseArg<SongEntity> resp = null;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            string uid = LoginViewModel.Instance.Model.Uid.ToString();
            string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
            resp = await App.RenRenService.RadioDeleteSong(seesionKey, secrectKey, access_token, uid, songId, radioId);
            return resp;
        }

        public static string UriDecode(string encodedUri, bool loginMode)
        {
            string retUri = null;
            //if (loginMode)
            //{
            //    string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            //    if (string.IsNullOrEmpty(secrectKey))
            //    {
            //        secrectKey = RenRenAPI.Constants.ConstantValue.SecretKey;
            //    }
            //    retUri = RenRenAPI.Helper.DESUtils.Decode(encodedUri, secrectKey, secrectKey);
            //}
            //else
            //{
            //    string secrectKey = RenRenAPI.Constants.ConstantValue.SecretKey;
            //    retUri = RenRenAPI.Helper.DESUtils.Decode(encodedUri, secrectKey, secrectKey);
            //}

            return retUri;
        }

        protected override Task<RenRenResponseArg<SongListEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<SongListEntity>> DoRequestById(uint radioId, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();

            bool loginMode = (bool)args[0];
            RenRenResponseArg<SongListEntity> resp = null;
            if (loginMode)
            {
                string seesionKey = LoginViewModel.Instance.Model.Session_key;
                string secrectKey = LoginViewModel.Instance.Model.Secret_key;
                string uid = LoginViewModel.Instance.Model.Uid.ToString();
                string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
                resp = await App.RenRenService.GetRadio(seesionKey, secrectKey, access_token, uid, radioId);
            }
            else
            {
                resp = await App.RenRenService.GetRadio(null, null, null, "7f2c43fc-e0f3-4968-b0e0-adab417cf0d9", radioId);
            }
            return resp;
        }

        /// <summary>
        /// Reset overall models
        /// </summary>
        protected override void DoReset()
        {
        }

        protected override void DoResetById(uint id)
        {
        }

        /// <summary>
        /// private construct to protected against the outside create
        /// </summary>
        private RadioViewModel() 
        { }
    }
}
