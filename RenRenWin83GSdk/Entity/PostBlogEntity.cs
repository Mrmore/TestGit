using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class PostBlogEntity : PropertyChangedBase
    {
        /// <summary>
        /// 日志id
        /// </summary>
        [DataMember]
        //public long id { get; set; }

        private long id;
        public long Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                this.NotifyPropertyChanged(postBlogEntity => postBlogEntity.Id);
            }
        }
    }
}
