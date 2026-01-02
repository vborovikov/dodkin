namespace Dodkin.Dispatch;

using System;

public abstract class UdsOperator : MessageOperator<ReadOnlySpan<byte>>
{
    protected const char PayloadSeparator = '\n';
    protected const int MaxConnections = 10;

    protected UdsOperator()
    {
        this.Timeout = DefaultTimeout;
    }

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
