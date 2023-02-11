namespace Dodkin.Interop
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Text.Json;

    abstract class PropertyBag : IDisposable
    {
        protected abstract class PropertyBox
        {
            private readonly IStrongBox box;

            protected PropertyBox(IStrongBox box)
            {
                this.box = box;
            }

            protected IStrongBox Box => this.box;

            public abstract MQPROPVARIANT Export(ref GCHandle handle);

            public abstract void Import(in MQPROPVARIANT variant, MQ.HR status);

            public virtual void Adjust(int size) { }

            public int Read(ref Utf8JsonReader reader) => reader.Read() ? ReadOverride(ref reader) : 0;

            public void Write(Utf8JsonWriter writer) => WriteOverride(writer);

            protected abstract int ReadOverride(ref Utf8JsonReader reader);

            protected abstract void WriteOverride(Utf8JsonWriter writer);
        }

        private abstract class PropertyBox<T> : PropertyBox
        {
            protected PropertyBox(IStrongBox box) : base(box) { }

            public virtual T Value
            {
                get => ((StrongBox<T>)this.Box).Value!;
                set => ((StrongBox<T>)this.Box).Value = value;
            }
        }

        private abstract class StructPropertyBox<T> : PropertyBox<T>
            where T : struct, IEquatable<T>
        {
            protected StructPropertyBox(T value) : base(new StrongBox<T>(value)) { }
        }

        private sealed class BytePropertyBox : StructPropertyBox<byte>
        {
            public BytePropertyBox(byte value) : base(value) { }

            public override MQPROPVARIANT Export(ref GCHandle handle) => new()
            {
                vt = (ushort)VarType.Byte,
                bVal = this.Value,
            };

            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                this.Value = variant.bVal;
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                this.Value = reader.GetByte();
                return 1;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteNumberValue(this.Value);
            }
        }

        private sealed class ShortPropertyBox : StructPropertyBox<ushort>
        {
            public ShortPropertyBox(ushort value) : base(value) { }

            public override MQPROPVARIANT Export(ref GCHandle handle) => new()
            {
                vt = (ushort)VarType.UShort,
                uiVal = this.Value,
            };

            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                this.Value = variant.uiVal;
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                this.Value = reader.GetUInt16();
                return 2;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteNumberValue(this.Value);
            }
        }

        private sealed class IntPropertyBox : StructPropertyBox<uint>
        {
            public IntPropertyBox() : this(0u) { }

            public IntPropertyBox(uint value) : base(value) { }

            public override MQPROPVARIANT Export(ref GCHandle handle) => new()
            {
                vt = (ushort)VarType.UInt,
                ulVal = this.Value,
            };

            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                this.Value = variant.ulVal;
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                this.Value = reader.GetUInt32();
                return 4;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteNumberValue(this.Value);
            }
        }

        private sealed class LongPropertyBox : StructPropertyBox<ulong>
        {
            public LongPropertyBox(ulong value) : base(value) { }

            public override MQPROPVARIANT Export(ref GCHandle handle) => new()
            {
                vt = (ushort)VarType.ULong,
                uhVal = this.Value,
            };

            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                this.Value = variant.uhVal;
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                this.Value = reader.GetUInt64();
                return 8;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteNumberValue(this.Value);
            }
        }

        private sealed class ArrayPropertyBox : PropertyBox<byte[]>
        {
            public ArrayPropertyBox() : this(Array.Empty<byte>()) { }

            public ArrayPropertyBox(byte[] buffer) : base(new StrongBox<byte[]>(buffer)) { }

            public ReadOnlySpan<byte> GetValue(int length)
            {
                var byteArray = this.Value;
                if (byteArray is not null && byteArray.Length > 0 &&
                    length > 0 && length <= byteArray.Length)
                {
                    return new ReadOnlySpan<byte>(byteArray, 0, length);
                }

                return ReadOnlySpan<byte>.Empty;
            }

            public override MQPROPVARIANT Export(ref GCHandle handle)
            {
                handle = GCHandle.Alloc(this.Value, GCHandleType.Pinned);
                return new()
                {
                    vt = (ushort)VarType.ByteArray,
                    caub =
                    {
                        cElems = (uint)this.Value.Length,
                        pElems = handle.AddrOfPinnedObject(),
                    }
                };
            }

            public override void Import(in MQPROPVARIANT variant, MQ.HR status) { }

            public override void Adjust(int size)
            {
                var byteArray = this.Value;
                if (byteArray is null || byteArray.Length < size)
                {
                    Array.Resize(ref byteArray, size);
                    this.Value = byteArray;
                }
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteBase64StringValue(this.Value);
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                var arr = reader.GetBytesFromBase64();
                this.Value = arr;
                return arr.Length;
            }
        }

        private abstract class BufferPropertyBox<T> : PropertyBox<T>
        {
            protected BufferPropertyBox(int size)
                : base(new StrongBox<byte[]>(new byte[size])) { }

            protected BufferPropertyBox(T value) : base(new StrongBox<byte[]>())
            {
                ((StrongBox<byte[]>)this.Box).Value = ToByteArray(value, null);
            }

            protected abstract VarType Type { get; }

            public sealed override T Value
            {
                get => FromByteArray(this.RawValue);
                set
                {
                    var box = (StrongBox<byte[]>)this.Box;
                    box.Value = ToByteArray(value, box.Value);
                }
            }

            public byte[]? RawValue => ((StrongBox<byte[]>)this.Box).Value;

            public override MQPROPVARIANT Export(ref GCHandle handle)
            {
                var value = this.RawValue;
                if (value is null)
                {
                    return new() { vt = (ushort)VarType.Null };
                }

                handle = GCHandle.Alloc(value, GCHandleType.Pinned);
                return new()
                {
                    vt = (ushort)this.Type,
                    caub =
                    {
                        cElems = (uint)value.Length,
                        pElems = handle.AddrOfPinnedObject(),
                    }
                };
            }

            public sealed override void Import(in MQPROPVARIANT variant, MQ.HR status) { }

            protected abstract T FromByteArray(byte[]? byteArray);
            protected abstract byte[] ToByteArray(T value, byte[]? byteArray);
        }

        private sealed class MessageIdPropertyBox : BufferPropertyBox<MessageId>
        {
            public MessageIdPropertyBox() : base(MessageId.Size) { }

            public MessageIdPropertyBox(MessageId value) : base(value) { }

            protected override VarType Type => VarType.ByteArray;

            protected override MessageId FromByteArray(byte[]? byteArray) =>
                byteArray is null ? default : new MessageId(byteArray);

            protected override byte[] ToByteArray(MessageId value, byte[]? byteArray) =>
                value.TryWriteBytes(byteArray) ? byteArray! : value.ToByteArray();

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                var str = reader.GetString();
                this.Value = str is not null ? MessageId.Parse(str) : default;
                return MessageId.Size;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                if (this.Value.IsNullOrEmpty())
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(this.Value.ToString());
                }
            }
        }

        private abstract class PointerPropertyBox<T> : BufferPropertyBox<T>
        {
            protected PointerPropertyBox(T value) : base(value) { }

            protected PointerPropertyBox(int size) : base(size) { }

            protected abstract int Size { get; }

            public sealed override MQPROPVARIANT Export(ref GCHandle handle)
            {
                handle = GCHandle.Alloc(this.RawValue, GCHandleType.Pinned);
                return new()
                {
                    vt = (ushort)this.Type,
                    ptr = handle.AddrOfPinnedObject(),
                };
            }
        }

        private sealed class StringPropertyBox : PointerPropertyBox<string>
        {
            public StringPropertyBox() : this(String.Empty) { }

            public StringPropertyBox(int length) : base(GetMaxByteCount(length)) { }

            public StringPropertyBox(string value) : base(value) { }

            protected override VarType Type => VarType.String;

            protected override int Size => 0;

            public string GetValue(int length) => StringFromByteArray(this.RawValue, length);

            public override void Adjust(int size)
            {
                var byteArray = this.RawValue;
                var newLength = GetMaxByteCount(size);
                if (byteArray is null || byteArray.Length < newLength)
                {
                    Array.Resize(ref byteArray, newLength);
                    ((StrongBox<byte[]>)this.Box).Value = byteArray;
                }
            }

            protected override string FromByteArray(byte[]? byteArray) => StringFromByteArray(byteArray);

            protected override byte[] ToByteArray(string value, byte[]? byteArray) => StringToByteArray(value, byteArray);

            private static string StringFromByteArray(byte[]? byteArray, int length = 0)
            {
                if (byteArray is null || byteArray.Length == 0)
                    return String.Empty;

                var bytes = length > 0 ?
                    byteArray.AsSpan(0, length * 2) :
                    byteArray.AsSpan().TrimEnd((byte)0);

                // trim last zero bytes but keep a zero byte if the remain length is not even
                if (length <= 0 && bytes.Length > 0 && bytes.Length % 2 != 0)
                {
                    bytes = byteArray.AsSpan(0, bytes.Length + 1);
                }

                return Encoding.Unicode.GetString(bytes);
            }

            private static byte[] StringToByteArray(string value, byte[]? byteArray)
            {
                if (String.IsNullOrEmpty(value))
                    return Array.Empty<byte>();

                var bufferLength = GetMaxByteCount(value.Length);
                var buffer = byteArray is not null && byteArray.Length >= bufferLength ? byteArray : new byte[bufferLength];
                Encoding.Unicode.GetBytes(value, buffer);
                buffer[bufferLength - 1] = 0;

                return buffer;
            }

            private static int GetMaxByteCount(int strLength) =>
                (strLength * 2) + 1;

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteStringValue(this.Value);
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                var str = reader.GetString() ?? string.Empty;
                this.Value = str;
                // the length (in Unicode characters) of the string plus the end-of-string character.
                return str.Length + 1;
            }
        }

        private sealed class GuidPropertyBox : PointerPropertyBox<Guid>
        {
            public GuidPropertyBox(Guid value) : base(value) { }

            protected override VarType Type => VarType.Guid;

            protected override int Size => 16;

            protected override Guid FromByteArray(byte[]? byteArray) =>
                byteArray is null ? Guid.Empty : new(byteArray);

            protected override byte[] ToByteArray(Guid value, byte[]? byteArray) =>
                value.TryWriteBytes(byteArray) ? byteArray! : value.ToByteArray();

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                this.Value = reader.GetGuid();
                return 16;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteStringValue(this.Value);
            }
        }

        private abstract class NativePropertyBox<T> : PropertyBox<T>
        {
            protected NativePropertyBox()
                : base(new StrongBox<T>()) { }

            public sealed override MQPROPVARIANT Export(ref GCHandle handle)
            {
                return new() { vt = (ushort)VarType.Null };
            }
        }

        private sealed class NativeStringPropertyBox : NativePropertyBox<string>
        {
            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                this.Value = Marshal.PtrToStringUni(variant.ptr)!;
                MQ.FreeMemory(variant.ptr);
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                var str = reader.GetString() ?? String.Empty;
                this.Value = str;
                // the length (in Unicode characters) of the string plus the end-of-string character.
                return str.Length + 1;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteStringValue(this.Value);
            }
        }

        private sealed class StringArrayPropertyBox : NativePropertyBox<string[]>
        {
            public override void Import(in MQPROPVARIANT variant, MQ.HR status)
            {
                var strs = this.Value = new string[variant.caub.cElems];

                for (var i = 0; i != variant.caub.cElems; ++i)
                {
                    var ptr = Marshal.ReadIntPtr(variant.caub.pElems, i * IntPtr.Size);
                    strs[i] = Marshal.PtrToStringUni(ptr)!;
                    MQ.FreeMemory(ptr);
                }
                MQ.FreeMemory(variant.caub.pElems);
            }

            protected override int ReadOverride(ref Utf8JsonReader reader)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                    return 0;

                var strs = new List<string>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    var str = reader.GetString();
                    if (str is not null)
                    {
                        strs.Add(str);
                    }
                }

                reader.Read();
                this.Value = strs.ToArray();

                return strs.Count;
            }

            protected override void WriteOverride(Utf8JsonWriter writer)
            {
                writer.WriteStartArray();
                foreach (var str in this.Value)
                {
                    writer.WriteStringValue(str);
                }
                writer.WriteEndArray();
            }
        }

        public sealed class Package : IDisposable
        {
            private readonly PropertyBag bag;

            private readonly int[] propIds;
            private readonly MQPROPVARIANT[] propVars;
            private readonly uint[] propStatus;
            private readonly GCHandle[] handles;

            internal Package(PropertyBag bag)
            {
                this.bag = bag;
                this.propIds = new int[this.bag.count];
                this.propVars = new MQPROPVARIANT[this.bag.count];
                this.propStatus = new uint[this.bag.count];
                this.handles = new GCHandle[this.bag.count + 3];

                Allocate();
            }

            public static implicit operator MQPROPS(Package package) => package.bag.dataRef;

            public TProperties Unpack<TProperties>() where TProperties : PropertyBag
            {
                Import();

                return (TProperties)this.bag;
            }

            public void Dispose()
            {
                Free();
            }

            public void Adjust(MQ.HR result)
            {
                Import();
                Free();

                if (MQ.IsBufferOverflow(result))
                {
                    for (var i = 0; i != this.propIds.Length; ++i)
                    {
                        var propertyId = this.propIds[i];
                        var propertyStatus = (MQ.HR)this.propStatus[i];
                        this.bag.Adjust(propertyId, propertyStatus, result);
                    }
                }

                Allocate();
            }

            private void Allocate()
            {
                var index = 0;
                var baseId = this.bag.baseId;
                var totalCount = this.bag.properties.Length;
                for (var i = 0; i != totalCount; ++i)
                {
                    var property = this.bag.properties[i];
                    if (property is null)
                        continue;

                    this.propIds[index] = baseId + i;
                    this.propVars[index] = property.Export(ref this.handles[index]);

                    ++index;
                }

                this.bag.dataRef.cProp = this.propIds.Length;

                var propIdsHandle = GCHandle.Alloc(this.propIds, GCHandleType.Pinned);
                this.bag.dataRef.aPropID = propIdsHandle.AddrOfPinnedObject();
                this.handles[index++] = propIdsHandle;

                var propVarsHandle = GCHandle.Alloc(this.propVars, GCHandleType.Pinned);
                this.bag.dataRef.aPropVar = propVarsHandle.AddrOfPinnedObject();
                this.handles[index++] = propVarsHandle;

                var propStatusHandle = GCHandle.Alloc(this.propStatus, GCHandleType.Pinned);
                this.bag.dataRef.aStatus = propStatusHandle.AddrOfPinnedObject();
                this.handles[index++] = propStatusHandle;
            }

            private void Free()
            {
                for (var i = 0; i != this.handles.Length; ++i)
                {
                    var handle = this.handles[i];
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }

                this.bag.dataRef.Clear();
            }

            private void Import()
            {
                if (this.bag.dataRef.IsEmpty)
                    throw new InvalidOperationException();

                for (var i = 0; i != this.propIds.Length; ++i)
                {
                    var propertyId = this.propIds[i];
                    if (propertyId > 0)
                    {
                        this.bag.properties[propertyId - this.bag.baseId]?.Import(this.propVars[i], (MQ.HR)this.propStatus[i]);
                    }
                }
            }
        }

        private readonly PropertyBox[] properties;
        private readonly int baseId;
        private int count;
        private readonly MQPROPS dataRef;

        protected PropertyBag(int maxPropertyCount, int basePropertyId)
        {
            this.properties = new PropertyBox[maxPropertyCount];
            this.baseId = basePropertyId;
            this.dataRef = new MQPROPS();
        }

        protected PropertyBox this[int propertyId] => this.properties[propertyId - this.baseId];

        public string GetString(int stringId, int stringLengthId)
        {
            if (this.properties[stringId - this.baseId] is StringPropertyBox stringProp &&
                this.properties[stringLengthId - this.baseId] is IntPropertyBox stringLengthProp)
            {
                return stringProp.GetValue((int)stringLengthProp.Value - 1);
            }

            return String.Empty;
        }

        public void SetString(int stringId, int stringLengthId, string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                Clear(stringId);
                Clear(stringLengthId);
            }
            else
            {
                SetValue(stringId, value);
                SetValue(stringLengthId, value.Length + 1);
            }
        }

        public ReadOnlySpan<byte> GetArray(int arrayId, int arrayLengthId)
        {
            if (this.properties[arrayId - this.baseId] is ArrayPropertyBox arrayProp &&
                this.properties[arrayLengthId - this.baseId] is IntPropertyBox arrayLengthProp)
            {
                return arrayProp.GetValue((int)arrayLengthProp.Value);
            }

            return ReadOnlySpan<byte>.Empty;
        }

        public void SetArray(int arrayId, int arrayLengthId, byte[] byteArray)
        {
            if (byteArray is null || byteArray.Length == 0)
            {
                Clear(arrayId);
                Clear(arrayLengthId);
            }
            else
            {
                SetValue(arrayId, byteArray);
                SetValue(arrayLengthId, byteArray.Length);
            }
        }

        public T GetValue<T>(int propertyId, T defaultValue = default!)
        {
            var property = this.properties[propertyId - this.baseId];
            if (property is null)
                return defaultValue;

            return ((PropertyBox<T>)property).Value;
        }

        public void SetValue<T>(int propertyId, T value)
        {
            var property = this.properties[propertyId - this.baseId];
            if (property is null)
            {
                property = this.properties[propertyId - this.baseId] = value switch
                {
                    string str => new StringPropertyBox(str),
                    byte[] arr => new ArrayPropertyBox(arr),
                    byte val => new BytePropertyBox(val),
                    short val => new ShortPropertyBox((ushort)val),
                    ushort val => new ShortPropertyBox(val),
                    int val => new IntPropertyBox((uint)val),
                    uint val => new IntPropertyBox(val),
                    long val => new LongPropertyBox((ulong)val),
                    ulong val => new LongPropertyBox(val),
                    Guid guid when guid != default => new GuidPropertyBox(guid),
                    MessageId msgId when msgId != default => new MessageIdPropertyBox(msgId),
                    _ => null!,
                };

                if (property is not null)
                {
                    ++this.count;
                }
            }
            else
            {
                ((PropertyBox<T>)property).Value = value;
            }
        }

        public Package Pack() => new(this);

        public void Dispose()
        {
            for (var i = 0; i != this.properties.Length; ++i)
            {
                this.properties[i] = null!;
            }
        }

        protected void Clear(int propertyId)
        {
            ref var property = ref this.properties[propertyId - this.baseId];
            if (property is not null)
            {
                property = null;
                --this.count;
            }
        }

        protected void InitMessageId(int propertyId) => Init<MessageIdPropertyBox>(propertyId);

        protected void InitArray(int propertyId, int lengthPropertyId, int length = 256)
        {
            if (length <= 0)
            {
                Init<ArrayPropertyBox>(propertyId);
                Init<IntPropertyBox>(lengthPropertyId);
            }
            else
            {
                if (this.properties[propertyId - this.baseId] is not null ||
                    this.properties[lengthPropertyId - this.baseId] is not null)
                {
                    throw new InvalidOperationException();
                }

                this.properties[propertyId - this.baseId] = new ArrayPropertyBox(new byte[length]);
                this.properties[lengthPropertyId - this.baseId] = new IntPropertyBox((uint)length);
                this.count += 2;
            }
        }

        protected void InitString(int propertyId, int lengthPropertyId, int length = 124)
        {
            if (length <= 0)
            {
                Init<StringPropertyBox>(propertyId);
                Init<IntPropertyBox>(lengthPropertyId);
            }
            else
            {
                if (this.properties[propertyId - this.baseId] is not null ||
                    this.properties[lengthPropertyId - this.baseId] is not null)
                {
                    throw new InvalidOperationException();
                }

                this.properties[propertyId - this.baseId] = new StringPropertyBox(length);
                this.properties[lengthPropertyId - this.baseId] = new IntPropertyBox((uint)length);
                this.count += 2;
            }
        }

        protected void InitString(int propertyId) => Init<NativeStringPropertyBox>(propertyId);

        protected void InitStringArray(int propertyId) => Init<StringArrayPropertyBox>(propertyId);

        private void Init<TProperty>(int propertyId) where TProperty : PropertyBox, new()
        {
            ref var property = ref this.properties[propertyId - this.baseId];
            if (property is not null)
                throw new InvalidOperationException();

            property = new TProperty();
            ++this.count;
        }

        private void Adjust(int propertyId, MQ.HR propertyStatus, MQ.HR result)
        {
            if (MQ.IsBufferOverflow(result) || MQ.IsBufferOverflow(propertyStatus))
            {
                var sizePropertyId = GetSizePropertyId(propertyId);
                if (sizePropertyId >= this.baseId)
                {
                    this.properties[propertyId - this.baseId].Adjust((int)GetValue<uint>(sizePropertyId));
                }
            }
        }

        protected virtual int GetSizePropertyId(int propertyId) => this.baseId - 1;
    }

    sealed class MessageProperties : PropertyBag
    {
        private static readonly string[] propertyNames;

        static MessageProperties()
        {
            // all possible flags, except None and All
            propertyNames = Enum.GetNames<MessageProperty>()[1..^1];
        }

        public MessageProperties(MessageProperty propertyFlags)
            : base(MQ.PROPID.M.Count, MQ.PROPID.M.BASE + 1)
        {
            Init(propertyFlags);
        }

        private void Init(MessageProperty propertyFlags, bool initEmpty = false)
        {
            if (propertyFlags.HasFlag(MessageProperty.MessageId))
            {
                InitMessageId(MQ.PROPID.M.MSGID);
            }
            if (propertyFlags.HasFlag(MessageProperty.CorrelationId))
            {
                InitMessageId(MQ.PROPID.M.CORRELATIONID);
            }
            if (propertyFlags.HasFlag(MessageProperty.Body))
            {
                InitArray(MQ.PROPID.M.BODY, MQ.PROPID.M.BODY_SIZE, initEmpty ? 0 : 256);
            }
            if (propertyFlags.HasFlag(MessageProperty.Label))
            {
                InitString(MQ.PROPID.M.LABEL, MQ.PROPID.M.LABEL_LEN, initEmpty ? 0 : 124);
            }
            if (propertyFlags.HasFlag(MessageProperty.Extension))
            {
                InitArray(MQ.PROPID.M.EXTENSION, MQ.PROPID.M.EXTENSION_LEN, initEmpty ? 0 : 256);
            }
            if (propertyFlags.HasFlag(MessageProperty.RespQueue))
            {
                InitString(MQ.PROPID.M.RESP_QUEUE, MQ.PROPID.M.RESP_QUEUE_LEN, initEmpty ? 0 : 124);
            }
            if (propertyFlags.HasFlag(MessageProperty.AdminQueue))
            {
                InitString(MQ.PROPID.M.ADMIN_QUEUE, MQ.PROPID.M.ADMIN_QUEUE_LEN, initEmpty ? 0 : 124);
            }
            if (propertyFlags.HasFlag(MessageProperty.DestQueue))
            {
                InitString(MQ.PROPID.M.DEST_QUEUE, MQ.PROPID.M.DEST_QUEUE_LEN, initEmpty ? 0 : 124);
            }
            if (propertyFlags.HasFlag(MessageProperty.XactStatusQueue))
            {
                InitString(MQ.PROPID.M.XACT_STATUS_QUEUE, MQ.PROPID.M.XACT_STATUS_QUEUE_LEN, initEmpty ? 0 : 124);
            }
            if (propertyFlags.HasFlag(MessageProperty.DeadLetterQueue))
            {
                InitString(MQ.PROPID.M.DEADLETTER_QUEUE, MQ.PROPID.M.DEADLETTER_QUEUE_LEN, initEmpty ? 0 : 124);
            }
        }

        public static implicit operator Message(MessageProperties properties) => new(properties);

        public void Write(Utf8JsonWriter writer, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var maxPropertyCount = propertyNames.Length;
            for (var i = 0; i != maxPropertyCount; ++i)
            {
                var property = base[GetPropertyId((MessageProperty)(1ul << i))];
                if (property is not null)
                {
                    var propertyName = propertyNames[i];
                    writer.WritePropertyName(propertyName);
                    property.Write(writer);
                }
            }

            writer.WriteEndObject();
        }

        public static MessageProperties Read(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var properties = new MessageProperties(MessageProperty.None);

            var propertyCount = propertyNames.Length;
            var propertyIndex = -1;
            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    break;

                // search for a property cycling through list starting from the given index
                for (var i = (propertyIndex + 1) % propertyCount; i != propertyIndex && i != propertyCount; i = (i + 1) % propertyCount)
                {
                    if (reader.ValueTextEquals(propertyNames[i]))
                    {
                        propertyIndex = i;

                        var propertyFlag = (MessageProperty)(1ul << i);
                        properties.Init(propertyFlag, initEmpty: true);
                        var propertyId = GetPropertyId(propertyFlag);
                        var property = properties[propertyId];
                        if (property is null)
                        {
                            reader.Read();
                            break;
                        }
                        var size = property.Read(ref reader);
                        if (size > 0)
                        {
                            var sizePropertyId = properties.GetSizePropertyId(propertyId);
                            if (sizePropertyId > MQ.PROPID.M.BASE)
                            {
                                var sizeProperty = properties[sizePropertyId];
                                sizeProperty.Import(new MQPROPVARIANT { vt = (ushort)VarType.UInt, ulVal = (uint)size }, MQ.HR.OK);
                            }
                        }

                        break;
                    }
                }
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return properties;
        }

        protected override int GetSizePropertyId(int propertyId) => propertyId switch
        {
            MQ.PROPID.M.BODY => MQ.PROPID.M.BODY_SIZE,
            MQ.PROPID.M.EXTENSION => MQ.PROPID.M.EXTENSION_LEN,
            MQ.PROPID.M.LABEL => MQ.PROPID.M.LABEL_LEN,
            MQ.PROPID.M.RESP_QUEUE => MQ.PROPID.M.RESP_QUEUE_LEN,
            MQ.PROPID.M.ADMIN_QUEUE => MQ.PROPID.M.ADMIN_QUEUE_LEN,
            MQ.PROPID.M.DEST_QUEUE => MQ.PROPID.M.DEST_QUEUE_LEN,
            MQ.PROPID.M.XACT_STATUS_QUEUE => MQ.PROPID.M.XACT_STATUS_QUEUE_LEN,
            MQ.PROPID.M.PROV_NAME => MQ.PROPID.M.PROV_NAME_LEN,
            MQ.PROPID.M.RESP_FORMAT_NAME => MQ.PROPID.M.RESP_FORMAT_NAME_LEN,
            MQ.PROPID.M.DEST_FORMAT_NAME => MQ.PROPID.M.DEST_FORMAT_NAME_LEN,
            MQ.PROPID.M.DEADLETTER_QUEUE => MQ.PROPID.M.DEADLETTER_QUEUE_LEN,
            MQ.PROPID.M.COMPOUND_MESSAGE => MQ.PROPID.M.COMPOUND_MESSAGE_SIZE,
            MQ.PROPID.M.SOAP_ENVELOPE => MQ.PROPID.M.SOAP_ENVELOPE_LEN,
            MQ.PROPID.M.SENDERID => MQ.PROPID.M.SENDERID_LEN,
            MQ.PROPID.M.SENDER_CERT => MQ.PROPID.M.SENDER_CERT_LEN,
            MQ.PROPID.M.DEST_SYMM_KEY => MQ.PROPID.M.DEST_SYMM_KEY_LEN,
            MQ.PROPID.M.SIGNATURE => MQ.PROPID.M.SIGNATURE_LEN,
            _ => MQ.PROPID.M.BASE,
        };

        private static int GetPropertyId(MessageProperty propertyFlag) => propertyFlag switch
        {
            MessageProperty.Class => MQ.PROPID.M.CLASS,
            MessageProperty.MessageId => MQ.PROPID.M.MSGID,
            MessageProperty.CorrelationId => MQ.PROPID.M.CORRELATIONID,
            MessageProperty.Priority => MQ.PROPID.M.PRIORITY,
            MessageProperty.Delivery => MQ.PROPID.M.DELIVERY,
            MessageProperty.Acknowledge => MQ.PROPID.M.ACKNOWLEDGE,
            MessageProperty.Journal => MQ.PROPID.M.JOURNAL,
            MessageProperty.AppApecific => MQ.PROPID.M.APPSPECIFIC,
            MessageProperty.Body => MQ.PROPID.M.BODY,
            MessageProperty.Label => MQ.PROPID.M.LABEL,
            MessageProperty.TimeToReachQueue => MQ.PROPID.M.TIME_TO_REACH_QUEUE,
            MessageProperty.TimeToBeReceived => MQ.PROPID.M.TIME_TO_BE_RECEIVED,
            MessageProperty.RespQueue => MQ.PROPID.M.RESP_QUEUE,
            MessageProperty.AdminQueue => MQ.PROPID.M.ADMIN_QUEUE,
            MessageProperty.Version => MQ.PROPID.M.VERSION,
            MessageProperty.SenderId => MQ.PROPID.M.SENDERID,
            MessageProperty.SenderIdType => MQ.PROPID.M.SENDERID_TYPE,
            MessageProperty.PrivLevel => MQ.PROPID.M.PRIV_LEVEL,
            MessageProperty.AuthLevel => MQ.PROPID.M.AUTH_LEVEL,
            MessageProperty.Authenticated => MQ.PROPID.M.AUTHENTICATED,
            MessageProperty.HashAlg => MQ.PROPID.M.HASH_ALG,
            MessageProperty.EncryptionAlg => MQ.PROPID.M.ENCRYPTION_ALG,
            MessageProperty.SenderCert => MQ.PROPID.M.SENDER_CERT,
            MessageProperty.SrcMachineId => MQ.PROPID.M.SRC_MACHINE_ID,
            MessageProperty.SentTime => MQ.PROPID.M.SENTTIME,
            MessageProperty.ArrivedTime => MQ.PROPID.M.ARRIVEDTIME,
            MessageProperty.DestQueue => MQ.PROPID.M.DEST_QUEUE,
            MessageProperty.Extension => MQ.PROPID.M.EXTENSION,
            MessageProperty.SecurityContext => MQ.PROPID.M.SECURITY_CONTEXT,
            MessageProperty.ConnectorType => MQ.PROPID.M.CONNECTOR_TYPE,
            MessageProperty.XactStatusQueue => MQ.PROPID.M.XACT_STATUS_QUEUE,
            MessageProperty.Trace => MQ.PROPID.M.TRACE,
            MessageProperty.BodyType => MQ.PROPID.M.BODY_TYPE,
            MessageProperty.DestSymmKey => MQ.PROPID.M.DEST_SYMM_KEY,
            MessageProperty.Signature => MQ.PROPID.M.SIGNATURE,
            MessageProperty.ProvType => MQ.PROPID.M.PROV_TYPE,
            MessageProperty.ProvName => MQ.PROPID.M.PROV_NAME,
            MessageProperty.FirstInXact => MQ.PROPID.M.FIRST_IN_XACT,
            MessageProperty.LastInXact => MQ.PROPID.M.LAST_IN_XACT,
            MessageProperty.XactId => MQ.PROPID.M.XACTID,
            MessageProperty.AuthenticatedEx => MQ.PROPID.M.AUTHENTICATED_EX,
            MessageProperty.RespFormatName => MQ.PROPID.M.RESP_FORMAT_NAME,
            MessageProperty.DestFormatName => MQ.PROPID.M.DEST_FORMAT_NAME,
            MessageProperty.LookupId => MQ.PROPID.M.LOOKUPID,
            MessageProperty.SoapEnvelope => MQ.PROPID.M.SOAP_ENVELOPE,
            MessageProperty.CompoundMessage => MQ.PROPID.M.COMPOUND_MESSAGE,
            MessageProperty.SoapHeader => MQ.PROPID.M.SOAP_HEADER,
            MessageProperty.SoapBody => MQ.PROPID.M.SOAP_BODY,
            MessageProperty.DeadLetterQueue => MQ.PROPID.M.DEADLETTER_QUEUE,
            MessageProperty.AbortCount => MQ.PROPID.M.ABORT_COUNT,
            MessageProperty.MoveCount => MQ.PROPID.M.MOVE_COUNT,
            MessageProperty.LastMoveTime => MQ.PROPID.M.LAST_MOVE_TIME,
            _ => MQ.PROPID.M.BASE,
        };
    }

    sealed class QueueProperties : PropertyBag
    {
        public QueueProperties()
            : base(MQ.PROPID.Q.Count, MQ.PROPID.Q.BASE + 1) { }

        public static implicit operator QueueInfo(QueueProperties properties) => new(properties);
    }

    sealed class MachineManagementProperties : PropertyBag
    {
        public MachineManagementProperties()
            : base(MQ.PROPID.MGMT_MSMQ.Count, MQ.PROPID.MGMT_MSMQ.BASE + 1)
        {
            InitStringArray(MQ.PROPID.MGMT_MSMQ.ACTIVEQUEUES);
            InitStringArray(MQ.PROPID.MGMT_MSMQ.PRIVATEQ);
            InitString(MQ.PROPID.MGMT_MSMQ.DSSERVER);
            InitString(MQ.PROPID.MGMT_MSMQ.CONNECTED);
            InitString(MQ.PROPID.MGMT_MSMQ.TYPE);
            SetValue(MQ.PROPID.MGMT_MSMQ.BYTES_IN_ALL_QUEUES, 0ul);
        }

        public static implicit operator MachineManagementInfo(MachineManagementProperties properties) => new(properties);
    }

    sealed class QueueManagementProperties : PropertyBag
    {
        public QueueManagementProperties()
            : base(MQ.PROPID.MGMT_QUEUE.Count, MQ.PROPID.MGMT_QUEUE.BASE + 1)
        {
            InitString(MQ.PROPID.MGMT_QUEUE.PATHNAME);
            InitString(MQ.PROPID.MGMT_QUEUE.FORMATNAME);
            InitString(MQ.PROPID.MGMT_QUEUE.TYPE);
            InitString(MQ.PROPID.MGMT_QUEUE.LOCATION);
            InitString(MQ.PROPID.MGMT_QUEUE.XACT);
            InitString(MQ.PROPID.MGMT_QUEUE.FOREIGN);
            SetValue(MQ.PROPID.MGMT_QUEUE.MESSAGE_COUNT, 0u);
            SetValue(MQ.PROPID.MGMT_QUEUE.BYTES_IN_QUEUE, 0u);
            SetValue(MQ.PROPID.MGMT_QUEUE.JOURNAL_MESSAGE_COUNT, 0u);
            SetValue(MQ.PROPID.MGMT_QUEUE.BYTES_IN_JOURNAL, 0u);
            InitString(MQ.PROPID.MGMT_QUEUE.STATE);
            InitStringArray(MQ.PROPID.MGMT_QUEUE.NEXTHOPS);

            SetValue(MQ.PROPID.MGMT_QUEUE.SUBQUEUE_COUNT, 0u);
            InitStringArray(MQ.PROPID.MGMT_QUEUE.SUBQUEUE_NAMES);
        }

        public static implicit operator QueueManagementInfo(QueueManagementProperties properties) => new(properties);
    }
}
