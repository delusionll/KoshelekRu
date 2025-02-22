using System.Net.WebSockets;
using System.Text.Json;

using Domain;

using KoshelekRuWebService;

using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateSlimBuilder();
ConfigureServices(builder.Services);
var app = builder.Build();
app.UseWebSockets();

app.Map("/ws", static async (HttpContext context, MyWebSocketManager wsManager) =>
{
    if(context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        var id = wsManager.Add(ws);

        try
        {
            await wsManager.ListenWebSocket(ws).ConfigureAwait(false);
        }
        finally
        {
            await wsManager.RemoveSocket(id).ConfigureAwait(false);
        }
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.MapGet("/lastmessages", async (MessageNpgRepository repo) =>
{
    string que = "SELECT time, sernumber, content FROM messages.messages WHERE time >= NOW() - INTERVAL '10 minutes';";
    var lastMessages = repo.GetRawAsync(que);
    IList<Message> messagesList = [];
    await foreach (var m in lastMessages.ConfigureAwait(false))
    {
        messagesList.Add(m);
    }
    if (messagesList.Count > 0)
    {
        return Results.Content(JsonSerializer.Serialize(messagesList));
    }
    return Results.NotFound();
});

app.MapPost("/messages", static async (Message message, MessageNpgRepository repo, MyWebSocketManager wsManager) =>
{
    if(string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 128)
    {
        return Results.BadRequest("message length is more than 128");
    }

    try
    {
        var repoTask = repo.InsertMessageAsync(message).ConfigureAwait(false);
        foreach(var c in wsManager.Clients.Values)
        {
            if(c.State == WebSocketState.Open)
            {
                // TODO arraypool, json compile time serialize
                var res = JsonSerializer.SerializeToUtf8Bytes(message);
                await c.SendAsync(new ArraySegment<byte>(res), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

        return await repoTask == 1 ? Results.Created() : Results.BadRequest();
    }
    catch(Exception ex)
    {
        return Results.Problem();
    }
});

app.Run();

void ConfigureServices(IServiceCollection col)
{
    col.AddLogging();
    col.AddScoped<MessageNpgRepository>();
    var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    col.AddSingleton<IConfiguration>(config);
    col.AddSingleton<MyWebSocketManager>();
}
