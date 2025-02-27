using System.Net.Mime;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Text.Json;

using Domain;

using KoshelekRuWebService;

using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateSlimBuilder();
ConfigureServices(builder.Services);
builder.WebHost.UseUrls("http://*:5249");
var app = builder.Build();
app.UseWebSockets();
app.UseStaticFiles();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapHealthChecks("/healthz");
app.MapGet("/", () => "Hi there!");
app.Map("/ws", async (HttpContext context, MyWebSocketManager wsManager) =>
{
    MyLogger.Info(logger, "Handling ws controller...");
    if (context.WebSockets.IsWebSocketRequest)
    {
        var ws = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        await wsManager.StartListening(ws).ConfigureAwait(false);
        return;
    }

    MyLogger.Error(logger, $"bad request for {context.TraceIdentifier}");
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
});

app.MapGet("/lastmessages", async (MessageNpgRepository repo, DateTime? from, DateTime? to, HttpResponse response) =>
{
    MyLogger.Info(logger, $"Handling /lastmessages controller.... from:{from}; to: {to}");
    try
    {
        from ??= DateTime.UtcNow.AddMinutes(-10);
        to ??= DateTime.UtcNow;
        var que = $@"SELECT time, sernumber, content 
                        FROM messages
                        WHERE time BETWEEN @from AND @to";

        var lastMessages = repo.GetRawAsync(que, [("@from", from.Value), ("@to", to.Value)]);
        response.ContentType = MediaTypeNames.Application.Json;
        response.StatusCode = StatusCodes.Status200OK;
        await JsonSerializer.SerializeAsync(response.BodyWriter, lastMessages, MyJsonContext.Default.IAsyncEnumerableMessage).ConfigureAwait(false);
        await response.BodyWriter.CompleteAsync().ConfigureAwait(false);
    }
    catch (Exception e)
    {
        MyLogger.Error(logger, "Error handling /lastmessages", e);
        response.StatusCode = StatusCodes.Status500InternalServerError;
        await response.WriteAsync("Internal Server Error").ConfigureAwait(false);
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
        var repoTask = repo.InsertMessageAsync(message).ConfigureAwait(false);
        ReadOnlyMemory<byte> res = JsonSerializer.SerializeToUtf8Bytes(message, MyJsonContext.Default.Message);
        var sendTasks = wsManager.Clients.Values
                    .Where(c => c.State == WebSocketState.Open)
                    .Select(c => c.SendAsync(res, WebSocketMessageType.Text, true, CancellationToken.None).AsTask());

        await Task.WhenAll(sendTasks).ConfigureAwait(false);
        return await repoTask == 1 ? Results.Created() : Results.BadRequest();
    }
    catch (Exception ex)
    {
        MyLogger.Error(logger, $"Error handling messages controller.", ex);
        return Results.Problem("Error while processing request.");
    }
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KoshelekRuAPI v1");
});

try
{
    MyLogger.Info(logger, "running app.");
    app.Run();
}
catch (OperationCanceledException oCe)
{
    MyLogger.Info(logger, "Operation cancelled");
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
    IConfiguration config = new ConfigurationBuilder()
         .AddUserSecrets<Program>()
         .AddEnvironmentVariables()
         .Build();

    col.AddSingleton(config);
    col.AddSingleton<MyWebSocketManager>();
    col.AddSingleton<CancellationTokenSource>();
    col.AddEndpointsApiExplorer();
    col.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "KoshelekRuAPI",
            Version = "v1",
        });
    });

    col.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>("regex"));
    col.AddHealthChecks();
}
