namespace Clients;

using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

class WebSocketClient
{
    public static async Task Main()
    {
        using var client = new ClientWebSocket();
        await client.ConnectAsync(new Uri("ws://localhost:5249/ws"), CancellationToken.None).ConfigureAwait(false);
        var buffer = new byte[1024];
        var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
        string response = Encoding.UTF8.GetString(buffer, 0, result.Count);
        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Закрытие соединения", CancellationToken.None).ConfigureAwait(false);
    }
}
