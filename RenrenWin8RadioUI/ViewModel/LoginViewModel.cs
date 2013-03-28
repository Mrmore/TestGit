using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI;
using RenRenAPI.Entity;
using RenRenWin83GSdk.CustomEventArgs;
using RenrenWin8RadioUI;

namespace RenRenWin8Radio.ViewModel
{
    /// <summary>
    /// LoginViewModel
    /// In charge of the login page's control and data issues
    /// \note: this class is defined as singleton since it will be easily used in global
    /// </summary>
    class LoginViewModel : ViewModelBase<int, UserEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static LoginViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly LoginViewModel _instance = new LoginViewModel();

        public bool HasLogin = false;

        /// <summary>
        /// User login interface
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <param name="userName">user name</param>
        /// <param name="password">password</param>
        /// <returns>The aync error wrapped value</returns>
        public async Task<RenRenResponseArg<UserEntity>> Login(string userName, string password, string assertKey = null)
        {
            RenRenResponseArg<UserEntity> response = await Request(userName, password, assertKey);
            return response;
        }

        protected async override Task<RenRenResponseArg<UserEntity>> DoRequest(params object[] args)
        {
            if (args.Length < 2) throw new ArgumentException();

            string userName = (string)args[0];
            string password = (string)args[1];
            string assertKey = (string)args[2];
            RenRenResponseArg<UserEntity> response = await App.RenRenService.LogIn(userName, password, assertKey);
            return response;
        }

        protected override Task<RenRenResponseArg<UserEntity>> DoRequestById(int id, params object[] args)
        {
            throw new NotImplementedException();
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
        private LoginViewModel()
        { }
    }
}
