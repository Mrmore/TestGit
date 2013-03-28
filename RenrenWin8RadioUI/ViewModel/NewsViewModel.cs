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
    class NewsViewModel : ViewModelBase<int, NewsCountEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static NewsViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly NewsViewModel _instance = new NewsViewModel();

        /// <summary>
        /// Request and update the self news count
        /// The wrapped convient method provided for outside call
        /// </summary>
        /// <returns></returns>
        public async Task<RenRenResponseArg<NewsCountEntity>> RequestMyNewsCount()
        { 
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<NewsCountEntity> resp = await App.RenRenService.GetNewsCount(seesionKey, secrectKey);

            Model = resp.Result;
            return resp;
        }

        protected async override Task<RenRenResponseArg<NewsCountEntity>> DoRequest(params object[] args)
        {
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;
            RenRenResponseArg<NewsCountEntity> resp = await App.RenRenService.GetNewsCount(seesionKey, secrectKey);
            return resp;
        }

        protected override Task<RenRenResponseArg<NewsCountEntity>> DoRequestById(int id, params object[] args)
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
        private NewsViewModel()
        { }
    }
}
