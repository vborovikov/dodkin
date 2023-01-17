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
        public abstract FormatType Format { get; }

        public abstract string QueueName { get; }

        public abstract QueueType QueueType { get; }

        public abstract string FormatName { get; }

        public abstract string PathName { get; }

        public static MessageQueueName FromPathName(string pathName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(pathName);

            var parts = pathName.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            var isPrivate = parts.Length == 3 && String.Equals(parts[1], "private$", StringComparison.OrdinalIgnoreCase);
            var computerName = parts[0];
            if (computerName == ".")
                computerName = Environment.MachineName;

            return new DirectFormatName(parts[^1], computerName, isPrivate);
        }

        public static MessageQueueName FromFormatName(string formatName)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(formatName);

            if (formatName.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
                return DirectFormatName.Parse(formatName);

            throw new NotSupportedException();
        }
    }

    public enum ComputerAddressProtocol
    {
        OS,
        HTTP,
        HTTPS,
        TCP,
        IPX,
    }

    sealed class DirectFormatName : MessageQueueName
    {
        private readonly ComputerAddressProtocol protocol;
        private readonly string address;

        internal DirectFormatName(string queueName, string computerName, bool isPrivate)
            : this(queueName, isPrivate ? QueueType.Private : QueueType.Public, ComputerAddressProtocol.OS, computerName)
        { }

        internal DirectFormatName(string queueName, QueueType queueType, ComputerAddressProtocol protocol, string address)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(queueName);
            ArgumentNullException.ThrowIfNullOrEmpty(address);

            this.QueueName = queueName;
            this.QueueType = queueType;
            this.protocol = protocol;
            this.address = address;
        }

        public override FormatType Format => FormatType.Direct;

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

        public static DirectFormatName Parse(ReadOnlySpan<char> formatName)
        {
            if (formatName.IsEmpty || formatName.IsWhiteSpace())
                throw new ArgumentNullException(nameof(formatName));
            if (!formatName.StartsWith("DIRECT=", StringComparison.OrdinalIgnoreCase))
                throw new FormatException();

            formatName = formatName[7..]; // skip "DIRECT="
            var separatorPos = formatName.IndexOf(':');
            if (separatorPos < 0)
                throw new FormatException();

            var protocol = formatName[..separatorPos];
            var address = formatName[(separatorPos + 1)..formatName.IndexOf('\\')];
            var queueType = formatName.Contains("private$", StringComparison.OrdinalIgnoreCase) ? QueueType.Private :
                formatName.Contains("system$", StringComparison.OrdinalIgnoreCase) ? QueueType.System : QueueType.Public;
            var queueName = formatName[(formatName.LastIndexOfAny("\\/") + 1)..];

            var protocolParsed = Enum.Parse<ComputerAddressProtocol>(protocol, ignoreCase: true);
            var addressStr = address.Equals(".", StringComparison.OrdinalIgnoreCase) ? Environment.MachineName : address.ToString();

            return new DirectFormatName(queueName.ToString(), queueType, protocolParsed, addressStr);
        }
    }
}
