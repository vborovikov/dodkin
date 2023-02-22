namespace Dodkin.Interop
{
    using System.Threading;

    /// <summary>
    /// Represents an asynchronous operation on the message queue.
    /// </summary>
    abstract class QueueAsyncRequest : IAsyncResult, IDisposable
    {
        protected readonly QueueConnection connection;
        protected readonly QueueCursorHandle cursorHandle;
        protected readonly PropertyBag.Package packedProperties;
        private readonly TaskCompletionSource<Message> taskSource;
        private readonly CancellationTokenRegistration cancelReg;

        protected QueueAsyncRequest(QueueConnection connection, QueueCursorHandle cursorHandle, MessageProperties properties, CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.cursorHandle = cursorHandle;
            this.packedProperties = properties.Pack();

            this.taskSource = new TaskCompletionSource<Message>();
            this.cancelReg = cancellationToken.Register(CancelRequest, useSynchronizationContext: false);
        }

        object? IAsyncResult.AsyncState => this.taskSource.Task.AsyncState;
        WaitHandle IAsyncResult.AsyncWaitHandle => ((IAsyncResult)this.taskSource.Task).AsyncWaitHandle;
        bool IAsyncResult.CompletedSynchronously => ((IAsyncResult)this.taskSource.Task).CompletedSynchronously;
        bool IAsyncResult.IsCompleted => this.taskSource.Task.IsCompleted;

        public void Dispose()
        {
            this.packedProperties.Dispose();
            this.cancelReg.Dispose();
        }

        public unsafe Task<Message> BeginRead()
        {
            // create overlapped with callback that sets the task complete source
            var overlapped = new Overlapped { AsyncResult = this };
            var nativeOverlapped = overlapped.Pack(CompletionCallback, null);
            var freeResources = false;
            try
            {
                var adjustAction = false;
                while (true)
                {
                    // receive, may complete synchronously or call the async callback on the overlapped defined above
                    var result = ReceiveMessage(nativeOverlapped, adjustAction);

                    if (MQ.IsBufferOverflow(result))
                    {
                        // successfully completed synchronously but no enough memory

                        adjustAction = true;
                        this.packedProperties.Adjust(result);
                        continue; // try again
                    }
                    else if (MQ.IsStaleHandle(result))
                    {
                        //todo: close cursor handle?
                        this.connection.CloseRead();
                        continue;
                    }

                    freeResources = TryHandleError(result);
                    break; // return the task
                }
            }
            catch (ObjectDisposedException)
            {
                this.taskSource.TrySetCanceled();
                freeResources = true;
            }
            finally
            {
                if (freeResources)
                {
                    Overlapped.Free(nativeOverlapped);
                    Dispose();
                }
            }

            return this.taskSource.Task;
        }

        protected abstract unsafe MQ.HR ReceiveMessage(NativeOverlapped* nativeOverlapped, bool anotherTry);

        private unsafe void EndRead(MQ.HR result, NativeOverlapped* pOverlap)
        {
            var freeResources = true;
            try
            {
                var adjustAction = false;
                while (true)
                {
                    if (MQ.IsBufferOverflow(result))
                    {
                        adjustAction = true;
                        this.packedProperties.Adjust(result);
                    }
                    else if (MQ.IsStaleHandle(result))
                    {
                        //todo: close cursor handle?
                        this.connection.CloseRead();
                    }
                    else
                    {
                        if (!TryHandleError(result))
                        {
                            // request is completed without errors
                            this.taskSource.TrySetResult(this.packedProperties.Unpack<MessageProperties>());
                        }
                        return;
                    }

                    result = ReceiveMessage(pOverlap, adjustAction);

                    if (result == MQ.HR.INFORMATION_OPERATION_PENDING)
                    {
                        // request is pending again
                        freeResources = false;
                        return;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                this.taskSource.TrySetCanceled();
            }
            finally
            {
                if (freeResources)
                {
                    Overlapped.Free(pOverlap);
                    Dispose();
                }
            }
        }

        private bool TryHandleError(MQ.HR result)
        {
            if (MQ.IsFatalError(result))
            {
                // something went wrong
                if (result == MQ.HR.ERROR_IO_TIMEOUT)
                {
                    this.taskSource.TrySetResult(default);
                }
                else if (result == MQ.HR.ERROR_OPERATION_CANCELLED)
                {
                    this.taskSource.TrySetCanceled();
                }
                else
                {
                    this.taskSource.TrySetException(new MessageQueueException(result));
                }

                return true;
            }

            return false;
        }

        private void CancelRequest()
        {
            this.connection.CloseRead();
            this.taskSource.TrySetCanceled(this.cancelReg.Token);
        }

        private unsafe void CompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlap)
        {
            EndRead(MQ.GetOverlappedResult(pOverlap), pOverlap);
        }
    }

    sealed class QueueReceiveAsyncRequest : QueueAsyncRequest
    {
        public QueueReceiveAsyncRequest(QueueConnection connection, QueueCursorHandle cursorHandle, MessageProperties properties, CancellationToken cancellationToken) : base(connection, cursorHandle, properties, cancellationToken)
        {
        }

        public ReceiveAction Action { get; init; }

        public TimeSpan? Timeout { get; init; }

        protected override unsafe MQ.HR ReceiveMessage(NativeOverlapped* nativeOverlapped, bool anotherTry)
        {
            // Need to special-case retrying PeekNext after a buffer overflow 
            // by using PeekCurrent on retries since otherwise MSMQ will
            // advance the cursor, skipping messages
            var action = anotherTry && this.Action == ReceiveAction.PeekNext ? ReceiveAction.PeekCurrent : this.Action;
            return MQ.ReceiveMessage(this.connection.ReadHandle, MQ.GetTimeout(this.Timeout), action,
                        this.packedProperties, nativeOverlapped, null, this.cursorHandle, IntPtr.Zero);
        }
    }

    sealed class QueueLookupAsyncRequest : QueueAsyncRequest
    {
        public QueueLookupAsyncRequest(QueueConnection connection, QueueCursorHandle cursorHandle, MessageProperties properties, CancellationToken cancellationToken) : base(connection, cursorHandle, properties, cancellationToken)
        {
        }

        public LookupAction Action { get; init; }

        public ulong LookupId { get; init; }

        protected override unsafe MQ.HR ReceiveMessage(NativeOverlapped* nativeOverlapped, bool anotherTry)
        {
            return MQ.ReceiveMessageByLookupId(this.connection.ReadHandle, this.LookupId, this.Action,
                        this.packedProperties, nativeOverlapped, null, IntPtr.Zero);
        }
    }
}