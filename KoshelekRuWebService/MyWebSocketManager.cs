namespace KoshelekRuWebService;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public class MyWebSocketManager : WebSocketManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _sockets = new();

    public Guid AddSocket(WebSocket socket)
    {
        var socketId = Guid.NewGuid();
        _sockets.TryAdd(socketId, socket);
        return socketId;
    }

    public async Task RemoveSocket(Guid socketId)
    {
        if(_sockets.TryRemove(socketId, out var socket))
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed", CancellationToken.None).ConfigureAwait(false);
            socket.Dispose();
        }
    }

    public Task SendMessageAsync<T>(Guid socketId, T message)
    {
        // TODO return bool? 
        if(_sockets.TryGetValue(socketId, out var socket) && socket.State == WebSocketState.Open)
        {
            string jsonMess = JsonSerializer.Serialize(message);
            byte[] bytes = Encoding.UTF8.GetBytes(jsonMess);
            return socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        return Task.CompletedTask;
    }

    public async Task BroadcastAsync<T>(T message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(jsonMessage);

        foreach(var socket in _sockets.Values)
        {
            if(socket.State == WebSocketState.Open)
            {
                await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    public override Task<WebSocket> AcceptWebSocketAsync(string? subProtocol)
    {
        throw new NotImplementedException();
    }

    public override bool IsWebSocketRequest => throw new NotImplementedException();

    public override IList<string> WebSocketRequestedProtocols => throw new NotImplementedException();
}
