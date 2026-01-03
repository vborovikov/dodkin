namespace Dodkin.Dispatch;

using System;
using System.Net.Sockets;

public abstract class UdsOperator : MessageOperator<ReadOnlySpan<byte>>
{
    protected const char PayloadSeparator = '\n';
    protected const int MaxConnections = 10;

    protected UdsOperator(string socketPath)
    {
        this.Endpoint = new(socketPath);
        this.Timeout = DefaultTimeout;
    }


    public UnixDomainSocketEndPoint Endpoint { get; }

    /// <inheritdoc/>
    protected sealed override bool IsEmpty(in ReadOnlySpan<byte> message) => message.IsEmpty;

    /// <inheritdoc/>
    protected sealed override ReadOnlySpan<byte> GetSignature(in ReadOnlySpan<byte> message)
    {
        var payloadPos = message.IndexOf((byte)PayloadSeparator);
        if (payloadPos > 0) return message[..payloadPos];

        return [];
    }

    /// <inheritdoc/>
    protected sealed override ReadOnlySpan<byte> GetBody(in ReadOnlySpan<byte> message)
    {
        var payloadPos = message.IndexOf((byte)PayloadSeparator);
        if (payloadPos > 0) return message[payloadPos..];

        return [];
    }
}
