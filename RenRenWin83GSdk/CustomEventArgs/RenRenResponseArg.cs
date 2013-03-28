using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenRenAPI.Entity;
using RenRenAPI.Model;

namespace RenRenWin83GSdk.CustomEventArgs
{
    /// <summary>
    /// The wrapper for RenRen service response
    /// It is in charge of recording three things:
    /// 1, the overall response entity data if this request is passed
    /// 2, the local exception if it happened in local
    /// 3, the remote error if it return by server
    /// </summary>
    /// <typeparam name="EntityType">the entity type</typeparam>
    public class RenRenResponseArg<EntityType> where EntityType : PropertyChangedBase, INotifyPropertyChanged, new()
    {
        #region Properties

        /// <summary>
        /// The overall response entity data
        /// </summary>
        public EntityType Result { get; private set; }

        /// <summary>
        /// The local exception recodeing
        /// </summary>
        public Exception LocalError { get; private set; }

        /// <summary>
        /// The remote error recording
        /// </summary>
        public ErrorEntity RemoteError { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Construct using entity data
        /// </summary>
        /// <param name="result">entity data</param>
        public RenRenResponseArg(EntityType result)
        {
            Result = result;
        }

        /// <summary>
        /// Construct using local exception
        /// </summary>
        /// <param name="ex">local exception</param>
        public RenRenResponseArg(Exception ex)
        {
            LocalError = ex;
        }

        /// <summary>
        /// Construct using remote error
        /// </summary>
        /// <param name="error">remote error</param>
        public RenRenResponseArg(ErrorEntity error)
        {
            RemoteError = error;
        }

        #endregion
    }
}
