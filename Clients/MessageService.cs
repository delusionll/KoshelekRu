namespace Clients;

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Domain;

internal sealed class MessageService(HttpClient httpClient, ClientWebSocket wsClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly ClientWebSocket _wsClient = wsClient;
    private int _count = 1;

    public async Task SendMessageAsync(string content)
    {
        var mess = new Message() { Content = content, SerNumber = _count++ };
        string res = JsonSerializer.Serialize(mess);
        using var jsonContent = new StringContent(res, Encoding.UTF8, MediaTypeNames.Application.Json);
        HttpResponseMessage response = await _httpClient.PostAsync(new Uri("http://localhost:5249/messages"), jsonContent).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public async Task ConnectAsync(Uri uri)
    {
        try
        {
            await _wsClient.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public async IAsyncEnumerable<Message> ReceiveMessagesAsync()
    {
        byte[] buffer = new byte[1024];
        while (_wsClient.State == WebSocketState.Open)
        {
            WebSocketReceiveResult result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(true);
            if (result.EndOfMessage)
            {
                string res = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Message? message = JsonSerializer.Deserialize(res, MyJsonContext.Default.Message);
                if (message != null)
                {
                    yield return message;
                }
            }
        }
    }

    internal IAsyncEnumerable<Message?> GetLastMessages()
    {
        return _httpClient.GetFromJsonAsAsyncEnumerable<Message>(new Uri("http://localhost:5249/lastmessages"));
    }
}