using System;
using System.Windows.Input;

namespace Microsoft.PlayerFramework
{
    /// <summary>
    /// Provides an ICommand implementation that allows delgates to be used to handle requests to the interface.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        /// <summary>
        /// The action to invoke when the Execute method is called.
        /// </summary>
        protected Action ExecuteMethod { get; set; }

        /// <summary>
        /// The action to invoke when the CanExecute method is called.
        /// </summary>
        protected Func<bool> CanExecuteMethod { get; set; }

        /// <inheritdoc /> 
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="executeMethod">A delegate to handle the Execute method</param>
        /// <param name="canExecuteMethod">A delegate to handle the CanExecute method</param>
        public DelegateCommand(Action executeMethod, Func<bool> canExecuteMethod)
        {
            this.ExecuteMethod = executeMethod;
            this.CanExecuteMethod = canExecuteMethod;
        }
        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// Always returns true for CanExecute
        /// </summary>
        /// <param name="executeMethod">A delegate to handle the Execute method</param>
        public DelegateCommand(Action executeMethod)
        {
            this.ExecuteMethod = executeMethod;
        }

        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// Delegates must be set separately.
        /// </summary>
        protected DelegateCommand()
        { }

        /// <summary>
        /// Indicates whether or not the command can execute without a parameter
        /// </summary>
        /// <returns>boolean indicating whether the command can execute.</returns>
        public virtual bool CanExecute()
        {
            if (CanExecuteMethod == null) return true;
            return CanExecuteMethod();
        }

        /// <summary>
        /// Executes the command without a parameter
        /// </summary>
        public virtual void Execute()
        {
            if (ExecuteMethod != null)
            {
                ExecuteMethod();
            }
        }

        /// <inheritdoc /> 
        public virtual bool CanExecute(object parameter)
        {
            return CanExecute();
        }

        /// <inheritdoc /> 
        public virtual void Execute(object parameter)
        {
            Execute();
        }

        /// <summary>
        /// Invokes the CanExecuteChanged event.
        /// </summary>
        public void OnCanExecuteChanged()
        {
            if (CanExecuteChanged != null) CanExecuteChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invokes the CanExecuteChanged event.
        /// Useful to assign as an event handler.
        /// </summary>
        public void OnCanExecuteChanged(object sender, object eventArgs)
        {
            OnCanExecuteChanged();
        }
    }

    /// <summary>
    /// Provides a strongly typed ICommand implementation that allows delgates to be used to handle requests to the interface.
    /// </summary>
    /// <typeparam name="T">Parameters are strongly typed to this.</typeparam>
    public class DelegateCommand<T> : DelegateCommand
    {
        /// <summary>
        /// The strongly typed delegate to handle the Execute method with the parameter cast.
        /// </summary>
        protected Action<T> ExecuteParameterMethod { get; set; }

        /// <summary>
        /// The strongly typed delegate to handle the CanExecute method with the parameter cast.
        /// </summary>
        protected Func<T, bool> CanExecuteParameterMethod { get; set; }

        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="executeMethod">A delegate to handle the Execute method</param>
        /// <param name="canExecuteMethod">A delegate to handle the CanExecute method</param>
        public DelegateCommand(Action<T> executeMethod, Func<T, bool> canExecuteMethod)
        {
            this.ExecuteParameterMethod = executeMethod;
            this.CanExecuteParameterMethod = canExecuteMethod;
        }
        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="executeMethod">A delegate to handle the Execute method</param>
        /// <param name="canExecuteMethod">A delegate to handle the CanExecute method</param>
        public DelegateCommand(Action<T> executeMethod, Func<bool> canExecuteMethod)
        {
            this.ExecuteParameterMethod = executeMethod;
            base.CanExecuteMethod = canExecuteMethod;
        }

        /// <summary>
        /// Instantiates a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="executeMethod">A delegate to handle the Execute method</param>
        public DelegateCommand(Action<T> executeMethod)
        {
            this.ExecuteParameterMethod = executeMethod;
        }

        /// <inheritdoc /> 
        protected DelegateCommand()
        { }

        /// <inheritdoc /> 
        public override bool CanExecute()
        {
            if (CanExecuteParameterMethod == null) return true;
            return CanExecuteParameterMethod(default(T));
        }

        /// <inheritdoc /> 
        public override void Execute()
        {
            if (ExecuteParameterMethod != null)
            {
                ExecuteParameterMethod(default(T));
            }
        }

        /// <inheritdoc /> 
        public override bool CanExecute(object parameter)
        {
            if (CanExecuteParameterMethod == null)
            {
                return base.CanExecute();
            }
            else
            {
                if (parameter is ValueType || parameter != null)
                {
                    return CanExecuteParameterMethod((T)parameter);
                }
                else
                {
                    return CanExecuteParameterMethod(default(T));
                }
            }
        }

        /// <inheritdoc /> 
        public override void Execute(object parameter)
        {
            if (ExecuteParameterMethod != null)
            {
                if (parameter is ValueType || parameter != null)
                {
                    ExecuteParameterMethod((T)parameter);
                }
                else
                {
                    ExecuteParameterMethod(default(T));
                }
            }
        }
    }
}
