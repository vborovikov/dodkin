﻿namespace Dodkin
{
    using Interop;

    /// <summary>
    /// Specifies the priority of the message.
    /// </summary>
    public enum MessagePriority : byte
    {
        Lowest = 0,
        VeryLow = 1,
        Low = 2,
        Normal = 3,
        AboveNormal = 4,
        High = 5,
        VeryHigh = 6,
        Highest = 7,
    }

    /// <summary>
    /// Specifies how Message Queuing delivers the message to the queue.
    /// </summary>
    public enum MessageDelivery : byte
    {
        /// <summary>
        /// The default. The message stays in volatile memory along its entire route until it is received.
        /// The message is not recovered if the computer where the message resides is rebooted. 
        /// An express message delivered to a queue on a virtual server in a cluster will be lost
        /// if the virtual server fails over before the message is received.
        /// </summary>
        Express = 0,

        /// <summary>
        /// In every hop along its route, the message is stored locally on disk until it is forwarded to the next computer.
        /// This guarantees delivery even in case of a computer crash. When the message is placed in the destination queue,
        /// it is written to disk in a memory-mapped file.
        /// </summary>
        Recoverable = 1,
    }

    /// <summary>
    /// Specifies whether Message Queuing stores copies of the message as it is routed to the destination queue.
    /// </summary>
    [Flags]
    public enum MessageJournaling : byte
    {
        /// <summary>
        /// The default. Source journaling is disabled. 
        /// Message Queuing does not store copies of the message in the computer journal on success
        /// nor in the applicable dead-letter queue on failure.
        /// </summary>
        None = 0,

        /// <summary>Negative source journaling is requested. 
        /// The message is stored in the applicable dead-letter queue on failure,
        /// but no copy is stored in the computer journal on success.</summary>
        DeadLetter = 1,

        /// <summary>
        /// Positive source journaling is requested. 
        /// A copy of the message is stored in the computer journal on the computer
        /// if the message was successfully delivered to the next computer, 
        /// but no copy is stored in the applicable dead-letter queue on failure.
        /// </summary>
        Journal = 2,
    }

    /// <summary>
    /// Specifies the type of sender identifier.
    /// </summary>
    public enum MessageSenderIdType : uint
    {
        /// <summary>
        /// No identifier is attached to the message.
        /// </summary>
        None = 0,

        /// <summary>
        /// The default. <see cref="Message.SenderId"/> contains the security identifier (SID) of the user sending the message.
        /// </summary>
        Sid = 1,
    }

    /// <summary>
    /// Indicates the format of the message body.
    /// </summary>
    public enum MessageBodyType : uint
    {
        /// <summary>
        /// Default. Note that the Message Queuing COM implementation treats this value as an array of bytes.
        /// </summary>
        Default = 0,

        /// <summary>
        /// A variable-size, NULL-terminated ANSI string.
        /// </summary>
        AnsiString = VarType.AnsiString,

        /// <summary>
        /// A variable-size, NULL-terminated Unicode string.
        /// </summary>
        UnicodeString = VarType.String,

        /// <summary>
        /// An array of bytes.
        /// </summary>
        ByteArray = VarType.ByteArray,
    }

    /// <summary>
    /// Specifies the type of acknowledgment messages that Message Queuing will post 
    /// (in the administration queue) when acknowledgments are requested.
    /// </summary>
    [Flags]
    public enum MessageAcknowledgment : byte
    {
        /// <summary>
        /// The default. No acknowledgment messages (positive or negative) are posted.
        /// </summary>
        None = 0,
        /// <summary>
        /// The positive arrival flag.
        /// </summary>
        PositiveArrival = 1,
        /// <summary>
        /// The positive receive flag
        /// </summary>
        PositiveReceive = 2,
        /// <summary>
        /// The negative arrival flag.
        /// </summary>
        NegativeArrival = 4,
        /// <summary>
        /// The negative receive flag.
        /// </summary>
        NegativeReceive = 8,
        /// <summary>
        /// Posts a negative acknowledgment when the message cannot reach the queue.
        /// </summary>
        NackReachQueue = NegativeArrival,
        /// <summary>
        /// Posts a positive or negative acknowledgment depending on whether or not the message reaches the queue.
        /// </summary>
        FullReachQueue = NegativeArrival | PositiveArrival,
        /// <summary>
        /// Posts a negative acknowledgment when the message cannot be retrieved from the queue 
        /// before the message's time-to-be-received timer expires.
        /// </summary>
        NackReceive = NegativeReceive | NegativeArrival,
        /// <summary>
        /// Posts a positive or negative acknowledgment depending on whether or not the message
        /// is retrieved from the queue before its time-to-be-received timer expires.
        /// </summary>
        FullReceive = PositiveReceive | NegativeReceive,
    }

    /// <summary>
    /// Describes a set of message properties.
    /// </summary>
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

        public MessageClass Class => new(this.properties.GetValue<ushort>(MQ.PROPID.M.CLASS));

        public MessageId Id => this.properties.GetValue<MessageId>(MQ.PROPID.M.MSGID);

        public MessageId CorrelationId
        {
            get => this.properties.GetValue<MessageId>(MQ.PROPID.M.CORRELATIONID);
            set => this.properties.SetValue(MQ.PROPID.M.CORRELATIONID, value);
        }

        public MessagePriority Priority
        {
            get => (MessagePriority)this.properties.GetValue<byte>(MQ.PROPID.M.PRIORITY);
            set => this.properties.SetValue(MQ.PROPID.M.PRIORITY, (byte)value);
        }

        public MessageDelivery Delivery
        {
            get => (MessageDelivery)this.properties.GetValue<byte>(MQ.PROPID.M.DELIVERY);
            set => this.properties.SetValue(MQ.PROPID.M.DELIVERY, (byte)value);
        }

        public MessageAcknowledgment Acknowledgment
        {
            get => (MessageAcknowledgment)this.properties.GetValue<byte>(MQ.PROPID.M.ACKNOWLEDGE);
            set => this.properties.SetValue(MQ.PROPID.M.ACKNOWLEDGE, (byte)value);
        }

        public MessageJournaling Journal
        {
            get => (MessageJournaling)this.properties.GetValue<byte>(MQ.PROPID.M.JOURNAL);
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

        public MessageSenderIdType SenderIdType
        {
            get => (MessageSenderIdType)this.properties.GetValue<uint>(MQ.PROPID.M.SENDERID_TYPE);
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

        public MessageBodyType BodyType
        {
            get => (MessageBodyType)this.properties.GetValue<uint>(MQ.PROPID.M.BODY_TYPE);
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