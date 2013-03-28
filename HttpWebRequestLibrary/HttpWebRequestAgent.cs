using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Diagnostics;

namespace HttpWebRequestLibrary
{
    public interface IRenRenHttpAgent
    {
        void AddParameters(string Name, string Value);
        void RemoveParameters(string Name);
        void ClearParameters();
        Task<string> DownloadString(Uri uri);
    }


    public class HttpWebRequestAgent : IRenRenHttpAgent
    {
        private static HttpClient getHc = null;
        private static HttpClient postHc = null;

        #region Members

        /// <summary>
        /// Post data query string.
        /// </summary>
        Dictionary<string, object> _parameters = new Dictionary<string, object>();

        public string Method;
        #endregion

        #region PublicMethod
        public void AddParameters(string Name, string Value)
        {
            _parameters.Add(Name, Value);
        }

        public void RemoveParameters(string Name)
        {
            if (_parameters.ContainsKey(Name))
            {
                _parameters.Remove(Name);
            }
        }

        public void ClearParameters()
        {
            _parameters.Clear();
        }

        public async Task<string> DownloadString(Uri uri)
        {
            string result = string.Empty;

            if (Method == null)
                throw new ArgumentNullException("Method NULL");
            else if (Method.ToUpper().Trim() == "GET")
            {
                result = await Get(uri);
            }
            else if (Method.ToUpper().Trim() == "POST")
            {
                result = await Post(uri);
            }

            return result;
        }
        #endregion

        #region WebRequest
        private async Task<string> Get(Uri uri)
        {
            string requestUri = uri + "?" + GenerateParameterString();

            if (getHc == null)
            {
                getHc = new HttpClient();
            }

            HttpResponseMessage result = await getHc.GetAsync(new Uri(requestUri));

            return await result.Content.ReadAsStringAsync();
        }

        private async Task<string> Post(Uri uri)
        {
            if (postHc == null)
            {
                postHc = new HttpClient();
            }

            try
            {
                string parameters = GenerateParameterString();
                var content = new StringContent(parameters);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                Debug.WriteLine("post data" + parameters);
                HttpResponseMessage response = await postHc.PostAsync(uri, content);

                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion

        #region PrivateMethod

        private string GenerateParameterString()
        {
            StringBuilder parameters = new StringBuilder();
            foreach (var parameter in _parameters)
            {
                parameters.Append(String.Format("{0}={1}&", parameter.Key, parameter.Value));
            }
            string resutl = parameters.ToString();
            return resutl.Substring(0, resutl.Length - 1);
        }

        #endregion

        public void Dispose()
        {
            if (getHc != null)
            {
                getHc.Dispose();
            }
            if (postHc != null)
            {
                postHc.Dispose();
            }
        }
    }
}
