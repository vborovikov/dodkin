namespace Dodkin;

using System.Runtime.Serialization;
using Interop;

[Serializable]
public class MessageQueueException : Exception
{
    protected MessageQueueException() { }

    protected MessageQueueException(string message) : base(message) { }

    internal MessageQueueException(MQ.HR errorCode)
        : base(errorCode.ToString())
    {
        this.HResult = (int)errorCode;
    }

    protected MessageQueueException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    protected MessageQueueException(string? message, Exception? innerException) : base(message, innerException) { }

    internal static void ThrowIfNotOK(MQ.HR hresult)
    {
        if (hresult != MQ.HR.OK)
            throw new MessageQueueException(hresult);
    }

    internal static void ThrowOnError(MQ.HR hresult)
    {
        if (MQ.IsFatalError(hresult))
            throw new MessageQueueException(hresult);
    }

    internal static void ThrowIfNotOK<T>(MQ.HR hresult, PropertyBag.Package package)
        where T : struct, Enum
    {
        if (hresult != MQ.HR.OK)
        {
            var ex = new MessageQueueException(hresult);
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
            var ex = new MessageQueueException(hresult);
#if DEBUG
            package.Dump<T>(ex.Data);
#endif
            throw ex;
        }
    }
}