using System.Buffers;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Domain;

using KoshelekRuWebService;

WebApplicationBuilder builder = WebApplication.CreateSlimBuilder();
ConfigureServices(builder.Services);
WebApplication app = builder.Build();
app.UseWebSockets();
ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();

app.Map("/ws", async (HttpContext context, MyWebSocketManager wsManager) =>
{
    MyLogger.Info(logger, "Handling ws controller...");
    if (context.WebSockets.IsWebSocketRequest)
    {
        WebSocket ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        Guid id = wsManager.Add(ws);

        try
        {
            MyLogger.Info(logger, $"...listening ws {id}...");
            await wsManager.Listen(ws).ConfigureAwait(false);
        }
        finally
        {
            await wsManager.RemoveSocket(id).ConfigureAwait(false);
        }
    }
    else
    {
        MyLogger.Error(logger, $"bad request for {context.TraceIdentifier}");
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

app.MapGet("/lastmessages", async (MessageNpgRepository repo, DateTime? from, DateTime? to) =>
{
    MyLogger.Info(logger, "Handling /lastmessages controller...");
    try
    {
        from ??= DateTime.UtcNow.AddMinutes(-10);
        to ??= DateTime.UtcNow;
        string que = $@"SELECT time, sernumber, content 
                        FROM messages.messages
                        WHERE time BETWEEN @from AND @to";
        IAsyncEnumerable<Message> lastMessages = repo.GetRawAsync(que, [("@from", from.Value), ("@to", to.Value)]);
        IList<Message> messagesList = [];
        await foreach (Message m in lastMessages.ConfigureAwait(false))
        {
            messagesList.Add(m);
        }

        IResult res = messagesList.Count > 0
            ? Results.Content(JsonSerializer.Serialize(messagesList, MyJsonContext.Default.IListMessage))
            : Results.NotFound();
        MyLogger.Info(logger, $".../lastmessages handled.");
        return res;
    }
    catch (Exception e)
    {
        MyLogger.Error(logger, "Error handling /lastmessages", e);
        throw;
    }
});

app.MapPost("/messages", async (Message message, MessageNpgRepository repo, MyWebSocketManager wsManager) =>
{
    MyLogger.Info(logger, "Handling messages controller...");
    if (string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 128)
    {
        MyLogger.Info(logger, "...Message is either too long or whitespace.");
        return Results.BadRequest("message length is more than 128");
    }

    try
    {
        ConfiguredTaskAwaitable<int> repoTask = repo.InsertMessageAsync(message).ConfigureAwait(false);

        byte[] arr = ArrayPool<byte>.Shared.Rent(128);

        foreach (WebSocket c in wsManager.Clients.Values)
        {
            if (c.State == WebSocketState.Open)
            {
                ReadOnlyMemory<byte> res = JsonSerializer.SerializeToUtf8Bytes(message, MyJsonContext.Default.Message);
                await c.SendAsync(res, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
            }
        }

        return await repoTask == 1 ? Results.Created() : Results.BadRequest();
    }
    catch (Exception ex)
    {
        MyLogger.Info(logger, $"Error handling messages controller.", ex);
        return Results.Problem();
        throw;
    }
});

try
{
    app.Run();
    MyLogger.Info(logger, "running app.");
}
catch (OperationCanceledException oCe)
{
    MyLogger.Info(logger, "Operation cancelled", oCe);
}
catch (Exception ex)
{
    MyLogger.Error(logger, "something went wronge", ex);
}

static void ConfigureServices(IServiceCollection col)
{
    col.AddLogging(static logger =>
    logger
        .AddSimpleConsole()
        .SetMinimumLevel(LogLevel.Trace))
        .AddHttpLogging();

    col.AddScoped<MessageNpgRepository>();
    IConfigurationRoot config = new ConfigurationBuilder()
         .AddUserSecrets<Program>()
         .AddEnvironmentVariables()
         .Build();

    col.AddSingleton<IConfiguration>(config);
    col.AddSingleton<MyWebSocketManager>();
    col.AddSingleton<CancellationTokenSource>();
}
