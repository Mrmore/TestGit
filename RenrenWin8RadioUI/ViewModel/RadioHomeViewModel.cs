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
    /// bool means if this is a logined user radio
    /// true: logined user
    /// false: unlogined user
    /// </summary>
    class RadioHomeViewModel : ViewModelBase<bool, GetHomeEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static RadioHomeViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly RadioHomeViewModel _instance = new RadioHomeViewModel();

        public async Task<RenRenResponseArg<GetHomeEntity>> RequestHomeByLoginMode(bool loginMode)
        {
            RenRenResponseArg<GetHomeEntity> resp = await RequestById(loginMode);
            return resp;
        }

        public static string UriDecode(string encodedUri, bool loginMode)
        {
            string retUri = null;
            if (loginMode)
            {
                string secrectKey = LoginViewModel.Instance.Model.Secret_key;
                if (string.IsNullOrEmpty(secrectKey))
                {
                    secrectKey = RenRenAPI.Constants.ConstantValue.SecretKey;
                }
                retUri = RenRenAPI.Helper.DESUtils.Decode(encodedUri, secrectKey, secrectKey);
            }
            else
            {
                string secrectKey = RenRenAPI.Constants.ConstantValue.SecretKey;
                retUri = RenRenAPI.Helper.DESUtils.Decode(encodedUri, secrectKey, secrectKey);
            }

            return retUri;
        }

        protected override Task<RenRenResponseArg<GetHomeEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        protected async override Task<RenRenResponseArg<GetHomeEntity>> DoRequestById(bool loginMode, params object[] args)
        {
            RenRenResponseArg<GetHomeEntity> resp = null;
            if (loginMode)
            {
                string seesionKey = LoginViewModel.Instance.Model.Session_key;
                string secrectKey = LoginViewModel.Instance.Model.Secret_key;
                string uid = LoginViewModel.Instance.Model.Uid.ToString();
                string access_token = LoginViewModel.Instance.Model.loginInfo.access_token;
                resp = await App.RenRenService.RadioGetHome(seesionKey, secrectKey,access_token, uid, "1", "768*1366");
            }
            else
            {
                resp = await App.RenRenService.RadioGetHome(null, null, null, "7f2c43fc-e0f3-4968-b0e0-adab417cf0d9", "1", "768*1366");
            }
            return resp;
        }

        /// <summary>
        /// Reset overall models
        /// </summary>
        protected override void DoReset()
        {
        }

        protected override void DoResetById(bool id)
        {
        }

        /// <summary>
        /// private construct to protected against the outside create
        /// </summary>
        private RadioHomeViewModel() 
        { }
    }
}
