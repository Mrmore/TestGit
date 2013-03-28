using RenRenAPI.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RenRenWin83GSdk.Entity
{
    [DataContract]
    public class LoginEntity : PropertyChangedBase
    {
        [DataMember]
        public string access_token;
        [DataMember]
        public string expires_in;
        [DataMember]
        public string scope;
    }
}
