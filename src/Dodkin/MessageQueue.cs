namespace Dodkin
{
    using System.Diagnostics.Contracts;
    using System.Net;
    using System.Text;
    using Interop;

    public abstract class MessageQueue : IDisposable
    {
        public abstract string FormatName { get; }

        internal abstract QueueHandle Handle { get; }

        public abstract void Dispose();

        /// <summary>Creates a message queue (if it does not already exist), returning the format name of the queue.</summary>
        /// <param name="path">the path (NOT format name) of the queue</param>
        /// <param name="isTransactional">create a transactional queue or not?</param>
        /// <param name="quotaKB">Maximum size of the queue, in KB, defaults to 20MB</param>
        /// <param name="label">the label to add to the queue</param>
        /// <param name="multicast">the multicast address to attach to the queue</param>
        public static string TryCreate(string path, bool isTransactional, int quotaKB = 20000, string? label = null, IPEndPoint? multicast = null)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(path));
            Contract.Requires(label == null || label.Length < 125);
            Contract.Ensures(Contract.Result<string>() != null);

            const int MaxLabelLength = 124;

            //Create properties.
            using var properties = new QueueProperties();
            properties.SetValue(MQ.PROPID.Q.PATHNAME, path);
            properties.SetValue(MQ.PROPID.Q.TRANSACTION, (byte)(isTransactional ? 1 : 0));
            properties.SetValue(MQ.PROPID.Q.QUOTA, quotaKB);
            if (label != null)
                properties.SetValue(MQ.PROPID.Q.LABEL, label);
            if (multicast != null)
                properties.SetValue(MQ.PROPID.Q.MULTICAST_ADDRESS, $"{multicast.Address}:{multicast.Port}");

            var formatName = new StringBuilder(MaxLabelLength);
            var len = MaxLabelLength;

            //Try to create queue.
            using var packedProps = properties.Pack();
            var res = MQ.CreateQueue(IntPtr.Zero, packedProps, formatName, ref len);
            packedProps.Dispose();

            if (res == MQ.HR.ERROR_QUEUE_EXISTS)
                return PathToFormatName(path);

            if (MQ.IsFatalError(res))
                throw new MessageQueueException(res);

            formatName.Length = len;
            return formatName.ToString();
        }

        /// <summary>Tries to delete an existing message queue, returns TRUE if the queue was deleted, FALSE if the queue does not exists</summary>
        /// <param name="formatName">The format name (NOT path name) of the queue</param>
        public static bool TryDelete(string formatName)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(formatName));

            var res = MQ.DeleteQueue(formatName);

            if (res == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            if (MQ.IsFatalError(res))
                throw new MessageQueueException(res);

            return true;
        }

        /// <summary>converts a queue path to a format name</summary>
        public static string PathToFormatName(string queuePath)
        {
            var formatNameBuilder = new StringBuilder();
            var formatNameLength = formatNameBuilder.Capacity;

            var hr = MQ.PathNameToFormatName(queuePath, formatNameBuilder, ref formatNameLength);
            if (MQ.IsBufferOverflow(hr))
            {
                formatNameBuilder.Capacity = formatNameLength;
                hr = MQ.PathNameToFormatName(queuePath, formatNameBuilder, ref formatNameLength);
            }
            MessageQueueException.ThrowIfNotOK(hr);

            formatNameBuilder.Length = formatNameLength - 1;
            return formatNameBuilder.ToString();
        }

        /// <summary>Tests if a queue existing. Does NOT accept format names</summary>
        public static bool Exists(string path)
        {
            ArgumentNullException.ThrowIfNull(path);

            var size = 255;
            var sb = new StringBuilder(size);
            var res = MQ.PathNameToFormatName(path, sb, ref size);
            if (res == MQ.HR.ERROR_QUEUE_NOT_FOUND)
                return false;

            MessageQueueException.ThrowIfNotOK(res);

            return true;
        }

        public static MachineManagementInfo GetMachineInfo(string? machine = null)
        {
            using var packedProperties = new MachineManagementProperties().Pack();

            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, "MACHINE", packedProperties));

            return packedProperties.Unpack<MachineManagementProperties>();
        }

        public static QueueManagementInfo GetQueueInfo(string formatName, string? machine = null)
        {
            ArgumentNullException.ThrowIfNull(formatName);

            using var packedProperties = new QueueManagementProperties().Pack();

            MessageQueueException.ThrowOnError(MQ.MgmtGetInfo(machine, $"QUEUE={formatName}", packedProperties));

            return packedProperties.Unpack<QueueManagementProperties>();
        }

        /// <summary>Returns the transactional property of the queue</summary>
        public static void Purge(MessageQueueName queueName)
        {
            using (var q = new MessageQueueReader(queueName))
                q.Purge();
        }
    }
}
