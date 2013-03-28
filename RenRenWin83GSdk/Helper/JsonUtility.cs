using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;

namespace RenRenAPI.Helper
{
    public class JsonUtility
    {
        public static object DeserializeObj(Stream inputStream, Type objType)
        {
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(objType);
            object result = serializer.ReadObject(inputStream);
            return result;
        }
    }
}
