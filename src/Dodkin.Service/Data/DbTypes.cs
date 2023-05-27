namespace Dodkin.Service.Data;

using System.Data;
using System.Text.Json;
using Dapper;

static class DbTypes
{
    private sealed class MessageIdHandler : SqlMapper.TypeHandler<MessageId>
    {
        public override MessageId Parse(object value)
        {
            return MessageId.Parse(value as string);
        }

        public override void SetValue(IDbDataParameter parameter, MessageId value)
        {
            parameter.DbType = DbType.AnsiString;
            parameter.Size = 50;
            parameter.Value = value.ToString();
        }
    }

    private sealed class MessageQueueNameHandler : SqlMapper.TypeHandler<MessageQueueName>
    {
        public override MessageQueueName Parse(object value)
        {
            return MessageQueueName.Parse((string)value);
        }

        public override void SetValue(IDbDataParameter parameter, MessageQueueName value)
        {
            parameter.DbType = DbType.AnsiString;
            parameter.Size = 250;
            parameter.Value = value.FormatName;
        }
    }

    private sealed class MessageHandler : SqlMapper.TypeHandler<Message>
    {
        public override Message Parse(object value)
        {
            return JsonSerializer.Deserialize<Message>((byte[])value);
        }

        public override void SetValue(IDbDataParameter parameter, Message value)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            parameter.DbType = DbType.Binary;
            parameter.Size = bytes.Length;
            parameter.Value = bytes;
        }
    }

    public static void Initialize()
    {
        SqlMapper.AddTypeHandler(new MessageIdHandler());
        SqlMapper.AddTypeHandler(new MessageQueueNameHandler());
        SqlMapper.AddTypeHandler(new MessageHandler());
    }
}
