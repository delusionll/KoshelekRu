namespace Domain;

public sealed record class Message
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Content { get; init; }

    public DateTime Time { get; init; } = DateTime.Now;

    public required int SerNumber { get; init; }
}
