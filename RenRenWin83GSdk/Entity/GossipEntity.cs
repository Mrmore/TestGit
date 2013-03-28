using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class GossipEntity : PropertyChangedBase
    {
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
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Id);
            }
        }

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
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.User_id);
            }
        }

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
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.User_name);
            }
        }

        [DataMember]
        //public string head_url { get; set; }

        private string head_url;
        public string Head_url
        {
            get
            {
                return head_url;
            }
            set
            {
                head_url = value;
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.head_url);
            }
        }

        [DataMember]
        //public string content { get; set; }

        private string content;
        public string Content
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Content);
            }
        }

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
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Time);
            }
        }

        [DataMember]
        //public int whisper { get; set; }

        private int whisper;
        public int Whisper
        {
            get
            {
                return whisper;
            }
            set
            {
                whisper = value;
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Whisper);
            }
        }

        [DataMember]
        //public int source { get; set; }

        private int source;
        public int Source
        {
            get
            {
                return source;
            }
            set
            {
                source = value;
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Source);
            }
        }

        [DataMember]
        //public string large_url { get; set; }

        private string large_url;
        public string Large_url
        {
            get
            {
                return large_url;
            }
            set
            {
                large_url = value;
                this.NotifyPropertyChanged(gossipEntity => gossipEntity.Large_url);
            }
        }
    }
}
