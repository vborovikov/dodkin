namespace Dodkin.Interop
{
    using System;
    using System.Text;

    /// <summary>
    /// Specifies how the application accesses the queue (peek, send, or receive).
    /// </summary>
    [Flags]
    public enum QueueAccessMode
    {
        /// <summary>
        /// Messages can be retrieved (read and removed) from the queue, peeked at, or purged.
        /// </summary>
        Receive = 1,

        /// <summary>
        /// Messages can only be sent to the queue. A subqueue cannot be opened using this access mode.
        /// </summary>
        Send = 2,

        /// <summary>
        /// Can be used only with a subqueue. Requires the user to have peek permission for the queue.
        /// </summary>
        Move = 4,

        /// <summary>
        /// Messages can only be looked at. They cannot be removed from the queue.
        /// </summary>
        Peek = 32,

        /// <summary>
        /// Used to access messages in a local outgoing queue.
        /// </summary>
        Admin = 128,
    }

    /// <summary>
    /// Specifies how the queue will be shared.
    /// </summary>
    public enum QueueShareMode
    {
        /// <summary>
        /// Default. The queue is available to everyone. 
        /// This setting must be used if the access mode is set to <see cref="QueueAccessMode.Send"/>.
        /// </summary>
        Shared = 0,

        /// <summary>
        /// Limits those who can receive messages from the queue to this process.
        /// Once a process opens a queue with this share mode and with the access mode set to <see cref="QueueAccessMode.Receive"/>,
        /// no one else, including the process that opened the queue, can open it again to peek or receive messages
        /// (this includes attempting to open the queue with multiple threads within the same process)
        /// until the original caller closes the queue.
        /// </summary>
        ExclusiveReceive = 1,
    }

    /// <summary>
    /// Manages the queue connection.
    /// </summary>
    class QueueConnection : IDisposable
    {
        private MessageQueueName queueName;
        private volatile bool queueNameValid;
        private readonly QueueShareMode shareMode;
        private readonly QueueAccessMode accessMode;

        private volatile QueueHandle readHandle = QueueHandle.InvalidHandle;
        private volatile QueueHandle writeHandle = QueueHandle.InvalidHandle;
        private volatile bool boundToThreadPool;
        private volatile bool isTransactionalValid = false;
        private bool isTransactional;

        private int refCount;
        private bool disposed;

        private readonly object syncRoot = new();

        public QueueConnection(MessageQueueName queueName, QueueAccessMode accessMode, QueueShareMode shareMode)
        {
            this.queueName = queueName;
            this.shareMode = shareMode;
            this.accessMode = accessMode;
        }

        public MessageQueueName QueueName
        {
            get
            {
                EnsureQueueName();
                return this.queueName;
            }
        }

        private void EnsureQueueName()
        {
            if (this.queueNameValid)
                return;

            while (true)
            {
                var handle = this.CanRead ? this.ReadHandle : this.CanWrite ? this.WriteHandle :
                    throw new MessageQueueException(MQ.HR.ERROR_QUEUE_NOT_ACTIVE);
                if (!handle.IsInvalid)
                {
                    lock (this.syncRoot)
                    {
                        if (this.queueNameValid)
                            break;

                        var formatNameBuilder = new StringBuilder(124);
                        var formatNameLength = formatNameBuilder.Capacity;

                        var hr = MQ.HandleToFormatName(handle, formatNameBuilder, ref formatNameLength);
                        if (MQ.IsBufferOverflow(hr))
                        {
                            formatNameBuilder.Capacity = formatNameLength;
                            hr = MQ.HandleToFormatName(handle, formatNameBuilder, ref formatNameLength);
                        }

                        if (!MQ.IsStaleHandle(hr))
                        {
                            MessageQueueException.ThrowOnError(hr);

                            formatNameBuilder.Length = formatNameLength - 1;
                            this.queueName = MessageQueueName.FromFormatName(formatNameBuilder.ToString());
                            this.queueNameValid = true;

                            break;
                        }
                    }
                }

                Close();
            }
        }

        public bool CanRead
        {
            get
            {
                if (!this.accessMode.CanRead())
                    return false;

                return TryOpenRead(out _);
            }
        }

        public bool CanWrite
        {
            get
            {
                if (!this.accessMode.CanWrite())
                    return false;

                return TryOpenWrite(out _);
            }
        }

        public int RefCount
        {
            get
            {
                return this.refCount;
            }
        }

        public QueueHandle ReadHandle
        {
            get
            {
                if (!TryOpenRead(out var errorCode))
                    throw new MessageQueueException(errorCode);

                return this.readHandle;
            }
        }

        public QueueHandle WriteHandle
        {
            get
            {
                if (!TryOpenWrite(out var errorCode))
                    throw new MessageQueueException(errorCode);

                return this.writeHandle;
            }
        }

        public bool IsTransactional
        {
            get
            {
                if (!this.isTransactionalValid)
                {
                    lock (this.syncRoot)
                    {
                        if (!this.isTransactionalValid)
                        {
                            using var props = new QueueProperties();
                            props.SetValue(MQ.PROPID.Q.TRANSACTION, (byte)MQ.PROPID.Q.TRANSACTIONAL.NONE);
                            using var packedProps = props.Pack();
                            var status = MQ.GetQueueProperties(this.queueName.FormatName, packedProps);
                            packedProps.Dispose();
                            MessageQueueException.ThrowOnError(status);

                            this.isTransactional = props.GetValue<byte>(MQ.PROPID.Q.TRANSACTION) != (byte)MQ.PROPID.Q.TRANSACTIONAL.NONE;
                            this.isTransactionalValid = true;
                        }
                    }
                }

                return this.isTransactional;
            }
        }

        public void AddRef()
        {
            lock (this)
            {
                ++this.refCount;
            }
        }

        public void BindToThreadPool()
        {
            if (!this.boundToThreadPool)
            {
                lock (this)
                {
                    if (!this.boundToThreadPool)
                    {
                        ThreadPool.BindHandle(this.ReadHandle);
                        this.boundToThreadPool = true;
                    }
                }
            }
        }

        public void CloseIfNotReferenced()
        {
            lock (this)
            {
                if (this.RefCount == 0)
                    Close();
            }
        }

        public void Close()
        {
            CloseWrite();
            CloseRead();
        }

        public void CloseRead()
        {
            this.boundToThreadPool = false;
            if (!this.readHandle.IsInvalid)
            {
                lock (this.syncRoot)
                {
                    if (!this.readHandle.IsInvalid)
                    {
                        this.readHandle.Close();
                    }
                }
            }
        }

        public void CloseWrite()
        {
            if (!this.writeHandle.IsInvalid)
            {
                lock (this.syncRoot)
                {
                    if (!this.writeHandle.IsInvalid)
                    {
                        this.writeHandle.Close();
                    }
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }

            this.disposed = true;
        }

        public void Release()
        {
            lock (this)
            {
                --this.refCount;
            }
        }

        private bool TryOpenRead(out MQ.HR errorCode)
        {
            if (this.readHandle.IsInvalid)
            {
                if (this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                lock (this.syncRoot)
                {
                    if (this.readHandle.IsInvalid)
                    {
                        var status = MQ.OpenQueue(this.queueName.FormatName, this.accessMode.GetReadAccessMode(), this.shareMode, out var result);
                        if (MQ.IsFatalError(status))
                        {
                            errorCode = status;
                            return false;
                        }

                        this.readHandle = result;
                    }
                }

                TryBindToThreadPool();
            }

            errorCode = default;
            return true;
        }

        public bool TryBindToThreadPool()
        {
            if (this.boundToThreadPool)
                return true;

            if (IsCompletionPortSupported(this.ReadHandle))
            {
                BindToThreadPool();
            }

            return this.boundToThreadPool;
        }

        private bool TryOpenWrite(out MQ.HR errorCode)
        {
            if (this.writeHandle.IsInvalid)
            {
                if (this.disposed)
                    throw new ObjectDisposedException(GetType().Name);

                lock (this.syncRoot)
                {
                    if (this.writeHandle.IsInvalid)
                    {
                        var status = MQ.OpenQueue(this.queueName.FormatName, this.accessMode.GetWriteAccessMode(), 0, out var result);
                        if (MQ.IsFatalError(status))
                        {
                            errorCode = status;
                            return false;
                        }

                        this.writeHandle = result;
                    }
                }
            }

            errorCode = default;
            return true;
        }

        private static bool IsCompletionPortSupported(QueueHandle handle)
        {
            // if it's a kernel handle, then it supports completion ports
            return MQ.GetHandleInformation(handle, out _) != 0;
        }
    }

    static class QueueAccessModeExtensions
    {
        public static bool CanRead(this QueueAccessMode accessMode)
        {
            return ((accessMode & QueueAccessMode.Receive) != 0) || ((accessMode & QueueAccessMode.Peek) != 0);
        }

        public static bool CanWrite(this QueueAccessMode accessMode)
        {
            return (accessMode & QueueAccessMode.Send) != 0;
        }

        public static QueueAccessMode GetReadAccessMode(this QueueAccessMode accessMode)
        {
            var result = accessMode & ~QueueAccessMode.Send;
            if (result != 0)
                return result;

            // this is fail-fast path, when we know right away that the operation is incompatible with access mode
            // AccessDenied can also happen in other cases,
            // (for example, when we try to receive on a queue opened only for peek.
            // We'll let MQReceiveMessage enforce these rules
            throw new MessageQueueException(MQ.HR.ERROR_ACCESS_DENIED);
        }

        public static QueueAccessMode GetWriteAccessMode(this QueueAccessMode accessMode)
        {
            var result = accessMode & QueueAccessMode.Send;
            if (result != 0)
                return result;

            // this is fail-fast path, when we know right away that the operation is incompatible with access mode
            // AccessDenied can also happen in other cases,
            // (for example, when we try to receive on a queue opened only for peek.
            // We'll let MQReceiveMessage enforce these rules
            throw new MessageQueueException(MQ.HR.ERROR_ACCESS_DENIED);
        }
    }
}
