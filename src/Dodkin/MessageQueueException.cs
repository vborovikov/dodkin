namespace Dodkin;

using System.Runtime.Serialization;
using Interop;

[Serializable]
public class MessageQueueException : Exception
{
    protected MessageQueueException() { }

    protected MessageQueueException(string message) : base(message) { }

    internal MessageQueueException(MQ.HR errorCode)
        : base(MQ.GetErrorMessage(errorCode))
    {
        this.HResult = (int)errorCode;
    }

    protected MessageQueueException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected MessageQueueException(string? message, Exception? innerException) : base(message, innerException) { }

    internal static void ThrowIfNotOK(MQ.HR hresult)
    {
        if (hresult != MQ.HR.OK)
        {
            if (hresult == MQ.HR.ERROR_IO_TIMEOUT)
                throw new TimeoutException { HResult = (int)hresult };

            throw new MessageQueueException(hresult);
        }
    }

    internal static void ThrowOnError(MQ.HR hresult)
    {
        if (hresult == MQ.HR.ERROR_IO_TIMEOUT)
            throw new TimeoutException { HResult = (int)hresult };
        if (MQ.IsFatalError(hresult))
            throw new MessageQueueException(hresult);
    }

    internal static void ThrowIfNotOK<T>(MQ.HR hresult, PropertyBag.Package package)
        where T : struct, Enum
    {
        if (hresult != MQ.HR.OK)
        {
            Exception ex = hresult == MQ.HR.ERROR_IO_TIMEOUT ?
                new TimeoutException { HResult = (int)hresult } : new MessageQueueException(hresult);
#if DEBUG
            package.Dump<T>(ex.Data);
#endif
            throw ex;
        }
    }

    internal static void ThrowOnError<T>(MQ.HR hresult, PropertyBag.Package package)
        where T : struct, Enum
    {
        if (MQ.IsFatalError(hresult))
        {
            Exception ex = hresult == MQ.HR.ERROR_IO_TIMEOUT ?
                new TimeoutException { HResult = (int)hresult } : new MessageQueueException(hresult);
#if DEBUG
            package.Dump<T>(ex.Data);
#endif
            throw ex;
        }
    }
}