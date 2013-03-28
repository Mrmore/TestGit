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

namespace RenRenAPI
{
    public partial class API
    {
        public async Task<RenRenResponseArg<GetHomeEntity>> RadioGetHome(string sessionKey, string secretKey, string access_token, string uuid, string quality, string resolution)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RadioGetHome));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("quality", quality));
            parameters.Add(new RequestParameterEntity("resolution", resolution));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<GetHomeEntity, RenRenResponseArg<GetHomeEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<SongListEntity>> GetRadio(string sessionKey, string secretKey, string access_token, string uuid, uint radioId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetRadio));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("radioId", radioId.ToString()));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<SongListEntity, RenRenResponseArg<SongListEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<SongEntity>> GetNextSong(string sessionKey, string secretKey, string access_token, string uuid, uint radioId, uint songId, uint duration)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.GetNextSong));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("radioId", radioId.ToString()));
            parameters.Add(new RequestParameterEntity("songId", songId.ToString()));
            parameters.Add(new RequestParameterEntity("duration", duration.ToString()));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<SongEntity, RenRenResponseArg<SongEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<CommentReultEntity>> RadioAddFavorite(string sessionKey, string secretKey, string access_token, string uuid, uint songId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RadioAddFavorite));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("songId", songId.ToString()));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<CommentReultEntity>> RadioRemoveFavorite(string sessionKey, string secretKey, string access_token, string uuid, uint songId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RadioRemoveFavorite));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("songId", songId.ToString()));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<CommentReultEntity>> RadioShareSong(string sessionKey, string secretKey, string access_token, string uuid, uint songId, string comment)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("client_id", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RadioShareSong));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            //parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("songId", songId.ToString()));
            parameters.Add(new RequestParameterEntity("comment", comment));
            //parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<CommentReultEntity, RenRenResponseArg<CommentReultEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }

        public async Task<RenRenResponseArg<SongEntity>> RadioDeleteSong(string sessionKey, string secretKey, string access_token, string uuid, uint songId, uint radioId)
        {
            List<RequestParameterEntity> parameters = new List<RequestParameterEntity>();
            parameters.Add(new RequestParameterEntity("api_key", ConstantValue.ApiKey));
            parameters.Add(new RequestParameterEntity("method", Method.RadioDeleteSong));
            parameters.Add(new RequestParameterEntity("call_id", ApiHelper.GenerateTime()));
            parameters.Add(new RequestParameterEntity("v", "1.0"));
            parameters.Add(new RequestParameterEntity("uuid", uuid));
            parameters.Add(new RequestParameterEntity("songId", songId.ToString()));
            parameters.Add(new RequestParameterEntity("radioId", radioId.ToString()));
            parameters.Add(new RequestParameterEntity("version", "1.0.1"));

            if (!string.IsNullOrEmpty(access_token))
                parameters.Add(new RequestParameterEntity("access_token", access_token));

            if (!string.IsNullOrEmpty(sessionKey))
                parameters.Add(new RequestParameterEntity("session_key", sessionKey));

            if (string.IsNullOrEmpty(secretKey))
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, ConstantValue.SecretKey)));
            else
                parameters.Add(new RequestParameterEntity("sig", ApiHelper.GenerateSig(parameters, secretKey)));

            var result = await agentReponseHandler<SongEntity, RenRenResponseArg<SongEntity>>(parameters, ConstantValue.PostMethod);
            return result;
        }
    }
}
