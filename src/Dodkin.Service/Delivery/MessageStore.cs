namespace Dodkin.Service.Delivery;

using System;
using System.Data.Common;
using System.Threading.Tasks;
using Dapper;
using Dodkin;
using Microsoft.Extensions.Options;

interface IMessageStore
{
    Task AddAsync(MessageRecord message, CancellationToken cancellationToken);
    Task<MessageRecord> GetAsync(CancellationToken cancellationToken);
    Task RemoveAsync(MessageId messageId, CancellationToken cancellationToken);
    Task RetryAsync(MessageId messageId, CancellationToken cancellationToken);
}

sealed class MessageStore : IMessageStore
{
    private readonly DbDataSource db;
    private readonly string tableName;
    private readonly ILogger<MessageStore> log;

    public MessageStore(DbDataSource db, IOptions<ServiceOptions> options, ILogger<MessageStore> log)
    {
        this.db = db;
        this.tableName = options.Value.TableName;
        this.log = log;
    }

    public async Task AddAsync(MessageRecord message, CancellationToken cancellationToken)
    {
        if (!message.IsValid)
            throw new InvalidOperationException();

        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        await using var tx = await cnn.BeginTransactionAsync(cancellationToken);

        try
        {
            await cnn.ExecuteAsync(
                $"""
                insert into {this.tableName} (MessageId, Message, Destination, DueTime)
                values (@MessageId, @Message, @Destination, @DueTime);
                """,
                new
                {
                    message.MessageId,
                    message.Message,
                    message.Destination,
                    message.DueTime,
                }, tx);

            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.log.LogError(ex,
                "Failed to record message {MessageId} for future delivery to {Destination} at {DueTime}.",
                message.MessageId, message.Destination, message.DueTime);
            await tx.RollbackAsync(cancellationToken);

            throw;
        }
    }

    public async Task RemoveAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        await using var tx = await cnn.BeginTransactionAsync(cancellationToken);

        try
        {
            await cnn.ExecuteAsync(
                $"""
                delete from {this.tableName}
                where MessageId = @MessageId;
                """, new { MessageId = messageId }, tx);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, "Failed to delete message {MessageId}.", messageId);
            await tx.RollbackAsync(cancellationToken);

            throw;
        }
    }

    public async Task<MessageRecord> GetAsync(CancellationToken cancellationToken)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        return await cnn.QueryFirstOrDefaultAsync<MessageRecord>(
            $"""
            select top 1 m.MessageId, m.Message, m.Destination, m.DueTime, m.RetryCount
            from {this.tableName} m
            order by m.DueTime; 
            """);
    }

    public async Task RetryAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        await using var cnn = await this.db.OpenConnectionAsync(cancellationToken);
        await using var tx = await cnn.BeginTransactionAsync(cancellationToken);

        try
        {
            await cnn.ExecuteAsync(
                $"""
                update {this.tableName} set RetryCount = RetryCount + 1
                where MessageId = @MessageId;
                """, new { MessageId = messageId }, tx);
            await tx.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await tx.RollbackAsync(cancellationToken);

            // no throw
        }
    }
}
