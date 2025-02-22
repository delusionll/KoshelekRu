namespace KoshelekRuWebService;

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;

internal sealed class MyWebSocketManager(ILogger<MyWebSocketManager> logger) : WebSocketManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = [];

    /// <inheritdoc/>
    public override bool IsWebSocketRequest => throw new NotImplementedException();

    /// <inheritdoc/>
    public override IList<string> WebSocketRequestedProtocols => throw new NotImplementedException();

    public IReadOnlyDictionary<Guid, WebSocket> Clients => _sockets;

    public async Task Listen(WebSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None)
                                                            .ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None)
                                .ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            MyLogger.Error(logger, $"Error while listening socket", ex);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true);
        }
    }

    public Guid Add(WebSocket socket)
    {
        var socketId = Guid.NewGuid();
        if (_sockets.TryAdd(socketId, socket))
        {
            MyLogger.Info(logger, $"socket with id {socketId} added.");
        }

        return socketId;
    }

    public async Task RemoveSocket(Guid socketId)
    {
        if (_sockets.TryRemove(socketId, out WebSocket? socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None)
                        .ConfigureAwait(false);
            socket.Dispose();
            MyLogger.Info(logger, $"socket with id {socketId} removed.");
        }
    }

    /// <inheritdoc/>
    public override Task<WebSocket> AcceptWebSocketAsync(string? subProtocol)
    {
        throw new NotImplementedException();
    }
}
