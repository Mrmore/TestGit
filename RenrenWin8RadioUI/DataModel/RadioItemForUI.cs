using RenRenWin8Radio.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenrenWin8RadioUI.DataModel
{
    public class RadioItemForUI : PropertyChangedBase
    {
        private ObservableCollection<RadioItem> item = new ObservableCollection<RadioItem>();
        public ObservableCollection<RadioItem> Item
        {
            get
            {
                return item;
            }
            set
            {
                item = value;
                this.NotifyPropertyChanged(entity => entity.Item);
            }
        }
    }
}
