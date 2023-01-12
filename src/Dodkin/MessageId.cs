namespace Dodkin
{
    using System.Collections;

    /// <summary>The message identifier</summary>
    public readonly struct MessageId : IEquatable<MessageId>
    {
        internal const int GenericIdSize = 16;
        internal const int Size = 20;

        private readonly byte[] bytes;

        public MessageId(byte[] id)
        {
            this.bytes = id;
        }

        public bool TryWriteBytes(Span<byte> destination)
        {
            if (this.bytes == destination)
                return true;

            if (this.bytes is null || this.bytes.Length > destination.Length)
                return false;

            this.bytes.CopyTo(destination);
            return true;
        }

        public byte[] ToByteArray()
        {
            return this.bytes;
        }

        /// <summary>Is this is null or all zeros?</summary>
        public bool IsNullOrEmpty()
        {
            if (this.bytes == null) return true;
            for (var i = 0; i < this.bytes.Length; i++)
            {
                if (this.bytes[i] != 0)
                    return false;
            }
            return true;
        }

        /// <summary>Returns Guid\long </summary>
        public override string ToString()
        {
            if (IsNullOrEmpty()) return "";

            var guidBytes = new byte[GenericIdSize];
            Array.Copy(this.bytes, guidBytes, GenericIdSize);
            var id = BitConverter.ToInt32(this.bytes, GenericIdSize);
            var guid = new Guid(guidBytes);
            return $"{guid}\\{id}";
        }

        public override int GetHashCode() =>
            StructuralComparisons.StructuralEqualityComparer.GetHashCode(this.bytes);

        public override bool Equals(object? obj) =>
            obj is MessageId msgId && Equals(msgId);

        public bool Equals(MessageId other) =>
            StructuralComparisons.StructuralEqualityComparer.Equals(this.bytes, other.bytes);

        public static bool operator ==(MessageId left, MessageId right) => left.Equals(right);

        public static bool operator !=(MessageId left, MessageId right) => !left.Equals(right);
    }
}
