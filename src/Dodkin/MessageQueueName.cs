namespace Dodkin
{
    using System.Diagnostics.CodeAnalysis;

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

    /// <summary>
    /// Represents the message queue name, be it either a format name or a path name.
    /// </summary>
    public abstract class MessageQueueName : IEquatable<MessageQueueName>, IEquatable<string>,
        IParsable<MessageQueueName>, ISpanParsable<MessageQueueName>
    {
        private const string IllegalQueueNameChars = "=:\r\n+,\"";
        protected internal const string LocalComputer = ".";
        protected const string PrivateQueueMoniker = "private$";
        protected const string SystemQueueMoniker = "system$";

        public abstract FormatType Format { get; }

        public abstract string QueueName { get; }

        public abstract QueueType QueueType { get; }

        public abstract string FormatName { get; }

        public abstract string PathName { get; }

        public static implicit operator string(MessageQueueName queueName) => queueName.FormatName;

        public sealed override string ToString() => this.FormatName;

        public sealed override int GetHashCode()
        {
            return HashCode.Combine(this.Format, this.QueueName, this.QueueType, GetHashCodeOverride());
        }

        protected abstract int GetHashCodeOverride();

        public sealed override bool Equals(object? obj) => obj switch
        {
            MessageQueueName other => Equals(other),
            string str => Equals(str),
            _ => false
        };

        public bool Equals(MessageQueueName? other)
        {
            if (other is null)
                return false;

            return
                this.QueueType == other.QueueType &&
                this.Format == other.Format &&
                string.Equals(this.QueueName, other.QueueName, StringComparison.OrdinalIgnoreCase) &&
                EqualsOverride(other);
        }

        protected abstract bool EqualsOverride(MessageQueueName other);

        public bool Equals(string? other) => TryParse(other, out var otherName) && Equals(otherName);

        public static bool operator ==(MessageQueueName a, MessageQueueName b) => Equals(a, b);

        public static bool operator !=(MessageQueueName a, MessageQueueName b) => !Equals(a, b);

        public static bool Equals(MessageQueueName a, MessageQueueName b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (a is null) return false;
            if (b is null) return false;
            return a.Equals(b);
        }

        public static MessageQueueName Parse(ReadOnlySpan<char> s)
        {
            if (TryParse(s, out var result))
                return result;

            throw new FormatException();
        }

        public static bool TryParse(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out MessageQueueName result)
        {
            result = default;
            if (s.IsEmpty || s.IsWhiteSpace())
                return false;

            if (DirectFormatName.TryParse(s, out var directFormatName))
            {
                result = directFormatName;
                return true;
            }

            var pathParts = new PathEnumerator(s);
            if (!pathParts.MoveNext())
                return false;

            var computerName = pathParts.Current;
            if (computerName.IsEmpty)
                return false;
            if (computerName.Equals(PrivateQueueMoniker, StringComparison.OrdinalIgnoreCase))
            {
                computerName = LocalComputer;
            }
            else if (!pathParts.MoveNext())
            {
                // just one part, consider it a private queue name
                if (computerName is not ['.'] && computerName.IndexOfAny(IllegalQueueNameChars) < 0)
                {
                    result = new DirectFormatName(computerName.ToString(), LocalComputer, isPrivate: true);
                    return true;
                }

                return false;
            }

            var queueType = pathParts.Current;
            if (queueType.IsEmpty)
                return false;
            var isPrivate = pathParts.MoveNext() ? queueType.Equals(PrivateQueueMoniker, StringComparison.OrdinalIgnoreCase) : false;
            
            var queueName = pathParts.Current;
            if (queueName.IsEmpty || pathParts.MoveNext() || queueName.IndexOfAny(IllegalQueueNameChars) > 0)
                return false;

            result = new DirectFormatName(queueName.ToString(), computerName.ToString(), isPrivate);
            return true;
        }

        /// <inheritdoc />
        public static MessageQueueName Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider,
            [MaybeNullWhen(false)] out MessageQueueName result) => TryParse(s, out result);

        /// <inheritdoc />
        public static MessageQueueName Parse(string s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
            [MaybeNullWhen(false)] out MessageQueueName result) => TryParse(s, out result);

        private ref struct PathEnumerator
        {
            private ReadOnlySpan<char> span;
            private ReadOnlySpan<char> current;

            public PathEnumerator(ReadOnlySpan<char> span)
            {
                this.span = span;
                this.current = default;
            }

            public readonly ReadOnlySpan<char> Current => this.current;

            public PathEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                var remaining = this.span;
                if (remaining.IsEmpty)
                    return false;

                var pos = remaining.IndexOf('\\');
                if (pos > 0)
                {
                    this.current = remaining[..pos];
                    this.span = ++pos < remaining.Length ? remaining[pos..] : default;

                    return true;
                }

                this.current = remaining;
                this.span = default;
                return pos != 0;
            }
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
        private const string FormatMoniker = "DIRECT=";

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
                    return $@"{FormatMoniker}{this.protocol}:{this.address}\{PrivateQueueMoniker}\{this.QueueName}";

                var separator = this.protocol == ComputerAddressProtocol.HTTP || this.protocol == ComputerAddressProtocol.HTTPS ? '/' : '\\';
                return $"{FormatMoniker}{this.protocol}:{this.address}{separator}{this.QueueName}";
            }
        }

        public override string PathName
        {
            get
            {
                if (this.QueueType == QueueType.Private)
                    return $@"{this.address}\{PrivateQueueMoniker}\{this.QueueName}";

                if (this.protocol == ComputerAddressProtocol.HTTP || this.protocol == ComputerAddressProtocol.HTTPS)
                    return $"{this.protocol}:{this.address}/{this.QueueName}";

                return $@"{this.address}\{this.QueueName}";
            }
        }

        protected override int GetHashCodeOverride() => HashCode.Combine(this.protocol, this.address);

        protected override bool EqualsOverride(MessageQueueName other) =>
            other is DirectFormatName directFormatName && this.protocol == directFormatName.protocol &&
            string.Equals(this.address, directFormatName.address, StringComparison.OrdinalIgnoreCase);

        public static bool TryParse(ReadOnlySpan<char> formatName, [MaybeNullWhen(false)] out DirectFormatName result)
        {
            result = default;
            if (formatName.IsEmpty || formatName.IsWhiteSpace())
                return false;
            if (!formatName.StartsWith(FormatMoniker, StringComparison.OrdinalIgnoreCase))
                return false;

            formatName = formatName[FormatMoniker.Length..]; // skip "DIRECT="
            var separatorPos = formatName.IndexOf(':');
            if (separatorPos < 0)
                return false;

            var protocol = formatName[..separatorPos];
            var namePos = formatName.LastIndexOfAny("\\/");
            var address = ReadOnlySpan<char>.Empty;
            var queueName = ReadOnlySpan<char>.Empty;
            if (namePos < 0)
            {
                // just one part, consider it a queue name
                queueName = formatName[(separatorPos + 1)..];
            }
            else
            {
                address = formatName[(separatorPos + 1)..namePos];
                queueName = formatName[(namePos + 1)..];
            }
            if (queueName.IsEmpty)
                return false;

            var queueType = address.EndsWith(PrivateQueueMoniker, StringComparison.OrdinalIgnoreCase) ? QueueType.Private :
                queueName.StartsWith(SystemQueueMoniker, StringComparison.OrdinalIgnoreCase) ? QueueType.System : QueueType.Public;
            if (queueType == QueueType.Private)
            {
                // cut 'private$' part
                address = address[..^9];
            }

            var protocolParsed = Enum.Parse<ComputerAddressProtocol>(protocol, ignoreCase: true);
            var addressStr = address.IsEmpty ? LocalComputer : address.ToString();

            result = new DirectFormatName(queueName.ToString(), queueType, protocolParsed, addressStr);
            return true;
        }
    }
}
