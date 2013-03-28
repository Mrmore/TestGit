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
    class StatusViewModel : ViewModelBase<long, StatusEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static StatusViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly StatusViewModel _instance = new StatusViewModel();

        /// <summary>
        /// Request and update the status content by status id
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <param name="uid">user id</param>
        /// <param name="statusId">status id</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<StatusEntity>> RequestStatusByUid(int uid, long statusId)
        {
            RenRenResponseArg<StatusEntity> resp = await RequestById(statusId, uid);
            return resp;
        }

        /// <summary>
        /// Request and update the self status content
        /// </summary>
        /// <returns>the async result</returns>
        protected override Task<RenRenResponseArg<StatusEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request and update the status content
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<StatusEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<StatusEntity> resp = await App.RenRenService.GetStatus(seesionKey, secrectKey, uid, id);
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
        private StatusViewModel() 
        { }
    }
}
