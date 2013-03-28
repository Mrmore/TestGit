using RenRenAPI.Entity;
using RenRenWin8Radio.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenrenWin8RadioUI.DataModel
{
    public class RadioItem : PropertyChangedBase
    {
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

        private string albumImg;
        public string AlbumImg
        {
            get
            {
                return albumImg;
            }
            set
            {
                albumImg = value;
                this.NotifyPropertyChanged(entity => entity.AlbumImg);
            }
        }

        private string albumCd;
        public string AlbumCD
        {
            get
            {
                return albumCd;
            }
            set
            {
                albumCd = value;
                this.NotifyPropertyChanged(entity => entity.AlbumCD);
            }
        }

        private ObservableCollection<SongEntity> songs = null;
        public ObservableCollection<SongEntity> Songs
        {
            get
            {
                return songs;
            }
            set
            {
                songs = value;
                this.NotifyPropertyChanged(entity => entity.Songs);
            }
        }

        private bool isCheck = false;
        public bool IsCheck
        {
            get
            {
                return isCheck;
            }
            set
            {
                isCheck = value;
                this.NotifyPropertyChanged(entity => entity.IsCheck);
            }
        }
    }
}
