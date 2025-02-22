using System.Net.WebSockets;
using System.Text.Json;

using Domain;

using KoshelekRuWebService;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
ConfigureServices(builder.Services);
WebApplication app = builder.Build();
app.UseWebSockets();

app.Map("/ws", static async (HttpContext context, MyWebSocketManager wsManager) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        Guid id = wsManager.Add(ws);

        try
        {
            await MyWebSocketManager.ListenWebSocket(ws).ConfigureAwait(false);
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

app.MapGet("/lastmessages", async (MessageNpgRepository repo, DateTime? from, DateTime? to) =>
{
    from ??= DateTime.UtcNow.AddMinutes(-10);
    to ??= DateTime.UtcNow;
    string que = $@"SELECT time, sernumber, content 
                    FROM messages.messages
                    WHERE time BETWEEN @from AND @to";
    IAsyncEnumerable<Message> lastMessages = repo.GetRawAsync(que, [("@from", from.Value), ("@to", to.Value)]);
    IList<Message> messagesList = [];
    await foreach (Message? m in lastMessages.ConfigureAwait(false))
    {
        messagesList.Add(m);
    }

    return messagesList.Count > 0 ? Results.Content(JsonSerializer.Serialize(messagesList)) : Results.NotFound();
});

app.MapPost("/messages", static async (Message message, MessageNpgRepository repo, MyWebSocketManager wsManager) =>
{
    if (string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 128)
    {
        return Results.BadRequest("message length is more than 128");
    }

    try
    {
        System.Runtime.CompilerServices.ConfiguredTaskAwaitable<int> repoTask = repo.InsertMessageAsync(message).ConfigureAwait(false);
        foreach (WebSocket c in wsManager.Clients.Values)
        {
            if (c.State == WebSocketState.Open)
            {
                // TODO arraypool, json compile time serialize
                byte[] res = JsonSerializer.SerializeToUtf8Bytes(message);
                await c.SendAsync(new ArraySegment<byte>(res), WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

        return await repoTask == 1 ? Results.Created() : Results.BadRequest();
    }
    catch (Exception)
    {
        return Results.Problem();
    }
});

app.Run();

static void ConfigureServices(IServiceCollection col)
{
    col.AddLogging(logger =>
    logger
        .AddSimpleConsole()
        .SetMinimumLevel(LogLevel.Trace));

    col.AddScoped<MessageNpgRepository>();
    IConfigurationRoot config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    col.AddSingleton<IConfiguration>(config);
    col.AddSingleton<MyWebSocketManager>();
}
