using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI.Model;
using RenRenWin83GSdk.CustomEventArgs;

namespace RenRenWin8Radio.ViewModel
{
    /// <summary>
    /// Generic View Model Base class based on id
    /// In charge of containing the common data using flyweighter and interfaces of all view model.
    /// We can put some generic interface or common data here,
    /// Since the renren serice apis have lots of different inputs, so in the init version,
    /// we just put the common data and some request interfaces in the base class, and later on, we can abstract 
    /// the more common interface in it.
    /// \note: id in this class means some kind of key linked to data model, for example:
    /// in feedlist view model id means user id;
    /// in nearby place view model id means the poi id
    /// </summary>
    /// <typeparam name="IdType">The id type</typeparam>
    /// <typeparam name="EntityType">The entity type</typeparam>
    abstract class ViewModelBase<IdType, EntityType> where EntityType : PropertyChangedBase, INotifyPropertyChanged, new()
    {
        protected ViewModelFlyweight<IdType, EntityType> _flyweighter = new ViewModelFlyweight<IdType, EntityType>();

        /// <summary>
        /// The self Data model
        /// </summary>
        public EntityType Model { get; set; }

        /// <summary>
        /// Flag, getter and setter the cache switcher
        /// </summary>
        public bool NeedCache { get; set; }

        /// <summary>
        /// The Data model of view by id
        /// </summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public EntityType this[IdType uid]
        {
            get
            {
                return _flyweighter.Entity(uid);
            }
            private set
            {
                _flyweighter.Add(uid, value);
            }
        }

        /// <summary>
        /// Self request and update
        /// \note: this is a async await interface
        /// </summary>
        /// <param name="args">the input arguments</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<EntityType>> Request(params object[] args)
        {
            RenRenResponseArg<EntityType> resp = await DoRequest(args);

            if (resp.LocalError == null && resp.RemoteError == null && resp.Result != null)
            {
                Model = resp.Result;
            }
            return resp;
        }

        /// <summary>
        /// Request and update by id
        /// \note: this is a async await interface
        /// </summary>
        /// <param name="args">the input arguments</param>
        /// <returns></returns>
        public async Task<RenRenResponseArg<EntityType>> RequestById(IdType id, params object[] args)
        {
            RenRenResponseArg<EntityType> resp = await DoRequestById(id, args);

            if (resp.LocalError == null && resp.RemoteError == null && resp.Result != null)
            {
                this[id] = resp.Result;
            }
            return resp;
        }

        /// <summary>
        /// Reset total view model instance
        /// </summary>
        public void Reset()
        {
            _flyweighter.Reset();
            Model = null;
            DoReset();
        }

        public void ResetById(IdType id)
        {
            _flyweighter.ResetById(id);
            DoResetById(id);
        }

        // Extention interface reserved
        public object Accecpt(ViewModelVisistor<IdType, EntityType> visitor, params object[] args)
        {
            return visitor.Visit(this, args);
        }

        protected abstract Task<RenRenResponseArg<EntityType>> DoRequest(params object[] args);
        protected abstract Task<RenRenResponseArg<EntityType>> DoRequestById(IdType id, params object[] args);
        protected abstract void DoReset();
        protected abstract void DoResetById(IdType id);
    }
}
