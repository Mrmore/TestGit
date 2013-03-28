using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class ErrorEntity : PropertyChangedBase
    {
        [DataMember]
        //public int error_code { get; set; }

        private int error_code;
        public int Error_code
        {
            get
            {
                return error_code;
            }
            set
            {
                error_code = value;
                this.NotifyPropertyChanged(errorEntity => errorEntity.Error_code);
            }
        }

        [DataMember]
        //public string error_msg { get; set; }

        private string error_msg;
        public string Error_msg
        {
            get
            {
                return error_msg;
            }
            set
            {
                error_msg = value;
                this.NotifyPropertyChanged(errorEntity => errorEntity.Error_msg);
            }
        }
    }
}
