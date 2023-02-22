namespace Dodkin.Interop
{
    using System.Threading;

    /// <summary>
    /// Represents an asynchronous operation on the message queue.
    /// </summary>
    class QueueAsyncRequest : IAsyncResult, IDisposable
    {
        private readonly QueueConnection connection;
        private readonly QueueCursorHandle cursor;
        private readonly MessageProperties.Package packedProperties;
        private readonly TaskCompletionSource<Message> taskSource;
        private readonly CancellationTokenRegistration cancelReg;

        public QueueAsyncRequest(QueueConnection connection, QueueCursorHandle cursor, MessageProperties.Package packedProperties, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(cursor);

            this.connection = connection;
            this.cursor = cursor;
            this.packedProperties = packedProperties;
            this.OwnsProperties = true;

            this.taskSource = new TaskCompletionSource<Message>();
            this.cancelReg = cancellationToken.Register(CancelRequest, useSynchronizationContext: false);
        }

        public QueueAsyncRequest(QueueConnection connection, MessageProperties properties, CancellationToken cancellationToken)
            : this(connection, QueueCursorHandle.None, properties.Pack(), cancellationToken)
        {
        }

        public ReceiveAction Action { get; init; }

        public TimeSpan? Timeout { get; init; }

        public bool OwnsProperties { get; init; }

        object? IAsyncResult.AsyncState => this.taskSource.Task.AsyncState;
        WaitHandle IAsyncResult.AsyncWaitHandle => ((IAsyncResult)this.taskSource.Task).AsyncWaitHandle;
        bool IAsyncResult.CompletedSynchronously => ((IAsyncResult)this.taskSource.Task).CompletedSynchronously;
        bool IAsyncResult.IsCompleted => this.taskSource.Task.IsCompleted;

        public void Dispose()
        {
            if (this.OwnsProperties)
            {
                // we own the packed properties
                this.packedProperties.Dispose();
            }
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
                var readAction = this.Action;
                while (true)
                {
                    // receive, may complete synchronously or call the async callback on the overlapped defined above
                    var result = MQ.ReceiveMessage(this.connection.ReadHandle, MQ.GetTimeout(this.Timeout), readAction,
                        this.packedProperties, nativeOverlapped, null!, this.cursor, IntPtr.Zero);

                    if (MQ.IsBufferOverflow(result))
                    {
                        // successfully completed synchronously but no enough memory

                        if (readAction == ReceiveAction.PeekNext)
                        {
                            // Need to special-case retrying PeekNext after a buffer overflow 
                            // by using PeekCurrent on retries since otherwise MSMQ will
                            // advance the cursor, skipping messages
                            readAction = ReceiveAction.PeekCurrent;
                        }

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

        private unsafe void EndRead(MQ.HR result, NativeOverlapped* pOverlap)
        {
            var freeResources = true;
            try
            {
                while (true)
                {
                    if (MQ.IsBufferOverflow(result))
                    {
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

                    var readAction = this.Action == ReceiveAction.PeekNext ? ReceiveAction.PeekCurrent : this.Action;
                    result = MQ.ReceiveMessage(this.connection.ReadHandle, MQ.GetTimeout(this.Timeout), readAction,
                        this.packedProperties, pOverlap, null, this.cursor, IntPtr.Zero);

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
}