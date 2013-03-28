using System;
using System.Net;
using System.Runtime.Serialization;

namespace RenRenWin8Radio.Model
{
    [DataContract]
    public class UserInfo : PropertyChangedBase
    {
        [DataMember]
        private string username;
        public string UserName
        {
            get
            {
                return username;
            }
            set
            {
                username = value;
                this.NotifyPropertyChanged(userInfo => userInfo.UserName);
                //this.NotifyPropertyChanged("UserName");
            }
        }

        [DataMember]
        private string password;
        public string PassWord
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
                this.NotifyPropertyChanged(userInfo => userInfo.PassWord);
                //this.NotifyPropertyChanged("PassWord");
            }
        }

        /*
        [DataMember]
        private UserSetting userSettingRadio;//; = new UserSetting();
        public UserSetting UserSettingRadio
        {
            get
            {
                return userSettingRadio;
            }
            set
            {
                userSettingRadio = value;
                this.NotifyPropertyChanged(userInfo => userInfo.userSettingRadio);
            }
        }
        */
    }
}
