namespace Dodkin.Dispatch;

using System.Collections.Concurrent;
using System.Net.Sockets;

sealed class UdsConnectionPool : IDisposable
{
    private readonly ConcurrentQueue<Socket> pool;
    private readonly SemaphoreSlim isConnecting;
    private readonly string endpointPath;
    private bool isDisposed;

    public UdsConnectionPool(string endpointPath, int maxConnections = 10)
    {
        this.pool = new();
        this.endpointPath = endpointPath;
        this.isConnecting = new SemaphoreSlim(maxConnections, maxConnections);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!this.isDisposed)
        {
            this.isConnecting?.Dispose();
            while (this.pool.TryDequeue(out var socket))
            {
                socket.Dispose();
            }

            this.isDisposed = true;
        }
    }

    public async ValueTask<Socket> ConnectAsync(CancellationToken cancellationToken)
    {
        await this.isConnecting.WaitAsync(cancellationToken);
        if (!this.pool.TryDequeue(out var socket))
        {
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await socket.ConnectAsync(new UnixDomainSocketEndPoint(this.endpointPath), cancellationToken);
        }

        return socket;
    }

    public void Disconnect(Socket socket)
    {
        this.isConnecting.Release();

        if (socket.Connected)
        {
            this.pool.Enqueue(socket);
        }
        else
        {
            socket.Dispose();
        }
    }
}