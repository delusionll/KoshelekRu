using System.Net.WebSockets;
using System.Text.Json;

using Domain;

using KoshelekRuWebService;

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
