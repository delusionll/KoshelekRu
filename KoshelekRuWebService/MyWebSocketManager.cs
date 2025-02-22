namespace KoshelekRuWebService;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;

internal sealed class MyWebSocketManager : WebSocketManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = [];

    /// <inheritdoc/>
    public override bool IsWebSocketRequest => throw new NotImplementedException();

    /// <inheritdoc/>
    public override IList<string> WebSocketRequestedProtocols => throw new NotImplementedException();

    public IReadOnlyDictionary<Guid, WebSocket> Clients => _sockets;

    public static async Task ListenWebSocket(WebSocket socket)
    {
        ArgumentNullException.ThrowIfNull(socket);

        // TODO arraypool
        byte[] buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public Guid Add(WebSocket socket)
    {
        var socketId = Guid.NewGuid();
        _sockets.TryAdd(socketId, socket);
        return socketId;
    }

    public async Task RemoveSocket(Guid socketId)
    {
        if (_sockets.TryRemove(socketId, out WebSocket? socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None).ConfigureAwait(false);
            socket.Dispose();
        }
    }

    /// <inheritdoc/>
    public override Task<WebSocket> AcceptWebSocketAsync(string? subProtocol)
    {
        throw new NotImplementedException();
    }
}
