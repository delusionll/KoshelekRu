﻿namespace Clients;

using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Domain;

public class MessageService(HttpClient httpClient, ClientWebSocket wsClient)
{
    private readonly CancellationTokenSource _cts = new();
    private readonly HttpClient _httpClient = httpClient;
    private readonly ClientWebSocket _wsClient = wsClient;
    private int _count = 1;

    public async Task SendMessageAsync(string content)
    {
        var mess = new Message() { Content = content, SerNumber = _count++ };
        var res = JsonSerializer.Serialize(mess);
        using var jsonContent = new StringContent(res, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("http://localhost:5249/messages", jsonContent).ConfigureAwait(true);

        response.EnsureSuccessStatusCode();
    }

    public async Task ConnectAsync(string uri)
    {
        try
        {
            await _wsClient.ConnectAsync(new Uri(uri), CancellationToken.None).ConfigureAwait(true);
        }
        catch(Exception ex)
        {
            // ignore
        }
    }

    public async IAsyncEnumerable<string> ReceiveMessagesAsync()
    {
        var buffer = new byte[1024];
        while(_wsClient.State == WebSocketState.Open)
        {
            var result = await _wsClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(true);
            if (result.EndOfMessage)
            {
                yield return Encoding.UTF8.GetString(buffer, 0, result.Count);
            }
        }
    }
}