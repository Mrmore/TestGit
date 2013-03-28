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
    class BlogCommentsViewModel : ViewModelBase<long, BlogCommentEntity>
    {
        /// <summary>
        /// Singleton property
        /// </summary>
        public static BlogCommentsViewModel Instance
        {
            get { return _instance; }
        }
        private static readonly BlogCommentsViewModel _instance = new BlogCommentsViewModel();

        /// <summary>
        /// Request and update the blog comment list by uid
        /// The wrapped convient method provided for outside call
        /// \note: if the uid is null means requesting self friends list
        /// </summary>
        /// <param name="uid">user id</param>
        /// <param name="page">page count</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<BlogCommentEntity>> RequestBlogCommentsByUid(int uid, long blogId, int page, string password)
        {
            RenRenResponseArg<BlogCommentEntity> resp = await RequestById(blogId, uid, page, password);
            return resp;
        }

        /// <summary>
        /// Request and update the self  blog comment list
        /// </summary>
        /// <returns>the async result</returns>
        protected override Task<RenRenResponseArg<BlogCommentEntity>> DoRequest(params object[] args)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Request and update the  blog comment list by uid
        /// </summary>
        /// <returns>the async result</returns>
        protected async override Task<RenRenResponseArg<BlogCommentEntity>> DoRequestById(long id, params object[] args)
        {
            if (args.Length < 1) throw new ArgumentException();
            int uid = (int)args[0];
            int page = args.Length > 1 ? (int)args[1] : 1;
            string password = args.Length > 2 ? (string)args[2] : string.Empty;
            string seesionKey = LoginViewModel.Instance.Model.Session_key;
            string secrectKey = LoginViewModel.Instance.Model.Secret_key;

            RenRenResponseArg<BlogCommentEntity> resp = await App.RenRenService.GetBlogComment(seesionKey, secrectKey, uid, id, page, password);
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
        private BlogCommentsViewModel() 
        { }
    }
}
