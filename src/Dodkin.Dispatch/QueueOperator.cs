namespace Dodkin.Dispatch;

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

/// <summary>
/// Represents a queue operator that recognizes request messages.
/// </summary>
public abstract class QueueOperator : IDisposable
{
    /// <summary>
    /// The essential message properties that are recognized by the operator.
    /// </summary>
    protected const MessageProperty MessageProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId | MessageProperty.RespQueue |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.Extension | MessageProperty.LookupId;

    /// <summary>
    /// Provides the queue default operating timeout.
    /// </summary>
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
    private readonly ConcurrentDictionary<string, Type> bodyTypeCache = new();
    private Assembly? interactionAssembly;

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
    /// References the assembly that contains the recognized request types.
    /// </summary>
    /// <param name="assembly"></param>
    public void RecognizeTypesFrom(Assembly assembly)
    {
        if (Interlocked.CompareExchange(ref this.interactionAssembly, assembly, null) == this.interactionAssembly &&
            this.interactionAssembly != assembly)
        {
            throw new InvalidOperationException("Interaction assembly is already referenced.");
        }
    }

    /// <summary>
    /// Gets the assembly that contains the recognized request types.
    /// </summary>
    private Assembly InteractionAssembly => this.interactionAssembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

    /// <summary>
    /// The endpoint describing the queues used by the queue operator.
    /// </summary>
    protected MessageEndpoint Endpoint { get; }

    /// <summary>
    /// Gets or sets the queue operating timeout.
    /// </summary>
    protected TimeSpan Timeout { get; init; }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected abstract void Dispose(bool disposing);

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
            ResponseQueue = this.Endpoint.ApplicationQueue,

            AdministrationQueue = this.Endpoint.AdministrationQueue,
            TimeToReachQueue = timeout ?? this.Timeout,
            Acknowledgment = MessageAcknowledgment.FullReachQueue,

            DeadLetterQueue = this.Endpoint.DeadLetterQueue.PathName,
            Journal = MessageJournaling.DeadLetter,
        };

        return message;
    }

    /// <summary>
    /// Reads the body of a <see cref="Message"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="message">The message.</param>
    /// <returns>The message body.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected T Read<T>(in Message message)
    {
        if (message.IsEmpty)
            throw new InvalidOperationException("The received message is empty.");

        var bodyType = FindBodyType(message) ??
            throw new InvalidOperationException("The message body type cannot be determined.");
        var body = JsonSerializer.Deserialize(message.Body, bodyType) ??
            throw new InvalidOperationException("The message body cannot be deserialized.");

        return (T)body;
    }

    /// <summary>
    /// Tries to read the body of a <see cref="Message"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="message">The message.</param>
    /// <param name="request">The message body.</param>
    /// <returns><c>true</c> if the message body could be read; otherwise, <c>false</c>.</returns>
    protected bool TryRead<T>(in Message message, [MaybeNullWhen(false)] out T request)
        where T : notnull, IRequest
    {
        if (!message.IsEmpty)
        {
            var bodyType = FindBodyType(message);
            if (bodyType is not null)
            {
                var body = JsonSerializer.Deserialize(message.Body, bodyType);
                if (body is T actualRequest)
                {
                    request = actualRequest;
                    return true;
                }
            }
        }

        request = default;
        return false;
    }

    /// <summary>
    /// Determines the type of the message body.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The type of the message body or <c>null</c> if the body type could not be determined.</returns>
    protected Type? FindBodyType(in Message message)
    {
        var bodyTypeBuffer = message.Extension;
        if (bodyTypeBuffer.IsEmpty)
            return null;

        var bodyTypeName = JsonSerializer.Deserialize<string>(bodyTypeBuffer);
        if (bodyTypeName is null)
            return null;
        if (this.bodyTypeCache.TryGetValue(bodyTypeName, out var bodyType))
            return bodyType;

        bodyType = Type.GetType(bodyTypeName, throwOnError: false);
        if (bodyType is null && AssemblyQualifiedTypeName.TryParse(bodyTypeName, out var typeInfo))
        {
            bodyType =
                this.InteractionAssembly.GetType(typeInfo.FullName, throwOnError: false) ??
                Type.GetType(typeInfo.FullName, throwOnError: false) ??
                FindBodyTypeByName(typeInfo.FullName);
        }
        if (bodyType is not null)
        {
            this.bodyTypeCache.TryAdd(bodyTypeName, bodyType);
            return bodyType;
        }

        return null;
    }

    private Type? FindBodyTypeByName(ReadOnlySpan<char> typeFullName)
    {
        var nameStart = typeFullName.LastIndexOf('.');
        if (nameStart <= 0)
            return null;

        var requestType = typeof(IRequest);
        var typeName = typeFullName[(nameStart + 1)..];
        foreach (var exportedType in this.InteractionAssembly.GetExportedTypes())
        {
            if (requestType.IsAssignableFrom(exportedType) && typeName.Equals(exportedType.Name, StringComparison.OrdinalIgnoreCase))
                return exportedType;
        }

        return null;
    }
}