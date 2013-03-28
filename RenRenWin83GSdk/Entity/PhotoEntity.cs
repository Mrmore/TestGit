using System;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class PhotoEntity : PropertyChangedBase
    {
        /// <summary>
        /// 照片id
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
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Id);
            }
        }

        /// <summary>
        /// 相册id
        /// </summary>
        [DataMember]
        //public long album_id { get; set; }

        private long album_id;
        public long Album_id
        {
            get
            {
                return album_id;
            }
            set
            {
                album_id = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Album_id);
            }
        }

        /// <summary>
        /// 所有者id
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
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.User_id);
            }
        }

        /// <summary>
        /// 照片小图片
        /// </summary>
        [DataMember]
        //public string img_head { get; set; }

        private string img_head;
        public string Img_head
        {
            get
            {
                return img_head;
            }
            set
            {
                img_head = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Img_head);
            }
        }

        /// <summary>
        /// 照片大图片
        /// </summary>
        [DataMember]
        //public string img_large { get; set; }

        private string img_large;
        public string Img_large
        {
            get
            {
                return img_large;
            }
            set
            {
                img_large = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Img_large);
            }
        }

        /// <summary>
        /// 照片描述
        /// </summary>
        [DataMember]
        //public string caption { get; set; }

        private string caption;
        public string Caption
        {
            get
            {
                return caption;
            }
            set
            {
                caption = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Caption);
            }
        }

        /// <summary>
        /// 照片时间
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
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Time);
            }
        }

        /// <summary>
        /// 浏览次数
        /// </summary>
        [DataMember]
        //public int view_count { get; set; }

        private int view_count;
        public int View_count
        {
            get
            {
                return view_count;
            }
            set
            {
                view_count = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.View_count);
            }
        }

        /// <summary>
        /// 评论次数
        /// </summary>
        [DataMember]
        //public int comment_count { get; set; }

        private int comment_count;
        public int Comment_count
        {
            get
            {
                return comment_count;
            }
            set
            {
                comment_count = value;
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.Comment_count);
            }
        }

        /// <summary>
        /// 所有者名称
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
                this.NotifyPropertyChanged(PhotoEntity => PhotoEntity.User_name);
            }
        }
    }
}
