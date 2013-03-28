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
    class BlogPageViewModel : ViewModelBase<long, BlogEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static BlogPageViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly BlogPageViewModel _instance = new BlogPageViewModel();

        /// <summary>
        /// Request and update the blog content by blog id
        /// The wrapped convient method provided for outside call
        /// \note: if the uid is null means requesting self friends list
        /// </summary>
        /// <param name="uid">user id</param>
        /// <param name="page">page count</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<BlogEntity>> RequestBlogByUid(int uid, long blogId, string password)
        {
            RenRenResponseArg<BlogEntity> resp = await RequestById(blogId, uid, password);
            return resp;
        }

        /// <summary>
        /// Request and update the self blog content
        /// </summary>
        /// <returns>the async result</returns>
        protected override Task<RenRenResponseArg<BlogEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request and update the blog content
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<BlogEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            string password = args.Length > 1 ? (string)args[1] : string.Empty; ;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<BlogEntity> resp = await App.RenRenService.GetBlog(seesionKey, secrectKey, uid, id, password);
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
        private BlogPageViewModel() 
        { }
    }
}
