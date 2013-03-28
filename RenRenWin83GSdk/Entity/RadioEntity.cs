using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using RenRenAPI.Model;

namespace RenRenAPI.Entity
{
    [DataContract]
    public class RadioEntity : PropertyChangedBase
    {
        [DataMember]
        private uint id;
        public uint Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
                this.NotifyPropertyChanged(entity => entity.Id);
            }
        }

        [DataMember]
        private string name;
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
                this.NotifyPropertyChanged(entity => entity.Name);
            }
        }

        [DataMember]
        private string bg;
        public string Background
        {
            get
            {
                return bg;
            }
            set
            {
                bg = value;
                this.NotifyPropertyChanged(entity => entity.Background);
            }
        }
    }
}
