using KoshelekRuWebService;

var builder = WebApplication.CreateSlimBuilder();
ConfigureServices(builder.Services);
var app = builder.Build();

app.MapPost("/messages", async (Message message) =>
{
    if (string.IsNullOrWhiteSpace(message.Content) || message.Content.Length > 128)
    {
        return Results.BadRequest("Текст должен быть от 1 до 128 символов");
    }

    try
    {
        var repo = app.Services.GetRequiredService<MessageNpgRepository>();
        var res = await repo.InsertMessageAsync(message).ConfigureAwait(false);
        return res == 1 ? Results.Created() : Results.BadRequest();
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
    col.AddSingleton<MessageNpgRepository>();
    var config = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .AddEnvironmentVariables()
        .Build();

    col.AddSingleton<IConfiguration>(config);
}
