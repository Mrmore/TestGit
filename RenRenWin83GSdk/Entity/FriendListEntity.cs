using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class FriendListEntity : PropertyChangedBase
    {
        /// <summary>
        /// 用户的全部好友数量
        /// </summary>
        [DataMember]
        //public int count { get; set; }

        private int count;
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
                this.NotifyPropertyChanged(friendListEntity => friendListEntity.Count);
            }
        }

        /// <summary>
        /// 表示搜索匹配结果的具体内容
        /// </summary>
        [DataMember]
        //public List<FriendEntity> friend_list { get; set; }

        private ObservableCollection<FriendEntity> friend_list = new ObservableCollection<FriendEntity>();
        public ObservableCollection<FriendEntity> Friend_list
        {
            get
            {
                return friend_list;
            }
            set
            {
                friend_list = value;
                this.NotifyPropertyChanged(friendListEntity => friendListEntity.Friend_list);
            }
        }
    }
}
