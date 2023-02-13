namespace Dodkin
{
    using System;
    using Interop;

    public readonly struct MachineManagementInfo : IDisposable
    {
        private const string MSMQ_CONNECTED = "CONNECTED";

        private readonly MachineManagementProperties properties;

        internal MachineManagementInfo(MachineManagementProperties properties)
        {
            this.properties = properties;
        }

        public string[] ActiveQueueFormatNames => this.properties.GetValue<string[]>(MQ.PROPID.MGMT_MSMQ.ACTIVEQUEUES);
        public string[] PrivateQueuePathNames => this.properties.GetValue<string[]>(MQ.PROPID.MGMT_MSMQ.PRIVATEQ);
        public string DirectoryService => this.properties.GetValue<string>(MQ.PROPID.MGMT_MSMQ.DSSERVER);
        public bool IsConnected => String.Equals(this.properties.GetValue<string>(MQ.PROPID.MGMT_MSMQ.CONNECTED),
                MSMQ_CONNECTED, StringComparison.OrdinalIgnoreCase);
        public string Type => this.properties.GetValue<string>(MQ.PROPID.MGMT_MSMQ.TYPE);
        public long QueueSize => (long)this.properties.GetValue<ulong>(MQ.PROPID.MGMT_MSMQ.BYTES_IN_ALL_QUEUES);

        public void Dispose()
        {
            this.properties.Dispose();
        }
    }
}
