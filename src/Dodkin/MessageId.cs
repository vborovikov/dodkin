namespace Dodkin
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary>The message identifier</summary>
    public readonly struct MessageId : IEquatable<MessageId>, IParsable<MessageId>, ISpanParsable<MessageId>
    {
        internal const int Size = 20;

        private readonly Guid guid;
        private readonly uint id;

        public MessageId(ReadOnlySpan<byte> bytes)
            : this(new Guid(bytes[0..16]), BitConverter.ToUInt32(bytes[16..]))
        {
        }

        private MessageId(Guid guid, uint id)
        {
            this.guid = guid;
            this.id = id;
        }

        public static MessageId Parse(ReadOnlySpan<char> span)
        {
            if (TryParse(span, out var messageId))
                return messageId;

            throw new FormatException();
        }

        public static bool TryParse(ReadOnlySpan<char> span, out MessageId messageId)
        {
            messageId = default;
            if (span.IsEmpty)
                return false;

            var separatorPos = span.IndexOf('\\');
            if (separatorPos <= 0)
                return false;

            if (Guid.TryParse(span[..separatorPos], out var guid) && 
                UInt32.TryParse(span[(separatorPos + 1)..], out var id))
            {
                messageId = new MessageId(guid, id);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public static MessageId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider,
            [MaybeNullWhen(false)] out MessageId result) => TryParse(s, out result);

        /// <inheritdoc />
        public static MessageId Parse(string s, IFormatProvider? provider) => Parse(s);

        /// <inheritdoc />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider,
            [MaybeNullWhen(false)] out MessageId result) => TryParse(s, out result);

        public bool TryWriteBytes(Span<byte> destination)
        {
            if (destination.Length < Size)
                return false;
            
            return this.guid.TryWriteBytes(destination[0..16]) &&
                BitConverter.TryWriteBytes(destination[16..], this.id);
        }

        public byte[] ToByteArray()
        {
            var bytes = new byte[Size];
            TryWriteBytes(bytes);
            return bytes;
        }

        public override string ToString()
        {
            if (this.guid == default && this.id == default)
                return String.Empty;

            return $"{this.guid}\\{this.id}";
        }

        public override int GetHashCode() => HashCode.Combine(this.guid, this.id);

        public override bool Equals(object? obj) => obj is MessageId msgId && Equals(msgId);

        public bool Equals(MessageId other) => this.guid.Equals(other.guid) && this.id.Equals(other.id);

        public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);

        public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right);
    }
}
