namespace Dodkin.Dispatch;

using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents a queue operator that recognizes request messages.
/// </summary>
public abstract class QueueOperator : MessageOperator<Message>
{
    /// <summary>
    /// The essential message properties that are recognized by the operator.
    /// </summary>
    protected const MessageProperty MessageProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId | MessageProperty.RespQueue |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.Extension | MessageProperty.LookupId;

    /// <summary>
    /// Creates new instance of <see cref="QueueOperator"/>
    /// </summary>
    /// <param name="endpoint">The endpoint describing the queues used by the queue operator.</param>
    /// <param name="logger">The logger.</param>
    protected QueueOperator(MessageEndpoint endpoint, ILogger logger)
    {
        this.Endpoint = endpoint;
        this.Timeout = DefaultTimeout;

        try
        {
            this.Endpoint.CreateIfNotExists(GetQueueLabel(), isTransactional: true);
        }
        catch (Exception ex)
        {
            logger.LogError(EventIds.EndpointFailed, ex, "Failed to create message endpoint: {MessageEndpoint}.", this.Endpoint);
            throw;
        }
    }


    /// <summary>
    /// Creates a new <see cref="Message"/> instance.
    /// </summary>
    /// <param name="body">The body of the message.</param>
    /// <param name="corellationId">The correlation message ID.</param>
    /// <param name="timeout">The message timeout.</param>
    /// <returns>The created message.</returns>
    protected Message CreateMessage(object body, in MessageId corellationId = default, TimeSpan? timeout = null)
    {
        var bodyType = body.GetType();
        var message = new Message(JsonSerializer.SerializeToUtf8Bytes(body),
            JsonSerializer.SerializeToUtf8Bytes(bodyType.AssemblyQualifiedName))
        {
            CorrelationId = corellationId,
            Label = bodyType.Name,
            ResponseQueue = this.Endpoint.ResponseQueue ?? this.Endpoint.ApplicationQueue,

            AdministrationQueue = this.Endpoint.AdministrationQueue,
            TimeToReachQueue = timeout ?? this.Timeout,
            Acknowledgment = MessageAcknowledgment.FullReachQueue,

            DeadLetterQueue = this.Endpoint.DeadLetterQueue.PathName,
            Journal = MessageJournaling.DeadLetter,
        };

        return message;
    }

    private string GetQueueLabel()
    {
        var type = GetType();
        var operatorName = type.Name;

        if (type.Assembly.GetName().Name is string { Length: > 0 } assemblyName)
        {
            return $"{assemblyName}.{operatorName}";
        }

        return operatorName;
    }

    /// <summary>
    /// The endpoint describing the queues used by the queue operator.
    /// </summary>
    protected MessageEndpoint Endpoint { get; }

    /// <inheritdoc/>
    protected sealed override bool IsEmpty(in Message message) => message.IsEmpty;

    /// <inheritdoc/>
    protected sealed override ReadOnlySpan<byte> GetSignature(in Message message) => message.Extension;

    /// <inheritdoc/>
    protected sealed override ReadOnlySpan<byte> GetBody(in Message message) => message.Body;
}