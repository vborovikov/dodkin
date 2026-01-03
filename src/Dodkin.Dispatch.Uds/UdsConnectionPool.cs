namespace Dodkin.Dispatch;

using System.Buffers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Threading.Tasks;

interface IUdsSocketPool
{
    UnixDomainSocketEndPoint ServerEndpoint { get; }

    ValueTask<Socket> ConnectAsync(CancellationToken cancellationToken);
    ValueTask DisconnectAsync(Socket socket, CancellationToken cancellationToken);
}

readonly struct UdsConnection : IAsyncDisposable
{
    private readonly Socket socket;
    private readonly IUdsSocketPool pool;

    public UdsConnection(Socket socket, IUdsSocketPool pool)
    {
        this.socket = socket;
        this.pool = pool;
    }

    public async ValueTask<int> ReceiveAsync(IBufferWriter<byte> buffer, CancellationToken cancellationToken)
    {
        var bytesReceived = await this.socket.ReceiveAsync(buffer.GetMemory(), cancellationToken);
        buffer.Advance(bytesReceived);

        return bytesReceived;
    }

    public async ValueTask<int> SendAsync(IBufferReader<byte> buffer, CancellationToken cancellationToken)
    {
        var tryCount = 2;
        while (tryCount-- > 0)
        {
            try
            {
                return await this.socket.SendAsync(buffer.WrittenMemory, SocketFlags.None, cancellationToken);
            }
            catch (SocketException x) when (x.SocketErrorCode == SocketError.NotConnected)
            {
                await this.socket.ConnectAsync(this.pool.ServerEndpoint, cancellationToken);
            }
        }

        return 0;
    }

    public async ValueTask DisposeAsync()
    {
        await this.pool.DisconnectAsync(this.socket, default);
    }
}

abstract class UdsConnectionManager : IUdsSocketPool, IDisposable
{
    private readonly string endpointPath;
    private bool isDisposed;

    protected UdsConnectionManager(UnixDomainSocketEndPoint serverEndpoint, string? endpointPath)
    {
        this.ServerEndpoint = serverEndpoint;
        this.endpointPath = endpointPath ?? Path.GetTempPath();
    }

    public void Dispose()
    {
        if (!this.isDisposed)
        {
            Dispose(disposing: true);
            this.isDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }

    public UnixDomainSocketEndPoint ServerEndpoint { get; }

    public async ValueTask<UdsConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var socket = await ConnectOverrideAsync(cancellationToken);
        return new(socket, this);
    }

    ValueTask<Socket> IUdsSocketPool.ConnectAsync(CancellationToken cancellationToken) =>
        ConnectOverrideAsync(cancellationToken);
    ValueTask IUdsSocketPool.DisconnectAsync(Socket socket, CancellationToken cancellationToken) =>
        DisconnectOverrideAsync(socket, cancellationToken);

    protected abstract ValueTask<Socket> ConnectOverrideAsync(CancellationToken cancellationToken);
    protected abstract ValueTask DisconnectOverrideAsync(Socket socket, CancellationToken cancellationToken);

    protected static Socket CreateSocket()
    {
        //var socketPath = Path.Combine(this.endpointPath, Path.ChangeExtension(Path.GetTempFileName(), ".sock"));
        //if (File.Exists(socketPath)) File.Delete(socketPath);

        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        //socket.Bind(new UnixDomainSocketEndPoint(socketPath));

        return socket;
    }
}

sealed class UdsConnectionPool : UdsConnectionManager
{
    private readonly ConcurrentQueue<Socket> pool;
    private readonly SemaphoreSlim isConnecting;

    public UdsConnectionPool(UnixDomainSocketEndPoint serverEndpoint)
        : this(serverEndpoint, Path.GetTempPath(), 10) { }

    public UdsConnectionPool(UnixDomainSocketEndPoint serverEndpoint, int maxConnections)
        : this(serverEndpoint, Path.GetTempPath(), maxConnections) { }

    public UdsConnectionPool(UnixDomainSocketEndPoint serverEndpoint, string endpointPath)
        : this(serverEndpoint, endpointPath, 10) { }

    public UdsConnectionPool(UnixDomainSocketEndPoint serverEndpoint, string? endpointPath, int maxConnections)
        : base(serverEndpoint, endpointPath)
    {
        this.pool = new();
        this.isConnecting = new SemaphoreSlim(maxConnections, maxConnections);
    }

    protected override void Dispose(bool disposing)
    {
        this.isConnecting?.Dispose();
        while (this.pool.TryDequeue(out var socket))
        {
            socket.Dispose();
        }
    }

    protected override async ValueTask<Socket> ConnectOverrideAsync(CancellationToken cancellationToken)
    {
        await this.isConnecting.WaitAsync(cancellationToken);
        if (!this.pool.TryDequeue(out var socket))
        {
            socket = CreateSocket();
        }

        try
        {
            await socket.ConnectAsync(this.ServerEndpoint, cancellationToken);
        }
        catch (SocketException)
        {
        }

        return socket;
    }

    protected override async ValueTask DisconnectOverrideAsync(Socket socket, CancellationToken cancellationToken = default)
    {
        this.isConnecting.Release();

        if (socket.Connected)
        {
            await socket.DisconnectAsync(reuseSocket: true, cancellationToken);
            this.pool.Enqueue(socket);
        }
        else
        {
            socket.Dispose();
        }
    }
}

sealed class UdsImmediateConnectionPool : UdsConnectionManager
{
    public UdsImmediateConnectionPool(UnixDomainSocketEndPoint serverEndpoint)
        : base(serverEndpoint, endpointPath: default) { }

    protected override async ValueTask<Socket> ConnectOverrideAsync(CancellationToken cancellationToken)
    {
        var socket = CreateSocket();
        await socket.ConnectAsync(this.ServerEndpoint, cancellationToken);
        return socket;
    }

    protected override async ValueTask DisconnectOverrideAsync(Socket socket, CancellationToken cancellationToken)
    {
        await socket.DisconnectAsync(reuseSocket: false, cancellationToken);
        socket.Dispose();
    }
}