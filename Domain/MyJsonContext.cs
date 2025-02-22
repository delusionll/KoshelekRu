namespace Domain;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(IAsyncEnumerable<Message>))]
public partial class MyJsonContext : JsonSerializerContext
{
}
