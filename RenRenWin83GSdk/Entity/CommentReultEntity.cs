using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class CommentReultEntity : PropertyChangedBase
    {
        /// <summary>
        /// 成功添加评论返回值, 值为1
        /// </summary>
        [DataMember]
        //public string result { get; set; }

        private string result;
        public string Result
        {
            get
            {
                return result;
            }
            set
            {
                result = value;
                this.NotifyPropertyChanged(commentReultEntity => commentReultEntity.Result);
            }
        }
    }
}
