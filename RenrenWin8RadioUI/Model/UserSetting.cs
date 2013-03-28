using RenRenWin8Radio.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace RenRenWin8Radio.Model
{
    [DataContract]
    public class UserSetting : PropertyChangedBase
    {
        [DataMember]
        private uint radioId;
        public uint RadioId
        {
            get
            {
                return radioId;
            }
            set
            {
                radioId = value;
                this.NotifyPropertyChanged(userSetting => userSetting.RadioId);
            }
        }

        [DataMember]
        private double volume = 1.0;
        public double Volume
        {
            get
            {
                return volume;
            }
            set
            {
                volume = value;
                this.NotifyPropertyChanged(userSetting => userSetting.Volume);
            }
        }

        [DataMember]
        private bool gravity = false;
        public bool Gravity
        {
            get
            {
                return gravity;
            }
            set
            {
                gravity = value;
                this.NotifyPropertyChanged(userSetting => userSetting.Gravity);
            }
        }

        [DataMember]
        private bool sensitive = false;
        public bool Sensitive
        {
            get
            {
                return sensitive;
            }
            set
            {
                sensitive = value;
                this.NotifyPropertyChanged(userSetting => userSetting.Sensitive);
            }
        }
    }
}
