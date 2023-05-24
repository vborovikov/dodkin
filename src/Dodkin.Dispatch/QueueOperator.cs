namespace Dodkin.Dispatch;

using System;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Relay.RequestModel;
using MsmqMessage = Dodkin.Message;

public abstract class QueueOperator : IDisposable
{
    protected readonly record struct Message(MessageId Id, IRequest? Request)
    {
        public bool IsEmpty => this.Request is null;
    };

    private const MessageProperty MessageProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId | MessageProperty.RespQueue |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.Extension;

    private static readonly Dictionary<string, Type> bodyTypeCache = new();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(500);
    private static Assembly? interactionAssembly;

    private readonly IMessageQueueWriter outputQueue;
    private readonly IMessageQueueReader inputQueue;
    private readonly string inputQueueFormatName;
    private readonly TimeSpan timeout;

    protected QueueOperator(string inputQueuePath, string outputQueuePath, TimeSpan? timeout = null)
    {
        this.inputQueue = new MessageQueueReader(MessageQueueName.FromPathName(inputQueuePath));
        this.outputQueue = new MessageQueueWriter(MessageQueueName.FromPathName(outputQueuePath));
        this.inputQueueFormatName = this.inputQueue.Name.FormatName;

        this.timeout = timeout ?? DefaultTimeout;
    }

    public static void ReferenceAssembly(Assembly assembly)
    {
        interactionAssembly = assembly;
    }

    internal static Assembly InteractionAssembly => interactionAssembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

    public void Dispose()
    {
        this.inputQueue.Dispose();
        this.outputQueue.Dispose();
    }

    protected Task<MessageId> SendRequestAsync(IRequest request, CancellationToken cancellationToken)
    {
        return SendResultAsync(request, default, cancellationToken);
    }

    protected async Task<MessageId> SendResultAsync(object result, MessageId corellationId, CancellationToken cancellationToken)
    {
        using var message = CreateMessage(result, corellationId);
        await this.outputQueue.WriteAsync(message, null, cancellationToken);
        return message.Id;
    }

    protected async Task<Message> ReceiveRequestAsync(CancellationToken cancellationToken)
    {
        using var message = await this.inputQueue.ReadAsync(MessageProperties, cancellationToken: cancellationToken);
        return new Message(message.Id, ReadMessage(message) as IRequest);
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

    private MsmqMessage CreateMessage(object body, MessageId corellationId = default)
    {
        var message = new MsmqMessage(
            JsonSerializer.SerializeToUtf8Bytes(body),
            JsonSerializer.SerializeToUtf8Bytes(body.GetType().AssemblyQualifiedName))
        {
            CorrelationId = corellationId,
            ResponseQueue = this.inputQueueFormatName,
            Label = body.GetType().Name,
        };

        return message;
    }

    private static object? ReadMessage(MsmqMessage message)
    {
        var bodyType = FindBodyType(message);
        if (bodyType is not null)
        {
            var body = JsonSerializer.Deserialize(message.Body, bodyType);
            return body;
        }

        return null;
    }

    private static Type? FindBodyType(MsmqMessage message)
    {
        var bodyTypeName = JsonSerializer.Deserialize<string>(message.Extension);
        if (bodyTypeName is null)
            return null;
        if (bodyTypeCache.TryGetValue(bodyTypeName, out var bodyType))
            return bodyType;

        bodyType = Type.GetType(bodyTypeName, throwOnError: false);
        if (bodyType is null)
        {
            var typeInfo = new AssemblyQualifiedTypeName(bodyTypeName);
            var typeFullName = typeInfo.FullName;

            bodyType =
                InteractionAssembly.GetType(typeFullName, throwOnError: false) ??
                Type.GetType(typeInfo.FullName, throwOnError: false);
        }
        if (bodyType is not null)
        {
            bodyTypeCache.Add(bodyTypeName, bodyType);
            return bodyType;
        }

        return null;
    }
}