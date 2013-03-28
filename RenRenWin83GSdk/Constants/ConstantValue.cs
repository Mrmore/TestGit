using System;
using System.Net;

namespace RenRenAPI.Constants
{
    public class ConstantValue
    {
        //public static string ApiKey = "0764158feb7d4465b60a8341199ddd85";

        //public static string SecretKey = "5d073ac743594670a101a955a4db0ff7";

        public static string ApiKey = "87860fa63de54523956d4f592ae2fc43";

        public static string SecretKey = "2a48dbf434bf4a6d85fc44138d365116";

        public static Uri RequestUri = new Uri("http://api.m.renren.com/api", UriKind.Absolute);

        public static Uri LoginUri = new Uri("https://login.renren.com/mlogin/auth/token", UriKind.Absolute);

        public static Uri SpecificRequestUri = new Uri("http://m.apis.tk/api", UriKind.Absolute);

        public static Uri ShareSongRequestUri = new Uri("http://api.m.renren.com/api/radio/shareSong", UriKind.Absolute);

        public static string PostMethod = "POST";
    }
}
