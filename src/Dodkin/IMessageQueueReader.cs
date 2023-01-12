﻿namespace Dodkin
{
    public interface IMessageQueueReader
    {
        /// <summary>
        /// Reads the message at the current cursor location but does not remove it from the queue.
        /// The cursor remains pointing at the current message.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="transaction">A transaction object</param>
        /// <returns>The message, which can be default if the operation times out.</returns>
        Message Peek(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = default, QueueTransaction? transaction = null);

        /// <summary>
        /// Reads the message at the current cursor location but does not remove it from the queue.
        /// The cursor remains pointing at the current message.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the message received,
        /// which can be default if the operation times out.
        /// </returns>
        Task<Message> PeekAsync(MessageProperty properties, TimeSpan? timeout = default, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads the message at the current cursor location and removes it from the queue.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="transaction">A transaction object</param>
        /// <returns>The message, which can be default if the operation times out.</returns>
        Message Read(MessageProperty properties = MessageProperty.All, TimeSpan? timeout = default, QueueTransaction? transaction = null);

        /// <summary>
        /// Reads the message at the current cursor location and removes it from the queue.
        /// </summary>
        /// <param name="properties">The message properties to receive.</param>
        /// <param name="timeout">
        /// Time to wait for the message. This parameter can be set to <see cref="Timeout.InfiniteTimeSpan"/>,
        /// <see cref="TimeSpan.Zero"/>, or a specific amount of time. The default setting is <see cref="Timeout.InfiniteTimeSpan"/>.
        /// </param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>
        /// A task that represents the asynchronous read operation. The value of the <c>TResult</c> parameter contains the message received,
        /// which can be default if the operation times out.
        /// </returns>
        Task<Message> ReadAsync(MessageProperty properties, TimeSpan? timeout = default, CancellationToken cancellationToken = default);
    }
}