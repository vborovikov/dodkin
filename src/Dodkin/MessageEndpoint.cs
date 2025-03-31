namespace Dodkin;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Configuration settings for message queue listener.
/// </summary>
[TypeConverter(typeof(MessageEndpointConverter))]
public record MessageEndpoint
{
    /// <summary>
    /// The application queue for incoming messages.
    /// </summary>
    public required MessageQueueName ApplicationQueue { get; init; }

    /// <summary>
    /// The response queue for outgoing messages.
    /// </summary>
    public MessageQueueName? ResponseQueue { get; init; }
    
    /// <summary>
    /// The application subqueue for invalid messages.
    /// </summary>
    public MessageQueueName? InvalidMessageQueue { get; init; }

    /// <summary>
    /// The administration queue for ACK or NACK messages.
    /// </summary>
    public required MessageQueueName AdministrationQueue { get; init; }

    /// <summary>
    /// The dead-letter queue for rejected or failed to handle messages.
    /// </summary>
    public required MessageQueueName DeadLetterQueue { get; init; }

    /// <summary>
	/// Creates a new instance of <see cref="MessageEndpoint"/> from a queue name.
	/// </summary>
	/// <param name="queueName">The queue name.</param>
	/// <returns>A new instance of <see cref="MessageEndpoint"/>.</returns>
	public static MessageEndpoint FromName(string queueName)
    {
        return new MessageEndpoint
        {
            ApplicationQueue = new DirectFormatName(queueName, MessageQueueName.LocalComputer, isPrivate: true),
            ResponseQueue = new DirectFormatName(queueName + "-data", MessageQueueName.LocalComputer, isPrivate: true),
            InvalidMessageQueue = new DirectFormatName(queueName + ";invalid", MessageQueueName.LocalComputer, isPrivate: true),
            AdministrationQueue = new DirectFormatName(queueName + "-admin", MessageQueueName.LocalComputer, isPrivate: true),
            DeadLetterQueue = new DirectFormatName(queueName + "-dlq", MessageQueueName.LocalComputer, isPrivate: true),
        };
    }

	/// <summary>
	/// Creates the message queues if they do not exist.
	/// </summary>
	/// <param name="queueLabel">The queue label.</param>
	/// <param name="isTransactional">Indicates whether the application queue is transactional.</param>
	/// <exception cref="MessageQueueException">The message queue was not created.</exception>
    public void CreateIfNotExists(string queueLabel, bool isTransactional = false)
    {
        if (!MessageQueue.Exists(this.ApplicationQueue))
        {
            MessageQueue.Create(this.ApplicationQueue,
                isTransactional: isTransactional, hasJournal: true, label: queueLabel);
        }

        if (this.ResponseQueue is not null && !MessageQueue.Exists(this.ResponseQueue))
        {
            MessageQueue.Create(this.ResponseQueue,
                isTransactional: true, hasJournal: false, label: queueLabel);
        }

        if (!MessageQueue.Exists(this.AdministrationQueue))
        {
            MessageQueue.Create(this.AdministrationQueue,
                isTransactional: false, hasJournal: false, label: queueLabel);
        }

        if (!MessageQueue.Exists(this.DeadLetterQueue))
        {
            MessageQueue.Create(this.DeadLetterQueue,
                isTransactional: true, hasJournal: false, label: queueLabel);
        }
    }
}

/// <summary>
/// Provides a type converter to convert <see cref='MessageEndpoint'/> objects to and from various other representations.
/// </summary>
public sealed class MessageEndpointConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
    {
        return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
    }

    /// <inheritdoc/>
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string text)
        {
            text = text.Trim();
            if (text.Length == 0)
            {
                return null;
            }

            return MessageEndpoint.FromName(text);
        }

        return base.ConvertFrom(context, culture, value);
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (destinationType == typeof(string) && value is MessageEndpoint endpoint)
        {
            return endpoint.ToString();
        }

        return base.ConvertTo(context, culture, value, destinationType);
    }
}