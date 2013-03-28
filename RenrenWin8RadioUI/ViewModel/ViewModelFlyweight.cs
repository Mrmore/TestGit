using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI.Model;

namespace RenRenWin8Radio.ViewModel
{
    /// <summary>
    /// View data model storage flyweight 
    /// </summary>
    /// <typeparam name="IdType"></typeparam>
    /// <typeparam name="EntityType"></typeparam>
    class ViewModelFlyweight<IdType, EntityType> where EntityType : PropertyChangedBase, INotifyPropertyChanged, new()
    {
        private IDictionary<IdType, EntityType> _refTable = new Dictionary<IdType, EntityType>();

        public void Add(IdType id, EntityType model)
        {
            if (!_refTable.ContainsKey(id))
            {
                _refTable.Add(id, model);
            }
            else
            {
                _refTable[id] = model;
            }
        }

        public void Reset()
        {
            _refTable.Clear();
        }

        public void ResetById(IdType id)
        {
            if (_refTable.ContainsKey(id))
            {
                _refTable.Remove(id);
            }
        }

        public EntityType Entity(IdType id)
        {
            if (_refTable.ContainsKey(id))
            {
                return _refTable[id];
            }
            else
            {
                return null;
            }
        }

        public bool ContainsId(IdType id)
        {
            return _refTable.ContainsKey(id);
        }

        public EntityType this[IdType id]
        {
            get 
            {
                return Entity(id);
            }
            set
            {
                Add(id, value);
            }
        }
    }
}
