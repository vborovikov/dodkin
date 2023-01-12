namespace Dodkin
{
    public enum QueueType
    {
        Private,
        Public,
        System,
    }

    public enum FormatType
    {
        Direct,
        Private,
        Public,
        DistributionList,
        Multicast,
        Machine,
    }

    public abstract class MessageQueueName
    {
        public abstract string QueueName { get; }

        public abstract QueueType QueueType { get; }

        public abstract string FormatName { get; }

        public abstract string PathName { get; }

        public static MessageQueueName FromPathName(string pathName)
        {
            var parts = pathName.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var isPrivate = parts.Length == 3 && String.Equals(parts[1], "private$", StringComparison.OrdinalIgnoreCase);
            var computerName = parts[0];
            if (computerName == ".")
                computerName = Environment.MachineName;

            return new DirectFormatName(parts[^1], computerName, isPrivate);
        }

        public static MessageQueueName FromFormatName(string formatName)
        {
            return null;
        }
    }

    public enum ComputerAddressProtocol
    {
        OS,
        HTTP,
        HTTPS,
        TCP,
        SPX,
    }

    sealed class DirectFormatName : MessageQueueName
    {
        private readonly ComputerAddressProtocol protocol;
        private readonly string address;

        public DirectFormatName(string queueName, string computerName, bool isPrivate)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(queueName);
            ArgumentNullException.ThrowIfNullOrEmpty(computerName);

            this.QueueName = queueName;
            this.protocol = ComputerAddressProtocol.OS;
            this.address = computerName;
            this.QueueType = isPrivate ? QueueType.Private : QueueType.Public;
        }

        public override string QueueName { get; }

        public override QueueType QueueType { get; }

        public override string FormatName
        {
            get
            {
                if (this.QueueType == QueueType.Private)
                    return $@"DIRECT={this.protocol}:{this.address}\PRIVATE$\{this.QueueName}";

                return $@"DIRECT={this.protocol}:{this.address}\{this.QueueName}";
            }
        }

        public override string PathName
        {
            get
            {
                if (this.QueueType == QueueType.Private)
                    return $@"{this.address}\PRIVATE$\{this.QueueName}";

                return $@"{this.address}\{this.QueueName}";
            }
        }

        public override string ToString() => this.FormatName;
    }
}
