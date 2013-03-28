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
    class LatestVisistorViewModel : ViewModelBase<int, VisitorsEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static LatestVisistorViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly LatestVisistorViewModel _instance = new LatestVisistorViewModel();

        /// <summary>
        /// Request and update the self visitors list
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<VisitorsEntity>> RequestMyVisitorList()
        {
            RenRenResponseArg<VisitorsEntity> resp = await Request();
            return resp;
        }

        /// <summary>
        /// Request and update the visitors list by uid
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<VisitorsEntity>> RequestVisistorListByUid(int uid, int page = 1)
        {
            RenRenResponseArg<VisitorsEntity> resp = await RequestById(uid, page);
            return resp;
        }

        protected async override Task<RenRenResponseArg<VisitorsEntity>> DoRequest(params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<VisitorsEntity> resp = await App.RenRenService.GetVisitorList(seesionKey, secrectKey);
            return resp;
        }

        protected async override Task<RenRenResponseArg<VisitorsEntity>> DoRequestById(int id, params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<VisitorsEntity> resp = await App.RenRenService.GetVisitorList(seesionKey, secrectKey, id);
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
        private LatestVisistorViewModel() 
        { }
    }
}
