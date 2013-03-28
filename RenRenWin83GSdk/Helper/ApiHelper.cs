using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using RenRenAPI.Entity;
using Windows.Security.Cryptography.Core;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace RenRenAPI.Helper
{
    public class ApiHelper
    {
        public static string GenerateSig(List<RequestParameterEntity> Parameters, string key)
        {
            StringBuilder sb = new StringBuilder();

            Parameters.Sort(new ParameterComparer());
            foreach (var requestParameter in Parameters)
            {
                sb.Append(string.Format("{0}={1}", requestParameter.Name, requestParameter.Values.Length < 50 ? requestParameter.Values : requestParameter.Values.Substring(0, 50)));
            }
            sb.Append(key);

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return MD5Core.GetHashString(bytes);
        }


        public static string ComputeMD5(string str)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(str, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        public static string GenerateTime()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmssfff");
        }
    }
}
