using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class VisitorUserEntity : PropertyChangedBase
    {
        /// <summary>
        /// 用户id
        /// </summary>
        [DataMember]
        //public int user_id { get; set; }

        private int user_id;
        public int User_id
        {
            get
            {
                return user_id;
            }
            set
            {
                user_id = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.User_id);
            }
        }

        /// <summary>
        /// 用户姓名
        /// </summary>
        [DataMember]
        //public string user_name { get; set; }

        private string user_name;
        public string User_name
        {
            get
            {
                return user_name;
            }
            set
            {
                user_name = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.User_name);
            }
        }

        /// <summary>
        /// 用户头像
        /// </summary>
        [DataMember]
        //public string user_head { get; set; }

        private string user_head;
        public string User_head
        {
            get
            {
                return user_head;
            }
            set
            {
                user_head = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.User_head);
            }
        }

        /// <summary>
        /// 来访时间
        /// </summary>
        [DataMember]
        //public long time { get; set; }

        private long time;
        public long Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.Time);
            }
        }

        /// <summary>
        /// ？
        /// </summary>
        [DataMember]
        //public int is_online { get; set; }

        private int is_online;
        public int Is_online
        {
            get
            {
                return is_online;
            }
            set
            {
                is_online = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.Is_online);
            }
        }

        /// <summary>
        /// ？
        /// </summary>
        [DataMember]
        //public int gender { get; set; }

        private int gender;
        public int Gender
        {
            get
            {
                return gender;
            }
            set
            {
                gender = value;
                this.NotifyPropertyChanged(visitorUserEntity => visitorUserEntity.Gender);
            }
        }
    }
}
