namespace Dodkin.Dispatch;

using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Relay.RequestModel;

public abstract class QueueOperator : IDisposable
{
    protected record Envelope(MessageId Id, MessageQueueName? ResponseQueue, IRequest? Request)
    {
        public bool IsEmpty => this.Request is null;
    };

    private const MessageProperty MessageProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId | MessageProperty.RespQueue |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.Extension;

    private static readonly Dictionary<string, Type> bodyTypeCache = new();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);
    private static Assembly? interactionAssembly;

    private readonly IMessageQueueReader inputQueue;
    private readonly MessageEndpoint endpoint;
    private readonly TimeSpan timeout;

    protected QueueOperator(MessageEndpoint endpoint, TimeSpan? timeout = null)
    {
        this.endpoint = endpoint;
        this.inputQueue = new MessageQueueReader(this.endpoint.ApplicationQueue);
        this.timeout = timeout ?? DefaultTimeout;
    }

    public static void ReferenceAssembly(Assembly assembly)
    {
        interactionAssembly = assembly;
    }

    internal static Assembly InteractionAssembly => interactionAssembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

    public void Dispose()
    {
        Dispose(true);
    }

    protected virtual void Dispose(bool disposing)
    {
        this.inputQueue.Dispose();
    }

    protected Task<MessageId> SendRequestAsync(IRequest request, CancellationToken cancellationToken)
    {
        return SendResultAsync(request, default, cancellationToken);
    }

    protected async Task<MessageId> SendResultAsync(object result, Envelope? original, CancellationToken cancellationToken)
    {
        using var message = CreateMessage(result, original?.Id ?? default);
        await SendMessageAsync(message, original?.ResponseQueue, cancellationToken);
        return message.Id;
    }

    protected abstract Task SendMessageAsync(Message message, MessageQueueName? destinationQueue, CancellationToken cancellationToken);

    protected async Task<Envelope> ReceiveRequestAsync(CancellationToken cancellationToken)
    {
        using var message = await this.inputQueue.ReadAsync(MessageProperties, cancellationToken: cancellationToken);
        return new Envelope(message.Id,
            MessageQueueName.TryParse(message.ResponseQueue, out var responseQueue) ? responseQueue : null,
            ReadMessage(message) as IRequest);
    }

    protected async Task<TResult> ReceiveResultAsync<TResult>(MessageId correlationId, TimeSpan? timeout, CancellationToken cancellationToken)
    {
        if (timeout == null || timeout == default(TimeSpan) || timeout == TimeSpan.Zero)
            timeout = this.timeout;

        using var msg = await this.inputQueue.ReadAsync(correlationId, MessageProperties, timeout, cancellationToken);
        if (msg.IsEmpty)
            return default!;

        var body = ReadMessage(msg);
        return body is null ? default! : (TResult)body;
    }

    private Message CreateMessage(object body, in MessageId corellationId = default)
    {
        var bodyType = body.GetType();
        var message = new Message(JsonSerializer.SerializeToUtf8Bytes(body),
            JsonSerializer.SerializeToUtf8Bytes<string>(bodyType.AssemblyQualifiedName!))
        {
            CorrelationId = corellationId,
            ResponseQueue = this.inputQueue.Name,
            Label = bodyType.Name,
        };

        return message;
    }

    private static object? ReadMessage(in Message message)
    {
        var bodyType = FindBodyType(message);
        if (bodyType is not null)
        {
            var body = JsonSerializer.Deserialize(message.Body, bodyType);
            return body;
        }

        return null;
    }

    private static Type? FindBodyType(in Message message)
    {
        var bodyTypeName = JsonSerializer.Deserialize<string>(message.Extension);
        if (bodyTypeName is null)
            return null;
        if (bodyTypeCache.TryGetValue(bodyTypeName, out var bodyType))
            return bodyType;

        bodyType = Type.GetType(bodyTypeName, throwOnError: false);
        if (bodyType is null && AssemblyQualifiedTypeName.TryParse(bodyTypeName, out var typeInfo))
        {
            bodyType =
                InteractionAssembly.GetType(typeInfo.FullName, throwOnError: false) ??
                Type.GetType(typeInfo.FullName, throwOnError: false) ??
                FindBodyTypeByName(typeInfo.FullName);
        }
        if (bodyType is not null)
        {
            bodyTypeCache.Add(bodyTypeName, bodyType);
            return bodyType;
        }

        return null;
    }

    private static Type? FindBodyTypeByName(ReadOnlySpan<char> typeFullName)
    {
        var nameStart = typeFullName.LastIndexOf('.');
        if (nameStart <= 0)
            return null;

        var typeName = typeFullName[(nameStart + 1)..];
        foreach (var exportedType in InteractionAssembly.GetExportedTypes())
        {
            if (typeName.Equals(exportedType.Name, StringComparison.OrdinalIgnoreCase))
                return exportedType;
        }

        return null;
    }
}