namespace Dodkin
{
    using Interop;

    public enum QueuePrivacyLevel : uint
    {
        None,
        Optional,
        Body,
    }

    public readonly struct QueueInfo
    {
        private readonly QueueProperties properties;

        internal QueueInfo(QueueProperties properties)
        {
            this.properties = properties;
        }

        public Guid Instance => this.properties.GetValue<Guid>(MQ.PROPID.Q.INSTANCE);

        public Guid Type
        {
            get => this.properties.GetValue<Guid>(MQ.PROPID.Q.TYPE);
            set => this.properties.SetValue(MQ.PROPID.Q.TYPE, value);
        }

        public string PathName
        {
            get => this.properties.GetValue<string>(MQ.PROPID.Q.PATHNAME);
            set => this.properties.SetValue(MQ.PROPID.Q.PATHNAME, value);
        }

        public bool HasJournal
        {
            get => this.properties.GetValue<byte>(MQ.PROPID.Q.JOURNAL) != 0;
            set => this.properties.SetValue(MQ.PROPID.Q.JOURNAL, (byte)(value ? 1 : 0));
        }

        public long Quota
        {
            get => this.properties.GetValue<uint>(MQ.PROPID.Q.QUOTA) * 1024;
            set => this.properties.SetValue(MQ.PROPID.Q.QUOTA, (uint)(value / 1024));
        }

        public short BasePriority
        {
            get => (short)this.properties.GetValue<ushort>(MQ.PROPID.Q.BASEPRIORITY);
            set => this.properties.SetValue(MQ.PROPID.Q.QUOTA, (ushort)value);
        }

        public long JournalQuota
        {
            get => this.properties.GetValue<uint>(MQ.PROPID.Q.JOURNAL_QUOTA) * 1024;
            set => this.properties.SetValue(MQ.PROPID.Q.JOURNAL_QUOTA, (uint)(value / 1024));
        }

        public string Label
        {
            get => this.properties.GetValue<string>(MQ.PROPID.Q.LABEL);
            set => this.properties.SetValue(MQ.PROPID.Q.LABEL, value);
        }

        public DateTimeOffset CreateTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>(MQ.PROPID.Q.CREATE_TIME));
        }

        public DateTimeOffset ModifyTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>(MQ.PROPID.Q.MODIFY_TIME));
        }

        public bool AuthenticatedOnly
        {
            get => this.properties.GetValue<byte>(MQ.PROPID.Q.AUTHENTICATE) != 0;
            set => this.properties.SetValue(MQ.PROPID.Q.AUTHENTICATE, (byte)(value ? 1 : 0));
        }

        public QueuePrivacyLevel PrivacyLevel
        {
            get => (QueuePrivacyLevel)this.properties.GetValue<uint>(MQ.PROPID.Q.PRIV_LEVEL);
            set => this.properties.SetValue(MQ.PROPID.Q.AUTHENTICATE, (uint)value);
        }

        public bool IsTransactional
        {
            get => this.properties.GetValue<byte>(MQ.PROPID.Q.TRANSACTION) != 0;
            set => this.properties.SetValue(MQ.PROPID.Q.TRANSACTION, (byte)(value ? 1 : 0));
        }

        public string PathNameDns
        {
            get => this.properties.GetValue<string>(MQ.PROPID.Q.PATHNAME_DNS);
        }

        public string MulticastAddress
        {
            get => this.properties.GetValue<string>(MQ.PROPID.Q.MULTICAST_ADDRESS);
            set => this.properties.SetValue(MQ.PROPID.Q.MULTICAST_ADDRESS, value);
        }

        public string AdsPath
        {
            get => this.properties.GetValue<string>(MQ.PROPID.Q.ADS_PATH);
        }
    }
}
