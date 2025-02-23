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

        var buffer = ArrayPool<byte>.Shared.Rent(256);
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, CancellationToken.None)
                                                            .ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(
                        result.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        result.CloseStatusDescription,
                        CancellationToken.None)
                            .ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            MyLogger.Error(logger, $"Error while listening socket", ex);
            await socket
                .CloseAsync(WebSocketCloseStatus.InternalServerError, "Server error", CancellationToken.None)
                .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true);
        }
    }

    public async Task RemoveSocket(Guid socketId)
    {
        if (_sockets.TryRemove(socketId, out var socket))
        {
            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None)
                            .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // ignore
                MyLogger.Error(logger, $"Error while removing socket {socketId}.", ex);
            }
            finally
            {
                socket.Dispose();
                MyLogger.Info(logger, $"socket with id {socketId} removed.");
            }
        }
    }

    /// <inheritdoc/>
    public override Task<WebSocket> AcceptWebSocketAsync(string? subProtocol)
    {
        throw new NotImplementedException();
    }

    public async Task StartListening(WebSocket ws)
    {
        var id = Add(ws);

        try
        {
            MyLogger.Info(logger, $"...listening ws {id}...");
            await Listen(ws).ConfigureAwait(false);
        }
        finally
        {
            await RemoveSocket(id).ConfigureAwait(false);
        }
    }

    private Guid Add(WebSocket socket)
    {
        var socketId = Guid.NewGuid();
        if (_sockets.TryAdd(socketId, socket))
        {
            MyLogger.Info(logger, $"socket with id {socketId} added.");
        }

        return socketId;
    }
}
