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
    protected const MessageProperty MessageProperties =
        MessageProperty.MessageId | MessageProperty.CorrelationId | MessageProperty.RespQueue |
        MessageProperty.Label | MessageProperty.Body | MessageProperty.Extension | MessageProperty.LookupId;

    private static readonly ConcurrentDictionary<string, Type> bodyTypeCache = new();
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
    private static Assembly? interactionAssembly;

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

    public static void ReferenceAssembly(Assembly assembly)
    {
        interactionAssembly = assembly;
    }

    internal static Assembly InteractionAssembly => interactionAssembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

    protected MessageEndpoint Endpoint { get; }

    protected TimeSpan Timeout { get; init; }

    public void Dispose()
    {
        Dispose(true);
    }

    protected abstract void Dispose(bool disposing);

    protected Message CreateMessage(object body, in MessageId corellationId = default, TimeSpan? timeout = null)
    {
        var bodyType = body.GetType();
        var message = new Message(JsonSerializer.SerializeToUtf8Bytes(body),
            JsonSerializer.SerializeToUtf8Bytes<string>(bodyType.AssemblyQualifiedName!))
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

    protected static T Read<T>(in Message message)
    {
        if (message.IsEmpty)
            throw new InvalidOperationException();
        var bodyType = FindBodyType(message);
        if (bodyType is null)
            throw new InvalidOperationException();
        var body = JsonSerializer.Deserialize(message.Body, bodyType);
        if (body is null)
            throw new InvalidOperationException();
        return (T)body;
    }

    protected static bool TryRead<T>(in Message message, [MaybeNullWhen(false)] out T request)
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

    protected static Type? FindBodyType(in Message message)
    {
        var bodyTypeBuffer = message.Extension;
        if (bodyTypeBuffer.IsEmpty)
            return null;

        var bodyTypeName = JsonSerializer.Deserialize<string>(bodyTypeBuffer);
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
            bodyTypeCache.TryAdd(bodyTypeName, bodyType);
            return bodyType;
        }

        return null;
    }

    private static Type? FindBodyTypeByName(ReadOnlySpan<char> typeFullName)
    {
        var nameStart = typeFullName.LastIndexOf('.');
        if (nameStart <= 0)
            return null;

        var requestType = typeof(IRequest);
        var typeName = typeFullName[(nameStart + 1)..];
        foreach (var exportedType in InteractionAssembly.GetExportedTypes())
        {
            if (requestType.IsAssignableFrom(exportedType) && typeName.Equals(exportedType.Name, StringComparison.OrdinalIgnoreCase))
                return exportedType;
        }

        return null;
    }
}