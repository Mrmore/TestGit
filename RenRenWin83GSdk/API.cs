using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using HttpWebRequestLibrary;
using RenRenAPI.Constants;
using RenRenAPI.Entity;
using RenRenAPI.Helper;
using System.Runtime.Serialization.Json;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using System.Threading.Tasks;
using Windows.UI.Core;
using System.Reflection;
using RenRenWin83GSdk.CustomEventArgs;
using System.Diagnostics;
using RenRenWin83GSdk.Entity;

namespace RenRenAPI
{
    public partial class API
    {
        #region PublicMethod
        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码</param>
        public async Task<RenRenResponseArg<UserEntity>> LogIn(string username, string password, string assertKey)
        {
            password = MD5Core.GetHashString(password).ToLower();
            byte[] bytes = Encoding.UTF8.GetBytes("Win8 Pad");
            string deviceId = Convert.ToBase64String(bytes);

            //if (password.Length > 50)
            //    password = password.Substring(0, 50);
            //if (username.Length > 50)
            //    username = username.Substring(0, 50);
            //if (deviceId.Length > 50)
            //    deviceId = deviceId.Substring(0, 50);

            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("client_id", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("client_secret", ConstantValue.SecretKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("password", password));
            parameters.Add(new RequestParameterEntity("username", username));
            if (!String.IsNullOrEmpty(assertKey))
            {
                parameters.Add(new RequestParameterEntity("captcha", assertKey));
            }
            parameters.Add(new RequestParameterEntity("grant_type", "password"));
            parameters.Add(new RequestParameterEntity("password_type", "md5"));
            parameters.Add(new RequestParameterEntity("scope", "radio.getHome,radio.getRadio,radio.getNextSong,radio.addFavorite,radio.removeFavorite,radio.shareSong,radio.remove"));

            //string sig = ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey);scope
            //if (sig.Length > 50)
            //    sig = sig.Substring(0, 50);
            //parameters.Add(new RequestParameterEntity("sig", sig));

            var result = await agentReponseHandler<LoginEntity, RenRenResponseArg<LoginEntity>>(parameters, ConstantValue.PostMethod, ConstantValue.LoginUri);

            RenRenResponseArg<UserEntity> ret = null;
            //检查登陆信息,网络错误
            if (result.RemoteError != null)
            {
                ret = new RenRenResponseArg<UserEntity>(result.RemoteError);
                return ret;
            }
            //本地解析等异常
            if (result.LocalError != null)
            {
                ret = new RenRenResponseArg<UserEntity>(result.LocalError);
                return ret;
            }

            UserEntity userInfo = new UserEntity();
            ret = new RenRenResponseArg<UserEntity>(userInfo);
            //数据无
            if (result.Result == null || string.IsNullOrEmpty(result.Result.access_token))
            {
                return ret;
            }

            //赋值
            userInfo.loginInfo = result.Result;
            //获取个人信息

            List<RequestParameterEntity> parametersUser = new List<RequestParameterEntity>();
            parametersUser.Add(new RequestParameterEntity("method", Method.GetUserInfo));
            parametersUser.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parametersUser.Add(new RequestParameterEntity("v", "1.0"));
            parametersUser.Add(new RequestParameterEntity("access_token", result.Result.access_token));
            parametersUser.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parametersUser, ConstantValue.SecretKey)));

            var userresult = await agentReponseHandler<UserInfoEntity, RenRenResponseArg<UserInfoEntity>>(parametersUser, ConstantValue.PostMethod);

            if (userresult.RemoteError == null && userresult.LocalError == null && userresult.Result != null)
            {
                userInfo.Uid = userresult.Result.User_id;
                userInfo.User_name = userresult.Result.User_name;
                userInfo.Head_url = userresult.Result.Head_url;
            }

            return ret;
        }

        /// <summary>
        /// 获取登录用户当前状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<StatusEntity>> GetStatus(string sessionKey, string userSecretKey)
        {
            return await GetStatus(sessionKey, userSecretKey, -1, -1);
        }
        /// <summary>
        /// 获取指定用户最近状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<StatusEntity>> GetStatus(string sessionKey, string userSecretKey, int userId)
        {
            return await GetStatus(sessionKey, userSecretKey, userId, -1);
        }
        /// <summary>
        /// 获取指定用户指定状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="statusId"></param>
        public async Task<RenRenResponseArg<StatusEntity>> GetStatus(string sessionKey, string userSecretKey, int userId, long statusId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetStatus));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (statusId != -1)
                parameters.Add(new RequestParameterEntity("id", statusId.ToString()));
            string sig = ApiHelper.GenerateSig(parameters, userSecretKey);
            parameters.Add(new RequestParameterEntity("sig", sig));

            var result = await agentReponseHandler<StatusEntity, RenRenResponseArg<StatusEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取登录用户信息列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<StatusListEntity>> GetStatusList(string sessionKey, string userSecretKey)
        {
            return await GetStatusList(sessionKey, userSecretKey, -1, -1, -1);
        }
        /// <summary>
        /// 获取指定用户信息列表（支持扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        public async Task<RenRenResponseArg<StatusListEntity>> GetStatusList(string sessionKey, string userSecretKey, int userId, int page, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetStatusList));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<StatusListEntity, RenRenResponseArg<StatusListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定状态的评论列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="statusId"></param>
        /// <param name="ownerId"></param>
        public async Task<RenRenResponseArg<StatusCommentsEntity>> GetStatusComments(string sessionKey, string userSecretKey, long statusId, int ownerId)
        {
            return await GetStatusComments(sessionKey, userSecretKey, statusId, ownerId, -1, -1, -1, -1);
        }

        public async Task<RenRenResponseArg<StatusCommentsEntity>> GetStatusComments(string sessionKey, string userSecretKey, long statusId, int ownerId, int page)
        {
            return await GetStatusComments(sessionKey, userSecretKey, statusId, ownerId, page, -1, -1, -1);
        }
        /// <summary>
        /// 获取指定状态的评论列表（支持扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="statusId"></param>
        /// <param name="ownerId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="withStatusDetail"></param>
        /// <param name="needSort"></param>
        public async Task<RenRenResponseArg<StatusCommentsEntity>> GetStatusComments(string sessionKey, string userSecretKey, long statusId, int ownerId, int page, int pageSize, int withStatusDetail, int needSort)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetStatusComments));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("id", statusId.ToString()));
            parameters.Add(new RequestParameterEntity("owner_id", ownerId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));
            if (withStatusDetail != -1)
                parameters.Add(new RequestParameterEntity("with_status", pageSize.ToString()));
            if (needSort != -1)
                parameters.Add(new RequestParameterEntity("sort", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<StatusCommentsEntity, RenRenResponseArg<StatusCommentsEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定用户的日志列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<BlogListEntity>> GetBlogList(string sessionKey, string userSecretKey, int userId)
        {
            return await GetBlogList(sessionKey, userSecretKey, userId, -1, -1);
        }

        /// <summary>
        /// 获取指定用户的日志列表（支持扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<BlogListEntity>> GetBlogList(string sessionKey, string userSecretKey, int userId, int pageNumber, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetUserBlogs));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (pageNumber != -1)
                parameters.Add(new RequestParameterEntity("page", pageNumber.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<BlogListEntity, RenRenResponseArg<BlogListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定日志信息
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="blogId"></param>
        public async Task<RenRenResponseArg<BlogEntity>> GetBlog(string sessionKey, string userSecretKey, int userId, long blogId)
        {
            return await GetBlog(sessionKey, userSecretKey, userId, blogId, null);
        }

        public async Task<RenRenResponseArg<BlogEntity>> GetBlog(string sessionKey, string userSecretKey, int userId, long blogId, string password)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetBlog));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("id", blogId.ToString()));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));
            parameters.Add(new RequestParameterEntity("need_html", "1"));   //设置获取日志信息为HTML格式
            parameters.Add(new RequestParameterEntity("only_desc", "0"));  // 设置不是只要摘要
            parameters.Add(new RequestParameterEntity("content_in_page", "0")); // 设置不需要分页
            if (!string.IsNullOrEmpty(password))
                parameters.Add(new RequestParameterEntity("password", password));


            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<BlogEntity, RenRenResponseArg<BlogEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<BlogCommentEntity>> GetBlogComment(string sessionKey, string userSecretKey, int userId, long blogId)
        {
            return await GetBlogComment(sessionKey, userSecretKey, userId, blogId, -1);
        }

        /// <summary>
        /// 获取指定日志评论信息
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="blogId"></param>
        public async Task<RenRenResponseArg<BlogCommentEntity>> GetBlogComment(string sessionKey, string userSecretKey, int userId, long blogId, int page)
        {
            return await GetBlogComment(sessionKey, userSecretKey, userId, blogId, page, null);
        }

        public async Task<RenRenResponseArg<BlogCommentEntity>> GetBlogComment(string sessionKey, string userSecretKey, int userId, long blogId, int page, string password)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetBlogComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("id", blogId.ToString()));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (!string.IsNullOrEmpty(password))
                parameters.Add(new RequestParameterEntity("password", password));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<BlogCommentEntity, RenRenResponseArg<BlogCommentEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取当前用户好友列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<FriendListEntity>> GetFriendList(string sessionKey, string userSecretKey)
        {
            return await GetFriendList(sessionKey, userSecretKey, -1, -1);
        }
        /// <summary>
        /// 获取指定用户好友列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        public async Task<RenRenResponseArg<FriendListEntity>> GetFriendList(string sessionKey, string userSecretKey, int userId, int page)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetFriends));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("hasNetwork", "1"));
            parameters.Add(new RequestParameterEntity("hasGroup", "1"));
            parameters.Add(new RequestParameterEntity("hasGender", "1"));

            if (userId != -1)
                parameters.Add(new RequestParameterEntity("userId", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<FriendListEntity, RenRenResponseArg<FriendListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定用户相册列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<AlbumListEntity>> GetAlbumList(string sessionKey, string userSecretKey, int userId)
        {
            return await GetAlbumList(sessionKey, userSecretKey, userId, -1, -1);
        }
        /// <summary>
        /// 获取指定用户相册列表 可扩展
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        public async Task<RenRenResponseArg<AlbumListEntity>> GetAlbumList(string sessionKey, string userSecretKey, int userId, int page, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetAlbums));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<AlbumListEntity, RenRenResponseArg<AlbumListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        //NOTE:接口不完整
        /// <summary>
        /// 获取指定相册内照片列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="albumId"></param>
        public async Task<RenRenResponseArg<PhotoListEntity>> GetPhotoList(string sessionKey, string userSecretKey, int userId, long albumId)
        {
            return await GetPhotoList(sessionKey, userSecretKey, userId, albumId, -1, -1);
        }

        /// <summary>
        /// 获取指定相册内照片列表[支持获取更多]
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="albumId"></param>
        public async Task<RenRenResponseArg<PhotoListEntity>> GetPhotoList(string sessionKey, string userSecretKey, int userId, long albumId, int page, int page_size)
        {
            return await GetPhotoList(sessionKey, userSecretKey, userId, albumId, page, page_size, null);
        }

        public async Task<RenRenResponseArg<PhotoListEntity>> GetPhotoList(string sessionKey, string userSecretKey, int userId, long albumId, int page, int page_size, string password)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetPhotos));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            parameters.Add(new RequestParameterEntity("aid", albumId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (page_size != -1)
                parameters.Add(new RequestParameterEntity("page_size", page_size.ToString()));
            if (!string.IsNullOrEmpty(password))
                parameters.Add(new RequestParameterEntity("password", password.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<PhotoListEntity, RenRenResponseArg<PhotoListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<PhotoEntity>> GetPhoto(string sessionKey, string userSecretKey, int userId, long photoId, string password)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetPhotos));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            parameters.Add(new RequestParameterEntity("pid", photoId.ToString()));
            if (!string.IsNullOrEmpty(password))
                parameters.Add(new RequestParameterEntity("password", password));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<PhotoEntity, RenRenResponseArg<PhotoEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定照片评论
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="albumId"></param>
        /// <param name="picId"></param>
        public async Task<RenRenResponseArg<PhotoCommentEntity>> GetPhotoComments(string sessionKey, string userSecretKey, int userId, long albumId, long picId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetPhotoComments));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            parameters.Add(new RequestParameterEntity("aid", albumId.ToString()));
            parameters.Add(new RequestParameterEntity("pid", picId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<PhotoCommentEntity, RenRenResponseArg<PhotoCommentEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 获取指定照片评论[支持分页]
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="albumId"></param>
        /// <param name="picId"></param>
        /// <param name="page"></param>
        /// <param name="pagesize"></param>
        public async Task<RenRenResponseArg<PhotoCommentEntity>> GetPhotoComments(string sessionKey, string userSecretKey, int userId, long albumId, long picId, int page, int pagesize, string password)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetPhotoComments));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            parameters.Add(new RequestParameterEntity("aid", albumId.ToString()));
            parameters.Add(new RequestParameterEntity("pid", picId.ToString()));
            parameters.Add(new RequestParameterEntity("page", page.ToString()));
            parameters.Add(new RequestParameterEntity("page_size", pagesize.ToString()));
            parameters.Add(new RequestParameterEntity("password", password));


            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<PhotoCommentEntity, RenRenResponseArg<PhotoCommentEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取登录用户新鲜事
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<FeedListEntity>> GetFeedList(string sessionKey, string userSecretKey)
        {
            return await GetFeedList(sessionKey, userSecretKey, -1, -1, -1);
        }
        /// <summary>
        /// 获取指定用户新鲜事
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<FeedListEntity>> GetFeedList(string sessionKey, string userSecretKey, int userId)
        {
            return await GetFeedList(sessionKey, userSecretKey, -1, -1, userId);
        }
        /// <summary>
        /// 获取新鲜事（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<FeedListEntity>> GetFeedList(string sessionKey, string userSecretKey, int page, int pageSize, int userId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetFeed));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("type", "102,103,104,107,501,502,601,701,709"));//
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<FeedListEntity, RenRenResponseArg<FeedListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取登录用户账户信息
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<UserInfoEntity>> GetUserInfo(string sessionKey, string userSecretKey)
        {
            return await GetUserInfo(sessionKey, userSecretKey, -1);
        }
        /// <summary>
        /// 获取指定用户账户信息
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<UserInfoEntity>> GetUserInfo(string sessionKey, string userSecretKey, int userId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetUserInfo));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("type", "8191"));//根据Type生成的值 获取用户所有信息
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<UserInfoEntity, RenRenResponseArg<UserInfoEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取登录用户最近来访
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<VisitorsEntity>> GetVisitorList(string sessionKey, string userSecretKey)
        {
            return await GetVisitorList(sessionKey, userSecretKey, -1);
        }
        /// <summary>
        /// 获取指定用户最近来访
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<VisitorsEntity>> GetVisitorList(string sessionKey, string userSecretKey, int userId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetVisitors));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<VisitorsEntity, RenRenResponseArg<VisitorsEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定用户留言板
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<GossipListEntity>> GetGossips(string sessionKey, string userSecretKey, int userId)
        {
            return await GetGossips(sessionKey, userSecretKey, userId, -1, -1);
        }
        /// <summary>
        /// 获取指定用户留言板（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        public async Task<RenRenResponseArg<GossipListEntity>> GetGossips(string sessionKey, string userSecretKey, int userId, int page, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetGossips));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<GossipListEntity, RenRenResponseArg<GossipListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        //todo:注意BoxType属性 默认不设置的情况下得到的是发件箱内容
        /// <summary>
        /// 获取站内信
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<MessageListEntity>> GetMessages(string sessionKey, string userSecretKey)
        {
            return await GetMessages(sessionKey, userSecretKey, -1, -1, -1, -1, -1);
        }
        /// <summary>
        /// 获取站内信（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<MessageListEntity>> GetMessages(string sessionKey, string userSecretKey, int boxType, int page, int count, int excludeList, int delNews)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetMessages));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (boxType != -1)
                parameters.Add(new RequestParameterEntity("box", boxType.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (count != -1)
                parameters.Add(new RequestParameterEntity("count", count.ToString()));
            if (excludeList != -1)
                parameters.Add(new RequestParameterEntity("exclude_list", count.ToString()));
            if (delNews != -1)
                parameters.Add(new RequestParameterEntity("del_news", count.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<MessageListEntity, RenRenResponseArg<MessageListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取登录用户好友申请列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<RequestFriendsListEntity>> GetRequestFriendList(string sessionKey, string userSecretKey)
        {
            return await GetRequestFriendList(sessionKey, userSecretKey, -1, -1, -1, -1);
        }
        /// <summary>
        /// 获取登录用户好友申请列表（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="excludeList"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="delNew"></param>
        public async Task<RenRenResponseArg<RequestFriendsListEntity>> GetRequestFriendList(string sessionKey, string userSecretKey, int excludeList, int page, int pageSize, int delNew)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetFriendsRequest));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (excludeList != -1)
                parameters.Add(new RequestParameterEntity("exclude_list", excludeList.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));
            if (excludeList != -1)
                parameters.Add(new RequestParameterEntity("exclude_list", excludeList.ToString()));
            if (delNew != -1)
                parameters.Add(new RequestParameterEntity("del_news", delNew.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<RequestFriendsListEntity, RenRenResponseArg<RequestFriendsListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取与指定好友的公共好友
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<CommonFriendsEntity>> GetCommonFriends(string sessionKey, string userSecretKey, int userId)
        {
            return await GetCommonFriends(sessionKey, userSecretKey, userId, -1, -1, 1, 1);
        }
        /// <summary>
        /// 获取与指定好友的公共好友（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="hasHeadImg"></param>
        /// <param name="hasNetwork"></param>
        public async Task<RenRenResponseArg<CommonFriendsEntity>> GetCommonFriends(string sessionKey, string userSecretKey, int userId, int page, int pageSize, int hasHeadImg, int hasNetwork)
        {
            //NOTE：文档错误
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetSharedFriends));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("userId", userId.ToString()));

            parameters.Add(new RequestParameterEntity("hasOnline", "1"));
            parameters.Add(new RequestParameterEntity("hasGroup", "1"));


            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("pageSize", pageSize.ToString()));
            if (hasHeadImg != -1)
                parameters.Add(new RequestParameterEntity("hasHeadImg", hasHeadImg.ToString()));
            if (hasNetwork != -1)
                parameters.Add(new RequestParameterEntity("hasNetwork", hasNetwork.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommonFriendsEntity, RenRenResponseArg<CommonFriendsEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 接受好友申请
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<AcceptFriendEntity>> AcceptFriendRequest(string sessionKey, string userSecretKey, int userId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.AcceptFriendRequest));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<AcceptFriendEntity, RenRenResponseArg<AcceptFriendEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 忽略好友申请
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        public async Task<RenRenResponseArg<DenyFriendEntity>> DenyFriendRequest(string sessionKey, string userSecretKey, int userId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.DenyFriendRequest));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<DenyFriendEntity, RenRenResponseArg<DenyFriendEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 获取消息数量
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<NewsCountEntity>> GetNewsCount(string sessionKey, string userSecretKey)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetNewsCount));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("type", "5"));//获取留言 好友请求 

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<NewsCountEntity, RenRenResponseArg<NewsCountEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取消息列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<NewsListEntity>> GetNewsList(string sessionKey, string userSecretKey)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetNewsList));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("type", "1"));//获取留言

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<NewsListEntity, RenRenResponseArg<NewsListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 将消息置为已读
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="newsId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> MakeNewsToReadState(string sessionKey, string userSecretKey, long newsId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.ReadNewsById));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("news_id", newsId.ToString()));//获取留言

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<ShareCommentEntity>> GetShareCommentList(string sessionKey, string userSecretKey, long shareId, int shareOwnerId)
        {
            return await GetShareCommentList(sessionKey, userSecretKey, shareId, shareOwnerId, 1, 10);
        }

        public async Task<RenRenResponseArg<ShareCommentEntity>> GetShareCommentList(string sessionKey, string userSecretKey, long shareId, int shareOwnerId, int page, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetShareComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("id", shareId.ToString()));
            parameters.Add(new RequestParameterEntity("user_id", shareOwnerId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<ShareCommentEntity, RenRenResponseArg<ShareCommentEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取指定分享
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<ShareEntity>> GetTheShare(string sessionKey, string userSecretKey, int userId, long shareId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetTheShare));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (shareId != -1)
                parameters.Add(new RequestParameterEntity("id", shareId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<ShareEntity, RenRenResponseArg<ShareEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取分享列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<ShareListEntity>> GetShareList(string sessionKey, string userSecretKey)
        {
            return await GetShareList(sessionKey, userSecretKey, -1, -1, -1);
        }
        /// <summary>
        /// 获取分享列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<ShareListEntity>> GetShareList(string sessionKey, string userSecretKey, int userId)
        {
            return await GetShareList(sessionKey, userSecretKey, userId, -1, -1);
        }
        /// <summary>
        /// 获取分享列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<ShareListEntity>> GetShareList(string sessionKey, string userSecretKey, int userId, int page, int pageSize)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetShareList));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            if (userId != -1)
                parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (page != -1)
                parameters.Add(new RequestParameterEntity("page", page.ToString()));
            if (pageSize != -1)
                parameters.Add(new RequestParameterEntity("page_size", pageSize.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<ShareListEntity, RenRenResponseArg<ShareListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 获取在线好友列表
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        public async Task<RenRenResponseArg<FriendListEntity>> GetOnlineFriendList(string sessionKey, string userSecretKey)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetOnlineFriendList));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("isOnline", "1"));
            parameters.Add(new RequestParameterEntity("hasNetwork", "1"));
            parameters.Add(new RequestParameterEntity("hasGroup", "1"));
            parameters.Add(new RequestParameterEntity("hasGender", "1"));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<FriendListEntity, RenRenResponseArg<FriendListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 判断是否为好友
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userIdOne"></param>
        /// <param name="userIdTwo"></param>
        public async Task<RenRenResponseArg<IsFriendResultEntity>> CheckIsFriend(string sessionKey, string userSecretKey, int userIdOne, int userIdTwo)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.CheckAreFriends));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("user_id_list_1", userIdOne.ToString()));
            parameters.Add(new RequestParameterEntity("user_id_list_2", userIdTwo.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<IsFriendResultEntity, RenRenResponseArg<IsFriendResultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 发布新状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="status"></param>
        public async Task<RenRenResponseArg<SetStatusEntity>> SetStatus(string sessionKey, string userSecretKey, string status)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.SetStatus));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("status", status));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<SetStatusEntity, RenRenResponseArg<SetStatusEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 转发状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="status"></param>
        /// <param name="statusId"></param>
        /// <param name="ownerId"></param>
        public async Task<RenRenResponseArg<ForwardStatusEntity>> ForwardStatus(string sessionKey, string userSecretKey, string status, long statusId, int ownerId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.ForwardStatus));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("status", status));
            parameters.Add(new RequestParameterEntity("forward_doing_id", statusId.ToString()));
            parameters.Add(new RequestParameterEntity("forward_owner_id", ownerId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<ForwardStatusEntity, RenRenResponseArg<ForwardStatusEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 发布新日志
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<PostBlogEntity>> PostBlog(string sessionKey, string userSecretKey, string title, string content)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostBlog));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("title", title));
            parameters.Add(new RequestParameterEntity("content", content));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<PostBlogEntity, RenRenResponseArg<PostBlogEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 评论日志
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="blogId"></param>
        /// <param name="content"></param>
        /// <param name="ownerUserId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostBlogComment(string sessionKey, string userSecretKey, long blogId, string content, int ownerUserId)
        {
            return await PostBlogComment(sessionKey, userSecretKey, blogId, content, ownerUserId, -1, -1);
        }
        /// <summary>
        /// 评论日志（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="blogId"></param>
        /// <param name="content"></param>
        /// <param name="ownerUserId"></param>
        /// <param name="replayId"></param>
        /// <param name="replayType"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostBlogComment(string sessionKey, string userSecretKey, long blogId, string content, int ownerUserId, int replayId, int replayType)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostBlogComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("id", blogId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));
            parameters.Add(new RequestParameterEntity("user_id", ownerUserId.ToString()));
            if (replayId != -1)
                parameters.Add(new RequestParameterEntity("rid", replayId.ToString()));
            if (replayType != -1)
                parameters.Add(new RequestParameterEntity("type", replayType.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 发表相册评论
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="albumId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostAlbumComment(string sessionKey, string userSecretKey, int onwerId, long albumId, string content)
        {
            return await PostAlbumComment(sessionKey, userSecretKey, onwerId, albumId, content, -1, -1);
        }
        /// <summary>
        /// 发表相册评论（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="albumId"></param>
        /// <param name="content"></param>
        /// <param name="replayUserId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostAlbumComment(string sessionKey, string userSecretKey, int onwerId, long albumId, string content, int replayUserId, int isWhisper)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostPhotoComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uid", onwerId.ToString()));
            parameters.Add(new RequestParameterEntity("aid", albumId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));
            if (replayUserId != -1)
                parameters.Add(new RequestParameterEntity("rid", replayUserId.ToString()));
            if (isWhisper != -1)
                parameters.Add(new RequestParameterEntity("whisper", isWhisper.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 发表照片评论
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="photoId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostPhotoComment(string sessionKey, string userSecretKey, int onwerId, long photoId, string content)
        {
            return await PostPhotoComment(sessionKey, userSecretKey, onwerId, photoId, content, -1, -1);
        }
        /// <summary>
        /// 发表照片评论（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="photoId"></param>
        /// <param name="content"></param>
        /// <param name="replayUserId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostPhotoComment(string sessionKey, string userSecretKey, int onwerId, long photoId, string content, int replayUserId, int isWhisper)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostPhotoComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uid", onwerId.ToString()));
            parameters.Add(new RequestParameterEntity("pid", photoId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));
            if (replayUserId != -1)
                parameters.Add(new RequestParameterEntity("rid", replayUserId.ToString()));
            if (isWhisper != -1)
                parameters.Add(new RequestParameterEntity("whisper", isWhisper.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 评论状态
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="content"></param>
        /// <param name="statusId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostStatusComment(string sessionKey, string userSecretKey, int onwerId, string content, long statusId)
        {
            return await PostStatusComment(sessionKey, userSecretKey, onwerId, content, statusId, -1);
        }
        /// <summary>
        /// 评论状态（可扩展）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="onwerId"></param>
        /// <param name="content"></param>
        /// <param name="statusId"></param>
        /// <param name="replayUserId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostStatusComment(string sessionKey, string userSecretKey, int onwerId, string content, long statusId, int replayUserId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostStatusComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("owner_id", onwerId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));
            parameters.Add(new RequestParameterEntity("status_id", statusId.ToString()));
            if (replayUserId != -1)
                parameters.Add(new RequestParameterEntity("rid", replayUserId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 上传图片至默认相册 未添加描述信息、相册等操作
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="basedata"></param>
        public async Task<RenRenResponseArg<UploadPhotoEntity>> UploadPhoto(string sessionKey, string userSecretKey, string basedata)
        {
            return await UploadPhoto(sessionKey, userSecretKey, basedata, -1, null);
        }
        /// <summary>
        /// 发送图片
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="basedata"></param>
        public async Task<RenRenResponseArg<UploadPhotoEntity>> UploadPhoto(string sessionKey, string userSecretKey, string basedata, long albumId, string caption)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostPhoto));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("base_data", basedata));
            if (albumId != -1)
                parameters.Add(new RequestParameterEntity("aid", albumId.ToString()));
            if (caption != null)
                parameters.Add(new RequestParameterEntity("caption", caption));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<UploadPhotoEntity, RenRenResponseArg<UploadPhotoEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 给指定好友留言
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostGossip(string sessionKey, string userSecretKey, int userId, string content)
        {
            return await PostGossip(sessionKey, userSecretKey, userId, content, -1, -1);
        }
        /// <summary>
        /// 给指定好友留言[悄悄话]
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostGossipByWhisper(string sessionKey, string userSecretKey, int userId, string content, int isWhisper)
        {
            return await PostGossip(sessionKey, userSecretKey, userId, content, -1, isWhisper);
        }
        /// <summary>
        /// 在指定留言板回复指定好友留言
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        /// <param name="reUserId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostGossip(string sessionKey, string userSecretKey, int userId, string content, int reUserId)
        {
            return await PostGossip(sessionKey, userSecretKey, userId, content, reUserId, -1);
        }
        /// <summary>
        /// 在指定留言板回复指定好友留言
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        /// <param name="reUserId"></param>
        /// <param name="isWhisper">输入值为1表示悄悄话</param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostGossip(string sessionKey, string userSecretKey, int userId, string content, int reUserId, int isWhisper)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostGossip));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("userId", userId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));
            if (reUserId != -1)
                parameters.Add(new RequestParameterEntity("reUserId", reUserId.ToString()));
            if (isWhisper != -1)
                parameters.Add(new RequestParameterEntity("isWhisper", isWhisper.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
        /// <summary>
        /// 好友申请
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> RequestFriend(string sessionKey, string userSecretKey, int userId, string content)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RequestFriend));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uid", userId.ToString()));
            if (content != null)
                parameters.Add(new RequestParameterEntity("content", content));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 发布分享（针对链接）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="sourceType"></param>
        /// <param name="url"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostShare(string sessionKey, string userSecretKey, int sourceType, string url)
        {
            return await PostShare(sessionKey, userSecretKey, sourceType, -1, -1, url, null);
        }
        /// <summary>
        /// 发布分享
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="sourceType"></param>
        /// <param name="id"></param>
        /// <param name="ownerId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostShare(string sessionKey, string userSecretKey, int sourceType, long id, int ownerId)
        {
            return await PostShare(sessionKey, userSecretKey, sourceType, id, ownerId, null, null);
        }
        /// <summary>
        /// 发布分享(可扩展)
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="sourceType"></param>
        /// <param name="id"></param>
        /// <param name="ownerId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostShare(string sessionKey, string userSecretKey, int sourceType, long id, int ownerId, string url, string comment)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostShare));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("source_type", sourceType.ToString()));
            //parameters.Add(new RequestParameterEntity("type", "0"));//表示分享 默认为0
            if (id != -1)
                parameters.Add(new RequestParameterEntity("id", id.ToString()));
            if (ownerId != -1)
                parameters.Add(new RequestParameterEntity("uid", ownerId.ToString()));
            if (url != null)
                parameters.Add(new RequestParameterEntity("url", url));
            if (!string.IsNullOrEmpty(comment))
                parameters.Add(new RequestParameterEntity("comment", comment));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<CommentReultEntity>> PublishLink(string sessionKey, string userSecretKey, string url, string thumbUrl, string title, string description, int fromId, string comment)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PublishLink));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("url", url));

            if (!string.IsNullOrEmpty(thumbUrl))
                parameters.Add(new RequestParameterEntity("thumb_url", thumbUrl));
            if (!string.IsNullOrEmpty(title))
                parameters.Add(new RequestParameterEntity("title", title));
            if (!string.IsNullOrEmpty(description))
                parameters.Add(new RequestParameterEntity("desc", description));
            if (fromId != -1)
                parameters.Add(new RequestParameterEntity("from", fromId.ToString()));
            if (!string.IsNullOrEmpty(comment))
                parameters.Add(new RequestParameterEntity("comment", comment));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        ///发布收藏 （可在PostShare里扩展这里为了不影响之前的逻辑先这么写会有冗余代码暂时这样吧）
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="sourceType"></param>
        /// <param name="id"></param>
        /// <param name="ownerId"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostCollect(string sessionKey, string userSecretKey, int sourceType, long id, int ownerId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostShare));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("source_type", sourceType.ToString()));
            parameters.Add(new RequestParameterEntity("type", "1"));

            //parameters.Add(new RequestParameterEntity("type", "0"));//表示分享 默认为0
            if (id != -1)
                parameters.Add(new RequestParameterEntity("id", id.ToString()));
            if (ownerId != -1)
                parameters.Add(new RequestParameterEntity("uid", ownerId.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 评论分享
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="shareId"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostShareComment(string sessionKey, string userSecretKey, long shareId, int userId, string content)
        {
            return await PostShareComment(sessionKey, userSecretKey, shareId, userId, content, -1);
        }
        /// <summary>
        /// 评论分享 可扩展
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="shareId"></param>
        /// <param name="userId"></param>
        /// <param name="content"></param>
        /// <param name="reUserid"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> PostShareComment(string sessionKey, string userSecretKey, long shareId, int userId, string content, int reUserid)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.PostShareComment));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("id", shareId.ToString()));
            parameters.Add(new RequestParameterEntity("user_id", userId.ToString()));
            parameters.Add(new RequestParameterEntity("content", content));

            if (reUserid != -1)
                parameters.Add(new RequestParameterEntity("rid", reUserid.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 向人人服务器注册推送
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="uri"></param>
        public async Task<RenRenResponseArg<CommentReultEntity>> AddNotification(string sessionKey, string userSecretKey, string uri)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.AddNotification));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uri", uri));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        /// <summary>
        /// 获取客户端的升级信息
        /// </summary>
        /// <param name="sessionKey"></param>
        /// <param name="userSecretKey"></param>
        /// <param name="phoneModel">手机型号</param>
        /// <param name="phoneOS">手机操作系统</param>
        /// <param name="pubDate">当前客户端的发布日期（打包时间 格式：20090909）</param>
        public async Task<RenRenResponseArg<ClientUpdateInfoEntity>> GetClientUpdateInfo(string sessionKey, string userSecretKey, string phoneModel, string phoneOS, int pubDate, int up)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetUpldateInfo));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("session_key", sessionKey));
            parameters.Add(new RequestParameterEntity("name", "0"));
            //parameters.Add(new RequestParameterEntity("property", "8"));
            parameters.Add(new RequestParameterEntity("property", "10"));
            parameters.Add(new RequestParameterEntity("subproperty", "0"));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));
            parameters.Add(new RequestParameterEntity("channelId", "9100301"));
            parameters.Add(new RequestParameterEntity("ua", phoneModel));
            parameters.Add(new RequestParameterEntity("os", phoneOS));
            parameters.Add(new RequestParameterEntity("pubdate", pubDate.ToString()));
            parameters.Add(new RequestParameterEntity("up", up.ToString()));

            parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, userSecretKey)));

            var result = await agentReponseHandler<ClientUpdateInfoEntity, RenRenResponseArg<ClientUpdateInfoEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        #endregion

        #region Response handler
        static private async Task<ArgType> agentReponseHandler<EntityType, ArgType>(ICollection<RequestParameterEntity> args, string method)
        {
            EntityType entity = default(EntityType);
            ErrorEntity error = null;
            ArgType result = default(ArgType);
            try
            {
                var agent = new HttpWebRequestAgent();
                agent.Method = method;

                foreach (var parameter in args)
                {
                    agent.AddParameters(parameter.Name, Uri.EscapeUriString(parameter.Values));
                }

                string response = await agent.DownloadString(ConstantValue.RequestUri);
                Debug.WriteLine(response);
                entity = (EntityType)JsonUtility.DeserializeObj(new MemoryStream(Encoding.UTF8.GetBytes(response)),
                                                                         typeof(EntityType));
                error = (ErrorEntity)JsonUtility.DeserializeObj(new MemoryStream(Encoding.UTF8.GetBytes(response)),
                                                                         typeof(ErrorEntity));

                if (error != null && error.Error_msg != null)
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), error);
                }
                else if (entity != null)
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), entity);
                }
                else
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), new ArgumentException());
                }
            }
            catch (Exception ex)
            {
                result = (ArgType)Activator.CreateInstance(typeof(ArgType), ex);
            }

            return result;
        }

        static private async Task<ArgType> agentReponseHandler<EntityType, ArgType>(ICollection<RequestParameterEntity> args, string method, Uri requestUrl)
        {
            EntityType entity = default(EntityType);
            ErrorEntity error = null;
            ArgType result = default(ArgType);
            try
            {
                var agent = new HttpWebRequestAgent();
                agent.Method = method;

                foreach (var parameter in args)
                {
                    agent.AddParameters(parameter.Name, Uri.EscapeUriString(parameter.Values));
                }

                string response = await agent.DownloadString(requestUrl);
                Debug.WriteLine(response);
                entity = (EntityType)JsonUtility.DeserializeObj(new MemoryStream(Encoding.UTF8.GetBytes(response)),
                                                                         typeof(EntityType));
                error = (ErrorEntity)JsonUtility.DeserializeObj(new MemoryStream(Encoding.UTF8.GetBytes(response)),
                                                                         typeof(ErrorEntity));

                if (error != null && error.Error_msg != null)
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), error);
                }
                else if (entity != null)
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), entity);
                }
                else
                {
                    result = (ArgType)Activator.CreateInstance(typeof(ArgType), new ArgumentException());
                }
            }
            catch (Exception ex)
            {
                result = (ArgType)Activator.CreateInstance(typeof(ArgType), ex);
            }

            return result;
        }

        #endregion

        #region 电台接口

        /// <summary>
        /// 获取歌词码
        /// </summary>
        public async Task<string> GetLyricsCode(string artist, string title, string flag, Uri requestUrl)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("Artist", artist));
            parameters.Add(new RequestParameterEntity("Title", title));
            parameters.Add(new RequestParameterEntity("Flag", flag));
            parameters.Add(new RequestParameterEntity("v", "1.0"));

            var result = await DownloadString(parameters, requestUrl);
            return result;
        }

        /// <summary>
        /// 获取歌词数据
        /// </summary>
        public async Task<string> GetLyricsData(string id, string code, Uri requestUrl)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("Id", id));
            parameters.Add(new RequestParameterEntity("Code", code));

            var result = await DownloadString(parameters, requestUrl);
            return result;
        }


        /// <summary>
        /// 直接下载文本
        /// </summary>
        /// <param name="args">参数</param>
        /// <param name="requestUrl">需要下载的url</param>
        /// <returns>返回的数据</returns>
        static private async Task<string> DownloadString(ICollection<RequestParameterEntity> args, Uri requestUrl)
        {
            string response;
            try
            {
                var agent = new HttpWebRequestAgent();
                agent.Method = "GET";
                foreach (var parameter in args)
                {
                    agent.AddParameters(parameter.Name, Uri.EscapeUriString(parameter.Values));
                }
                response = await agent.DownloadString(requestUrl);
                Debug.WriteLine(response);

            }
            catch (Exception ex)
            {
                response = string.Empty;
            }

            return response;
        }

        /// <summary>
        /// 直接下载文本
        /// </summary>
        /// <param name="requestUrl">需要下载的url</param>
        /// <returns>返回的数据</returns>
        static private async Task<string> DownloadString(Uri requestUrl)
        {
            string response;
            try
            {
                var agent = new HttpWebRequestAgent();
                agent.Method = "GET";
                response = await agent.DownloadString(requestUrl);
                Debug.WriteLine(response);

            }
            catch (Exception ex)
            {
                response = string.Empty;
            }

            return response;
        }
        #endregion
    }
}
