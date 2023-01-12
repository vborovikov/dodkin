namespace Dodkin
{
    using Interop;

    public readonly struct Message
    {
        private readonly MessageProperties properties;

        public Message() : this(new MessageProperties(MessageProperty.MessageId))
        {
        }

        public Message(byte[] body)
            : this()
        {
            this.properties.SetArray(MQ.PROPID.M.BODY, MQ.PROPID.M.BODY_SIZE, body);
        }

        public Message(byte[] body, byte[] extension)
            : this(body)
        {
            this.properties.SetArray(MQ.PROPID.M.EXTENSION, MQ.PROPID.M.EXTENSION_LEN, extension);
        }

        internal Message(MessageProperties properties)
        {
            ArgumentNullException.ThrowIfNull(properties);
            this.properties = properties;
        }

        internal MessageProperties Properties => this.properties;

        public bool IsEmpty => this.properties is null;

        public MessageClass Class => (MessageClass)this.properties.GetValue<ushort>(MQ.PROPID.M.CLASS);

        public MessageId Id => this.properties.GetValue<MessageId>(MQ.PROPID.M.MSGID);

        public MessageId CorrelationId
        {
            get => this.properties.GetValue<MessageId>(MQ.PROPID.M.CORRELATIONID);
            set => this.properties.SetValue(MQ.PROPID.M.CORRELATIONID, value);
        }

        public Priority Priority
        {
            get => (Priority)this.properties.GetValue<byte>(MQ.PROPID.M.PRIORITY);
            set => this.properties.SetValue(MQ.PROPID.M.PRIORITY, (byte)value);
        }

        public Delivery Delivery
        {
            get => (Delivery)this.properties.GetValue<byte>(MQ.PROPID.M.DELIVERY);
            set => this.properties.SetValue(MQ.PROPID.M.DELIVERY, (byte)value);
        }

        public AcknowledgmentType Acknowledgment
        {
            get => (AcknowledgmentType)this.properties.GetValue<byte>(MQ.PROPID.M.ACKNOWLEDGE);
            set => this.properties.SetValue(MQ.PROPID.M.ACKNOWLEDGE, (byte)value);
        }

        public Journal Journal
        {
            get => (Journal)this.properties.GetValue<byte>(MQ.PROPID.M.JOURNAL);
            set => this.properties.SetValue(MQ.PROPID.M.JOURNAL, (byte)value);
        }

        public uint AppSpecific
        {
            get => this.properties.GetValue<uint>(MQ.PROPID.M.APPSPECIFIC);
            set => this.properties.SetValue(MQ.PROPID.M.APPSPECIFIC, value);
        }

        public ReadOnlySpan<byte> Body => this.properties.GetArray(MQ.PROPID.M.BODY, MQ.PROPID.M.BODY_SIZE);

        public string Label
        {
            get => this.properties.GetString(MQ.PROPID.M.LABEL, MQ.PROPID.M.LABEL_LEN);
            set => this.properties.SetString(MQ.PROPID.M.LABEL, MQ.PROPID.M.LABEL_LEN, value);
        }

        public TimeSpan TimeToReachQueue
        {
            get => TimeSpan.FromSeconds(this.properties.GetValue<uint>(MQ.PROPID.M.TIME_TO_REACH_QUEUE));
            set => this.properties.SetValue(MQ.PROPID.M.TIME_TO_REACH_QUEUE, (uint)value.TotalSeconds);
        }

        public TimeSpan TimeToBeReceived
        {
            get => TimeSpan.FromSeconds(this.properties.GetValue<uint>(MQ.PROPID.M.TIME_TO_BE_RECEIVED));
            set => this.properties.SetValue(MQ.PROPID.M.TIME_TO_BE_RECEIVED, (uint)value.TotalSeconds);
        }

        public string ResponseQueue
        {
            get => this.properties.GetString(MQ.PROPID.M.RESP_QUEUE, MQ.PROPID.M.RESP_QUEUE_LEN);
            set => this.properties.SetString(MQ.PROPID.M.RESP_QUEUE, MQ.PROPID.M.RESP_QUEUE_LEN, value);
        }

        public string AdministrationQueue
        {
            get => this.properties.GetString(MQ.PROPID.M.ADMIN_QUEUE, MQ.PROPID.M.ADMIN_QUEUE_LEN);
            set => this.properties.SetString(MQ.PROPID.M.ADMIN_QUEUE, MQ.PROPID.M.ADMIN_QUEUE_LEN, value);
        }

        public SenderIdType SenderIdType
        {
            get => (SenderIdType)this.properties.GetValue<uint>(MQ.PROPID.M.SENDERID_TYPE);
            set => this.properties.SetValue(MQ.PROPID.M.SENDERID_TYPE, (uint)value);
        }

        public DateTimeOffset SentTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>(MQ.PROPID.M.SENTTIME));
        }

        public DateTimeOffset ArrivedTime
        {
            get => DateTimeOffset.FromUnixTimeSeconds(this.properties.GetValue<uint>(MQ.PROPID.M.ARRIVEDTIME));
        }

        public string DestinationQueue => this.properties.GetString(MQ.PROPID.M.DEST_QUEUE, MQ.PROPID.M.DEST_QUEUE_LEN);

        public ReadOnlySpan<byte> Extension => this.properties.GetArray(MQ.PROPID.M.EXTENSION, MQ.PROPID.M.EXTENSION_LEN);

        public BodyType BodyType
        {
            get => (BodyType)this.properties.GetValue<uint>(MQ.PROPID.M.BODY_TYPE);
            set => this.properties.SetValue(MQ.PROPID.M.BODY_TYPE, (uint)value);
        }

        public bool TransactionFirst => this.properties.GetValue<byte>(MQ.PROPID.M.FIRST_IN_XACT) == 1;

        public bool TransactionLast => this.properties.GetValue<byte>(MQ.PROPID.M.LAST_IN_XACT) == 1;

        public MessageId TransactionId => this.properties.GetValue<MessageId>(MQ.PROPID.M.XACTID);

        public ulong LookupId => this.properties.GetValue<ulong>(MQ.PROPID.M.LOOKUPID);

        public uint TransactionAbortCount => this.properties.GetValue<uint>(MQ.PROPID.M.ABORT_COUNT);

        public uint TransactionMoveCount => this.properties.GetValue<uint>(MQ.PROPID.M.MOVE_COUNT);

        public void Dispose()
        {
            this.properties.Dispose();
        }
    }
}