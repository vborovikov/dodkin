namespace Dodkin.Dispatch;

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Relay.RequestModel;

public abstract class MessageOperator<TMessage> : IDisposable
    where TMessage : notnull, allows ref struct
{
    /// <summary>
    /// Provides the queue default operating timeout.
    /// </summary>
    protected static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);
    private readonly ConcurrentDictionary<string, Type> bodyTypeCache = new();
    private Assembly? interactionAssembly;

    /// <summary>
    /// Gets or sets the queue operating timeout.
    /// </summary>
    protected TimeSpan Timeout { get; init; }

    /// <summary>
    /// Gets the assembly that contains the recognized request types.
    /// </summary>
    private Assembly InteractionAssembly => this.interactionAssembly ??= Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    protected abstract void Dispose(bool disposing);

    /// <summary>
    /// Determines the type of the message body.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="bodyBaseType">The message body known base type.</param>
    /// <returns>The type of the message body or <c>null</c> if the body type could not be determined.</returns>
    protected Type? FindBodyType(in TMessage message, Type bodyBaseType)
    {
        var bodyTypeBuffer = GetSignature(message);
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
                FindBodyTypeByName(typeInfo.FullName, bodyBaseType);
        }
        if (bodyType is not null)
        {
            this.bodyTypeCache.TryAdd(bodyTypeName, bodyType);
            return bodyType;
        }

        return null;
    }

    protected abstract bool IsEmpty(in TMessage message);
    protected abstract ReadOnlySpan<byte> GetSignature(in TMessage message);
    protected abstract ReadOnlySpan<byte> GetBody(in TMessage message);

    /// <summary>
    /// Reads the body of a <see cref="Message"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of the message body.</typeparam>
    /// <param name="message">The message.</param>
    /// <returns>The message body.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    protected T Read<T>(in TMessage message)
    {
        if (IsEmpty(message))
            throw new InvalidOperationException("The received message is empty.");

        var bodyType = FindBodyType(message, typeof(T)) ??
            throw new InvalidOperationException("The message body type cannot be determined.");
        var body = JsonSerializer.Deserialize(GetBody(message), bodyType) ??
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
    protected bool TryRead<T>(in TMessage message, [MaybeNullWhen(false)] out T request)
        where T : notnull, IRequest
    {
        if (!IsEmpty(message))
        {
            var bodyType = FindBodyType(message, typeof(T));
            if (bodyType is not null)
            {
                var body = JsonSerializer.Deserialize(GetBody(message), bodyType);
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

    private Type? FindBodyTypeByName(ReadOnlySpan<char> typeFullName, Type bodyBaseType)
    {
        var nameStart = typeFullName.LastIndexOf('.');
        if (nameStart <= 0)
            return null;

        var typeName = typeFullName[(nameStart + 1)..];
        foreach (var exportedType in this.InteractionAssembly.GetExportedTypes())
        {
            if (bodyBaseType.IsAssignableFrom(exportedType) && typeName.Equals(exportedType.Name, StringComparison.OrdinalIgnoreCase))
                return exportedType;
        }

        return null;
    }
}