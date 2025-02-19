namespace Clients;

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using Domain;

public class MessageService(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    private int _count = 1;

    public async Task SendMessageAsync(string content)
    {
        var mess = new Message() { Content = content, SerNumber = _count++ };
        var res = JsonSerializer.Serialize(mess);
        using var jsonContent = new StringContent(res, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync("http://localhost:5249/messages", jsonContent).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
    }
}