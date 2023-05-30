namespace Dodkin
{
    using System;
    using Interop;

    public readonly struct QueueManagementInfo : IDisposable
    {
        private const string YES = "YES";
        private const string NO = "NO";

        private readonly QueueManagementProperties properties;

        internal QueueManagementInfo(QueueManagementProperties properties)
        {
            this.properties = properties;
        }

        public string PathName => this.properties.GetValue<string>((int)MQ.PROPID.MGMT_QUEUE.PATHNAME);
        public string FormatName => this.properties.GetValue<string>((int)MQ.PROPID.MGMT_QUEUE.FORMATNAME);
        public string Type => this.properties.GetValue<string>((int)MQ.PROPID.MGMT_QUEUE.TYPE);
        public string Location => this.properties.GetValue<string>((int)MQ.PROPID.MGMT_QUEUE.LOCATION);
        public bool? IsTransactional => GetBoolean((int)MQ.PROPID.MGMT_QUEUE.XACT);
        public bool? IsForeign => GetBoolean((int)MQ.PROPID.MGMT_QUEUE.FOREIGN);
        public int MessageCount => (int)this.properties.GetValue<uint>((int)MQ.PROPID.MGMT_QUEUE.MESSAGE_COUNT);
        public int Size => (int)this.properties.GetValue<uint>((int)MQ.PROPID.MGMT_QUEUE.BYTES_IN_QUEUE);
        public int JournalMessageCount => (int)this.properties.GetValue<uint>((int)MQ.PROPID.MGMT_QUEUE.JOURNAL_MESSAGE_COUNT);
        public int JournalSize => (int)this.properties.GetValue<uint>((int)MQ.PROPID.MGMT_QUEUE.BYTES_IN_JOURNAL);
        public string State => this.properties.GetValue<string>((int)MQ.PROPID.MGMT_QUEUE.STATE);
        public string[] NextHops => this.properties.GetValue<string[]>((int)MQ.PROPID.MGMT_QUEUE.NEXTHOPS);

        public int SubqueueCount => (int)this.properties.GetValue<uint>((int)MQ.PROPID.MGMT_QUEUE.SUBQUEUE_COUNT);
        public string[] SubqueueNames => this.properties.GetValue<string[]>((int)MQ.PROPID.MGMT_QUEUE.SUBQUEUE_NAMES);

        public void Dispose()
        {
            this.properties?.Dispose();
        }

        private bool? GetBoolean(int propertyId)
        {
            var xact = this.properties.GetValue<string>(propertyId);

            return
                String.Equals(xact, NO, StringComparison.OrdinalIgnoreCase) ?
                String.Equals(xact, YES, StringComparison.OrdinalIgnoreCase) :
                null;
        }
    }
}
