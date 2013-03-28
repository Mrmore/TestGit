using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI;
using RenRenAPI.Entity;
using RenRenAPI.Model;
using RenRenWin83GSdk.CustomEventArgs;

namespace RenRenWin8Radio.ViewModel
{
    abstract class ViewModelVisistor<IdType, EntityType> where EntityType : PropertyChangedBase, INotifyPropertyChanged, new()
    {
        // Simple visit interface
        public abstract void Visit(ViewModelBase<IdType, EntityType> viewModelBase);
        // Advanced visit interface
        public abstract object Visit(ViewModelBase<IdType, EntityType> viewModelBase, params object[] args);
    }

    // Calculate the total shared friends, this is a example
    class SharedFriendsVisitor : ViewModelVisistor<int, FriendListEntity>
    {
        public override void Visit(ViewModelBase<int, FriendListEntity> viewModelBase)
        {
            throw new NotImplementedException();
        }

        public override object Visit(ViewModelBase<int, FriendListEntity> viewModelBase, params object[] args)
        {
            if (args.Length < 2) throw new ArgumentException(); 
            if (viewModelBase is FriendsViewModel)
            {
                FriendListEntity result = new FriendListEntity();
                for (int i = 1; i < args.Length; i++)
                {
                }
                return result;
            }
            else
            {
                return 0;
            }
        }
    }
}
