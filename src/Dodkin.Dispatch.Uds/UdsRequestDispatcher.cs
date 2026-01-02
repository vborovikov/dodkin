namespace Dodkin.Dispatch;

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Relay.RequestModel;

public class UdsRequestDispatcher : UdsOperator, IQueueRequestDispatcher
{
    private readonly UdsConnectionPool connectionPool;
    private readonly ILogger log;

    public UdsRequestDispatcher(string socketPath, ILogger logger)
    {
        this.connectionPool = new(socketPath, MaxConnections);
        this.log = logger;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        // No socket to dispose here as we create connections per request
    }

    /// <inheritdoc />
    public Task ExecuteAsync<TCommand>(TCommand command) where TCommand : ICommand =>
        ExecuteWaitAsync(command, null);

    /// <inheritdoc />
    public Task ExecuteAsync<TCommand>(TCommand command, TimeSpan timeout) where TCommand : ICommand =>
        ExecuteWaitAsync(command, timeout);

    /// <inheritdoc />
    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query) =>
        RunWaitAsync((Query<TResult>)query, null);

    /// <inheritdoc />
    public Task<TResult> RunAsync<TResult>(IQuery<TResult> query, TimeSpan timeout) =>
        RunWaitAsync((Query<TResult>)query, timeout);

    private async Task ExecuteWaitAsync<TCommand>(TCommand command, TimeSpan? timeout) where TCommand : ICommand
    {
        using var timeoutCts = new CancellationTokenSource(timeout ?? this.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(command.CancellationToken, timeoutCts.Token);
        var cancellationToken = linkedCts.Token;
        var socket = await this.connectionPool.ConnectAsync(cancellationToken);
        using var buffer = new Utf8MemoryBuffer();
        try
        {
            buffer.TryAppend(command.GetType().AssemblyQualifiedName);
            buffer.TryAppend(PayloadSeparator);
            buffer.TrySerialize(command);

            await socket.SendAsync(buffer.WrittenMemory, SocketFlags.None, cancellationToken);

            buffer.Clear();
            var bytesReceived = await socket.ReceiveAsync(buffer.GetMemory(), cancellationToken);
            buffer.Advance(bytesReceived);

            var response = buffer.WrittenSpan;
            if (!response.SequenceEqual("ack"u8))
            {
                if (TryRead<Exception>(response, out var exception))
                    throw exception;

                throw new InvalidOperationException($"Unexpected response: {Encoding.UTF8.GetString(response)}");
            }
        }
        catch (OperationCanceledException x) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException("Timeout", x);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.CommandFailed, x, "Error executing command");
            throw;
        }
        finally
        {
            this.connectionPool.Disconnect(socket);
        }
    }

    private async Task<TResult> RunWaitAsync<TResult>(Query<TResult> query, TimeSpan? timeout)
    {
        using var timeoutCts = new CancellationTokenSource(timeout ?? this.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(query.CancellationToken, timeoutCts.Token);
        var cancellationToken = linkedCts.Token;
        var socket = await this.connectionPool.ConnectAsync(cancellationToken);
        using var buffer = new Utf8MemoryBuffer();
        try
        {
            buffer.TryAppend(query.GetType().AssemblyQualifiedName);
            buffer.TryAppend(PayloadSeparator);
            buffer.TrySerialize(query);
            await socket.SendAsync(buffer.WrittenMemory, SocketFlags.None, cancellationToken);

            buffer.Clear();
            var bytesReceived = await socket.ReceiveAsync(buffer.GetMemory(), cancellationToken);
            buffer.Advance(bytesReceived);

            var response = buffer.WrittenSpan;
            if (TryRead<TResult>(response, out var result))
            {
                return result;
            }
            else
            {
                if (TryRead<Exception>(response, out var exception))
                    throw exception;

                throw new InvalidOperationException($"Unexpected response: {Encoding.UTF8.GetString(response)}");
            }
        }
        catch (OperationCanceledException x) when (timeoutCts.IsCancellationRequested)
        {
            throw new TimeoutException("Timeout", x);
        }
        catch (Exception x) when (x is not OperationCanceledException)
        {
            this.log.LogError(EventIds.QueryFailed, x, "Error executing query");
            throw;
        }
        finally
        {
            this.connectionPool.Disconnect(socket);
        }
    }
}
