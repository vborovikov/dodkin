namespace Dodkin
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using Interop;

    internal sealed class MessageJsonConverter : JsonConverter<Message>
    {
        private static readonly string[] propertyNames;

        static MessageJsonConverter()
        {
            // all possible flags, except None and All
            propertyNames = Enum.GetNames<MessageProperty>()[1..^1];
        }

        public override void Write(Utf8JsonWriter writer, Message value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var maxPropertyCount = propertyNames.Length;
            for (var i = 0; i != maxPropertyCount; ++i)
            {
                var propertyFlag = (MessageProperty)(1ul << i);
                var property = value.Properties[propertyFlag];
                if (property is not null)
                {
                    var propertyName = propertyNames[i];
                    writer.WritePropertyName(propertyName);
                    property.Write(writer);
                }
            }

            writer.WriteEndObject();
        }

        public override Message Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var properties = new MessageProperties(MessageProperty.None);

            while (reader.Read())
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    break;
                if (!TryReadProperty(ref reader, properties))
                    reader.Read();
            }

            if (reader.TokenType != JsonTokenType.EndObject)
                throw new JsonException();

            return properties;
        }

        private static bool TryReadProperty(ref Utf8JsonReader reader, MessageProperties properties)
        {
            var maxPropertyCount = propertyNames.Length;
            for (var i = 0; i != maxPropertyCount; ++i)
            {
                if (reader.ValueTextEquals(propertyNames[i]))
                {
                    var propertyFlag = (MessageProperty)(1ul << i);
                    return properties.TryRead(ref reader, propertyFlag);
                }
            }

            return false;
        }
    }
}