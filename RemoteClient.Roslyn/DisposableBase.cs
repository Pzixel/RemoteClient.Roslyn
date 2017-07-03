using System;
using System.Threading;

namespace RemoteClient.Roslyn
{
    /// <summary>
    /// DisposableBase class. Represents an implementation of the IDisposable interface.
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        /// <summary>
        /// A value which indicates the disposable state. 0 indicates undisposed, 1 indicates disposing
        /// or disposed.
        /// </summary>
        private volatile int _disposableState;

        protected internal bool IsDisposed => _disposableState == 1;

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with disposing of resources.
        /// </summary>
        public void Dispose()
        {
#pragma warning disable 420
            if (Interlocked.CompareExchange(ref _disposableState, 1, 0) == 0)
#pragma warning restore 420
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        #endregion IDisposable Members

        /// <summary>
        /// Dispose resources. Override this method in derived classes. Unmanaged resources should always be released
        /// when this method is called. Managed resources may only be disposed of if disposing is true.
        /// </summary>
        /// <param name="disposing">A value which indicates whether managed resources may be disposed of.</param>
        protected abstract void Dispose(bool disposing);
    }
}
