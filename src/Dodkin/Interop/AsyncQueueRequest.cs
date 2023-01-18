namespace Dodkin.Interop
{
    using System.Threading;

    /// <summary>Bundle request details into a class that is keep in the <see cref="outstandingRequests"/> set during the async call.</summary>
    class AsyncQueueRequest : IAsyncResult, IDisposable
    {
        private readonly QueueConnection connection;
        private readonly QueueCursorHandle cursor;
        private readonly MessageProperties.Package packedProperties;
        private readonly TaskCompletionSource<Message> taskSource;
        private readonly CancellationTokenRegistration cancelReg;

        public AsyncQueueRequest(QueueConnection connection, QueueCursorHandle cursor, MessageProperties.Package packedProperties, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(cursor);

            this.connection = connection;
            this.cursor = cursor;
            this.packedProperties = packedProperties;
            this.OwnsProperties = true;

            this.taskSource = new TaskCompletionSource<Message>();
            this.cancelReg = cancellationToken.Register(CancelRequest, useSynchronizationContext: false);
        }

        public AsyncQueueRequest(QueueConnection connection, MessageProperties properties, CancellationToken cancellationToken)
            : this(connection, QueueCursorHandle.None, properties.Pack(), cancellationToken)
        {
        }

        public ReadAction Action { get; init; }

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

                        if (readAction == ReadAction.PeekNext)
                        {
                            // Need to special-case retrying PeekNext after a buffer overflow 
                            // by using PeekCurrent on retries since otherwise MSMQ will
                            // advance the cursor, skipping messages
                            readAction = ReadAction.PeekCurrent;
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

        private unsafe void EndRead(MQ.HR hrStatus, NativeOverlapped* pOverlap)
        {
            var freeResources = true;
            try
            {
                var result = MQ.GetOverlappedResult(pOverlap);
                while (true)
                {
                    if (MQ.IsBufferOverflow(result))
                    {
                        this.packedProperties.Adjust(result);
                        var readAction = this.Action == ReadAction.PeekNext ? ReadAction.PeekCurrent : this.Action;
                        result = MQ.ReceiveMessage(this.connection.ReadHandle, MQ.GetTimeout(this.Timeout), readAction,
                            this.packedProperties, pOverlap, null!, this.cursor, IntPtr.Zero);
                        if (result == MQ.HR.INFORMATION_OPERATION_PENDING)
                        {
                            freeResources = false;
                            return;
                        }
                    }
                    //todo: check for stale handle error
                    else if (!TryHandleError(result))
                    {
                        this.taskSource.TrySetResult(this.packedProperties.Unpack<MessageProperties>());
                    }

                    break; // request is completed
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
            var hrStatus = MQ.HR.OK;
            if (errorCode != 0u)
            {
                // MSMQ does a hacky trick to return the operation 
                // result through the completion port.

                // eugenesh Dec 2004. Bug 419155: 
                // NativeOverlapped.InternalLow returns IntPtr, which is 64 bits on a 64 bit platform.
                // It contains MSMQ error code, which, when set to an error value, is outside of the int range
                // Therefore, OverflowException is thrown in checked context. 
                // However, IntPtr (int) operator ALWAYS runs in checked context on 64 bit platforms.
                // Therefore, we first cast to long to avoid OverflowException, and then cast to int
                // in unchecked context 
                var msmqError = (long)pOverlap->InternalLow;
                unchecked
                {
                    hrStatus = (MQ.HR)msmqError;
                }
            }

            EndRead(hrStatus, pOverlap);
        }
    }
}