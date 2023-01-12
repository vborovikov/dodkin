namespace Dodkin
{
    using System.Diagnostics.CodeAnalysis;
    using Interop;

    /// <summary>
    /// A transaction to send or receive messages.
    /// Use <see cref="SingleMessage"/> for a transaction that only exists for a single send or receive,
    /// use <see cref="DTC"/> to the use ambient DTC transaction.
    /// </summary>
    public class QueueTransaction : IDisposable
    {
        /// <summary>
        /// An ambient DTC transaction from System.Transactions.TransactionScope
        /// </summary>
        public static readonly QueueTransaction DTC = new SpecialTransaction(1);

        /// <summary>
        /// XA-compliant transaction
        /// </summary>
        public static readonly QueueTransaction XA = new SpecialTransaction(2);

        /// <summary>
        /// A single-message transaction
        /// </summary>
        public static readonly QueueTransaction SingleMessage = new SpecialTransaction(3);

        private readonly ITransaction internalTransaction;
        private bool comitted;
        private bool disposed;

        /// <summary>Starts a new MSMQ internal transaction</summary>
        public QueueTransaction()
        {
            MessageQueueException.ThrowIfNotOK(MQ.BeginTransaction(out this.internalTransaction));
        }

        internal ITransaction InternalTransaction => this.internalTransaction;

        /// <summary>Commit the transaction</summary>
        public virtual void Commit()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(QueueTransaction));

            MessageQueueException.ThrowIfNotOK((MQ.HR)this.InternalTransaction.Commit(0, 0, 0));
            this.comitted = true;
        }

        /// <summary>Rollback the transaction</summary>
        public virtual void Abort()
        {
            if (this.disposed)
                throw new ObjectDisposedException(nameof(QueueTransaction));

            MessageQueueException.ThrowIfNotOK((MQ.HR)this.InternalTransaction.Abort(0, 0, 0));
            this.comitted = true;
        }

        /// <summary>Aborts the transaction if <see cref="Commit"/> or <see cref="Abort"/> has not already been called</summary>
        public virtual void Dispose()
        {
            if (this.disposed || this.comitted)
                return;

            this.InternalTransaction.Abort(0, 0, 0); // don't check for errors or throw in dispose method

            this.disposed = true;
            this.comitted = true;
        }

        internal sealed class SpecialTransaction : QueueTransaction
        {
            public IntPtr SpecialId { get; }

            public SpecialTransaction(nint specialId)
            {
                this.SpecialId = specialId;
            }

            public override void Commit()
            {
                throw new NotImplementedException();
            }

            public override void Abort()
            {
                throw new NotImplementedException();
            }

            public override void Dispose()
            {
            }
        }
    }

    static class QueueTransactionExtensions
    {
        public static bool TryGetHandle([NotNullWhen(false)] this QueueTransaction? transaction, out IntPtr handle)
        {
            if (transaction is null)
            {
                handle = IntPtr.Zero;
                return true;
            }

            if (transaction is QueueTransaction.SpecialTransaction special)
            {
                handle = special.SpecialId;
                return true;
            }

            handle = IntPtr.Zero;
            return false;
        }
    }
}
