namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Specifies the privacy level that is required by the queue.
    /// </summary>
    public enum QueuePrivacyLevel : uint
    {
        /// <summary>The queue accepts only non-private (clear) messages.</summary>
        None,
        /// <summary>
        /// The default. The queue does not enforce privacy. 
        /// It accepts private (encrypted) messages and non-private (clear) messages.
        /// </summary>
        Optional,
        /// <summary>The queue accepts only private (encrypted) messages.</summary>
        Body,
    }

    /// <summary>
    /// Provides information about a queue.
    /// </summary>
    public readonly struct QueueInfo : IDisposable
    {
        private readonly QueueProperties properties;

        public QueueInfo()
        {
            this.properties = new QueueProperties();
        }

        internal QueueInfo(QueueProperties properties)
        {
            this.properties = properties;
        }

        internal QueueProperties Properties => this.properties;

        public Guid Instance => this.properties.GetValue<Guid>((int)MQ.PROPID.Q.INSTANCE);

        public Guid Type
        {
            get => this.properties.GetValue<Guid>((int)MQ.PROPID.Q.TYPE);
            set => this.properties.SetValue((int)MQ.PROPID.Q.TYPE, value);
        }

        public string PathName
        {
            get => this.properties.GetValue<string>((int)MQ.PROPID.Q.PATHNAME);
            set => this.properties.SetValue((int)MQ.PROPID.Q.PATHNAME, value);
        }

        public bool HasJournal
        {
            get => this.properties.GetValue<byte>((int)MQ.PROPID.Q.JOURNAL) != 0;
            set => this.properties.SetValue((int)MQ.PROPID.Q.JOURNAL, (byte)(value ? 1 : 0));
        }

        public long Quota
        {
            get => this.properties.GetValue<uint>((int)MQ.PROPID.Q.QUOTA) * 1024;
            set => this.properties.SetValue((int)MQ.PROPID.Q.QUOTA, (uint)(value / 1024));
        }

        public short BasePriority
        {
            get => (short)this.properties.GetValue<ushort>((int)MQ.PROPID.Q.BASEPRIORITY);
            set => this.properties.SetValue((int)MQ.PROPID.Q.BASEPRIORITY, (ushort)value);
        }

        public long JournalQuota
        {
            get => this.properties.GetValue<uint>((int)MQ.PROPID.Q.JOURNAL_QUOTA) * 1024;
            set => this.properties.SetValue((int)MQ.PROPID.Q.JOURNAL_QUOTA, (uint)(value / 1024));
        }

        public string Label
        {
            get => this.properties.GetValue<string>((int)MQ.PROPID.Q.LABEL);
            set => this.properties.SetValue((int)MQ.PROPID.Q.LABEL, value);
        }

        public DateTimeOffset CreateTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>((int)MQ.PROPID.Q.CREATE_TIME));
        }

        public DateTimeOffset ModifyTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>((int)MQ.PROPID.Q.MODIFY_TIME));
        }

        public bool AuthenticatedOnly
        {
            get => this.properties.GetValue<byte>((int)MQ.PROPID.Q.AUTHENTICATE) != 0;
            set => this.properties.SetValue((int)MQ.PROPID.Q.AUTHENTICATE, (byte)(value ? 1 : 0));
        }

        public QueuePrivacyLevel PrivacyLevel
        {
            get => (QueuePrivacyLevel)this.properties.GetValue<uint>((int)MQ.PROPID.Q.PRIV_LEVEL);
            set => this.properties.SetValue((int)MQ.PROPID.Q.PRIV_LEVEL, (uint)value);
        }

        public bool IsTransactional
        {
            get => this.properties.GetValue<byte>((int)MQ.PROPID.Q.TRANSACTION) != 0;
            set => this.properties.SetValue((int)MQ.PROPID.Q.TRANSACTION, (byte)(value ? 1 : 0));
        }

        public string PathNameDns
        {
            get => this.properties.GetValue<string>((int)MQ.PROPID.Q.PATHNAME_DNS);
        }

        public string MulticastAddress
        {
            get => this.properties.GetValue<string>((int)MQ.PROPID.Q.MULTICAST_ADDRESS);
            set => this.properties.SetValue((int)MQ.PROPID.Q.MULTICAST_ADDRESS, value);
        }

        public string AdsPath
        {
            get => this.properties.GetValue<string>((int)MQ.PROPID.Q.ADS_PATH);
        }

        public void Dispose()
        {
            this.properties?.Dispose();
        }
    }
}
