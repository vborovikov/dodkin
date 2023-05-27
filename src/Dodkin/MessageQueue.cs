namespace Dodkin
{
    using System.Net;
    using System.Text;
    using Interop;

    public abstract class MessageQueue : IDisposable
    {
        public abstract MessageQueueName Name { get; }

        internal abstract QueueHandle Handle { get; }

        public abstract void Dispose();

        /// <summary>Creates a message queue (if it does not already exist).</summary>
        /// <param name="queueName">the name of the queue</param>
        /// <param name="isTransactional">create a transactional queue or not?</param>
        /// <param name="maxStorageSize">maximum size of the queue, in bytes</param>
        /// <param name="label">the label to add to the queue</param>
        /// <param name="multicastAddress">the multicast address to attach to the queue</param>
        /// <returns>the name of the queue</returns>
        public static MessageQueueName TryCreate(MessageQueueName queueName, bool isTransactional = false, long maxStorageSize = 20 * 1024 * 1024, string? label = null, IPEndPoint? multicastAddress = null)
        {
            ArgumentNullException.ThrowIfNull(queueName);

            using var queueInfo = new QueueInfo
            {
                PathName = queueName.PathName,
                IsTransactional = isTransactional,
                Quota = maxStorageSize,
            };
            if (!String.IsNullOrWhiteSpace(label))
                queueInfo.Label = label;
            if (multicastAddress is not null)
                queueInfo.MulticastAddress = multicastAddress.ToString();

            using var packedQueueProps = queueInfo.Properties.Pack();
            var result = CreateQueue(queueInfo, out var formatName);
            if (result == MQ.HR.ERROR_QUEUE_EXISTS)
                return MessageQueueName.Parse(PathToFormatName(queueName.PathName));

            MessageQueueException.ThrowOnError(result);
            return MessageQueueName.Parse(formatName);
        }

        /// <summary>
        /// Tries to delete an existing message queue.
        /// </summary>
        /// <param name="queueName">The name of the queue</param>
        /// <returns><c>true</c> if the queue was deleted, <c>false</c> if the queue does not exists</returns>
        public static bool TryDelete(MessageQueueName queueName)
        {
            var result = MQ.DeleteQueue(queueName.FormatName);
            if (result == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            MessageQueueException.ThrowOnError(result);
            return true;
        }

        /// <summary>Tests if a queue exists.</summary>
        public static bool Exists(MessageQueueName queueName)
        {
            ArgumentNullException.ThrowIfNull(queueName);

            var formatNameBuilder = new StringBuilder(124);
            var formatNameLength = formatNameBuilder.Capacity;
            var result = MQ.PathNameToFormatName(queueName.PathName, formatNameBuilder, ref formatNameLength);
            if (result == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            MessageQueueException.ThrowIfNotOK(result);
            return true;
        }

        public static MachineManagementInfo GetMachineInfo(string? machine = null)
        {
            using var packedProperties = new MachineManagementProperties().Pack();
            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, "MACHINE", packedProperties));
            return packedProperties.Unpack<MachineManagementProperties>();
        }

        public static QueueManagementInfo GetQueueInfo(MessageQueueName queueName, string? machine = null)
        {
            ArgumentNullException.ThrowIfNull(queueName);

            using var packedProperties = new QueueManagementProperties().Pack();
            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, $"QUEUE={queueName.FormatName}", packedProperties));
            return packedProperties.Unpack<QueueManagementProperties>();
        }

        public static void Purge(MessageQueueName queueName)
        {
            using var queueReader = new MessageQueueReader(queueName);
            queueReader.Purge();
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
