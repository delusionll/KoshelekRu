namespace KoshelekRuWebService;

public sealed record class Message
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public string Content { get; init; }

    public DateTime Time { get; init; } = DateTime.Now;

    public int SerNumber { get; init; }
}
