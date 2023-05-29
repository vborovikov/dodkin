namespace Dodkin
{
    using System.Net;
    using System.Text;
    using Interop;

    /// <summary>Represents a message queue.</summary>
    public abstract class MessageQueue : IDisposable
    {
        private const int DefaultQueueStorageSize = 20 * 1024 * 1024;

        /// <summary>Gets the name of the message queue.</summary>
        public abstract MessageQueueName Name { get; }

        internal abstract QueueHandle Handle { get; }

        /// <summary>Disposes the message queue.</summary>
        public abstract void Dispose();

        /// <summary>Tries to create a message queue.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="isTransactional">The flag indicating whether the queue is transactional.</param>
        /// <param name="hasJournal">The flag indicating whether the queue has a journal.</param>
        /// <param name="maxStorageSize">Maximum size of the queue, in bytes.</param>
        /// <param name="label">The label of the queue.</param>
        /// <param name="multicastAddress">The multicast address to attach to the queue.</param>
        /// <returns>
        /// <c>true</c> if the queue was created, <c>false</c> if the queue already exists or there was an error.
        /// </returns>
        public static bool TryCreate(MessageQueueName queueName, bool isTransactional = false, bool hasJournal = false,
            long maxStorageSize = DefaultQueueStorageSize, string? label = null, IPEndPoint? multicastAddress = null)
        {
            var result = CreateQueue(queueName, isTransactional, hasJournal, maxStorageSize, label, multicastAddress);
            return !MQ.IsFatalError(result);
        }

        /// <summary>Creates a message queue.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="isTransactional">The flag indicating whether the queue is transactional.</param>
        /// <param name="hasJournal">The flag indicating whether the queue has a journal.</param>
        /// <param name="maxStorageSize">Maximum size of the queue, in bytes.</param>
        /// <param name="label">The label of the queue.</param>
        /// <param name="multicastAddress">The multicast address to attach to the queue.</param>
        /// <exception cref="ArgumentNullException"><paramref name="queueName"/> is <c>null</c>.</exception>
        /// <exception cref="MessageQueueException">The queue already exists or there was an error.</exception>
        public static void Create(MessageQueueName queueName, bool isTransactional = false, bool hasJournal = false,
            long maxStorageSize = DefaultQueueStorageSize, string? label = null, IPEndPoint? multicastAddress = null)
        {
            ArgumentNullException.ThrowIfNull(queueName);
            var result = CreateQueue(queueName, isTransactional, hasJournal, maxStorageSize, label, multicastAddress);
            MessageQueueException.ThrowOnError(result);
        }

        /// <summary>
        /// Tries to delete an existing message queue.
        /// </summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <returns>
        /// <c>true</c> if the queue was deleted, <c>false</c> if the queue does not exists or there was an error.
        /// </returns>
        public static bool TryDelete(MessageQueueName queueName)
        {
            var result = MQ.DeleteQueue(queueName.FormatName);
            if (result == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            return !MQ.IsFatalError(result);
        }

        /// <summary>Deletes an existing message queue.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <exception cref="MessageQueueException">The queue does not exists or there was an error.</exception>
        public static void Delete(MessageQueueName queueName)
        {
            var result = MQ.DeleteQueue(queueName.FormatName);
            MessageQueueException.ThrowOnError(result);
        }

        /// <summary>Tests if a message queue exists.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <returns><c>true</c> if the queue exists, <c>false</c> otherwise.</returns>
        public static bool Exists(MessageQueueName queueName)
        {
            ArgumentNullException.ThrowIfNull(queueName);

            var formatNameBuilder = new StringBuilder(124);
            var formatNameLength = formatNameBuilder.Capacity;
            var result = MQ.PathNameToFormatName(queueName.PathName, formatNameBuilder, ref formatNameLength);
            if (result == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            return !MQ.IsFatalError(result);
        }

        /// <summary>Gets the management information of the specified machine.</summary>
        /// <param name="machine">The name of the machine.</param>
        /// <returns>The management information of the specified machine.</returns>
        public static MachineManagementInfo GetMachineInfo(string? machine = null)
        {
            using var packedProperties = new MachineManagementProperties().Pack();
            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, "MACHINE", packedProperties));
            return packedProperties.Unpack<MachineManagementProperties>();
        }

        /// <summary>Gets the management information of the specified queue.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <param name="machine">The name of the machine.</param>
        /// <returns>The management information of the specified queue.</returns>
        public static QueueManagementInfo GetQueueInfo(MessageQueueName queueName, string? machine = null)
        {
            ArgumentNullException.ThrowIfNull(queueName);

            using var packedProperties = new QueueManagementProperties().Pack();
            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, $"QUEUE={queueName.FormatName}", packedProperties));
            return packedProperties.Unpack<QueueManagementProperties>();
        }

        /// <summary>Purges the specified queue.</summary>
        /// <param name="queueName">The name of the queue.</param>
        /// <exception cref="MessageQueueException">The queue does not exists or there was an error.</exception>
        public static void Purge(MessageQueueName queueName)
        {
            using var queueReader = new MessageQueueReader(queueName);
            queueReader.Purge();
        }

        private static MQ.HR CreateQueue(MessageQueueName queueName, bool isTransactional, bool hasJournal,
            long maxStorageSize, string? label, IPEndPoint? multicastAddress)
        {
            if (queueName is null)
                return MQ.HR.ERROR_ILLEGAL_QUEUE_PATHNAME;

            using var queueInfo = new QueueInfo
            {
                PathName = queueName.PathName,
                IsTransactional = isTransactional,
                Quota = maxStorageSize,
            };
            if (hasJournal)
            {
                queueInfo.HasJournal = true;
                queueInfo.JournalQuota = maxStorageSize * 2;
            }
            if (!String.IsNullOrWhiteSpace(label))
                queueInfo.Label = label;
            if (multicastAddress is not null)
                queueInfo.MulticastAddress = multicastAddress.ToString();

            return CreateQueue(queueInfo, out var formatName);
        }

        private static MQ.HR CreateQueue(in QueueInfo queueInfo, out string formatName)
        {
            var formatNameBuilder = new StringBuilder(124);
            var formatNameLength = formatNameBuilder.Capacity;

            using var packedQueueProps = queueInfo.Properties.Pack();
            var result = MQ.CreateQueue(IntPtr.Zero, packedQueueProps, formatNameBuilder, ref formatNameLength);
            if (MQ.IsBufferOverflow(result))
            {
                formatNameBuilder.Capacity = formatNameLength;
                result = MQ.CreateQueue(IntPtr.Zero, packedQueueProps, formatNameBuilder, ref formatNameLength);
            }

            formatNameBuilder.Length = formatNameLength - 1;
            formatName = formatNameBuilder.ToString();
            return result;
        }

        private static string PathToFormatName(string queuePath)
        {
            var formatNameBuilder = new StringBuilder(124);
            var formatNameLength = formatNameBuilder.Capacity;

            var result = MQ.PathNameToFormatName(queuePath, formatNameBuilder, ref formatNameLength);
            if (MQ.IsBufferOverflow(result))
            {
                formatNameBuilder.Capacity = formatNameLength;
                result = MQ.PathNameToFormatName(queuePath, formatNameBuilder, ref formatNameLength);
            }

            MessageQueueException.ThrowIfNotOK(result);
            formatNameBuilder.Length = formatNameLength - 1;
            return formatNameBuilder.ToString();
        }
    }
}
