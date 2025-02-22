namespace Domain;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(IList<Message>))]
public partial class MyJsonContext : JsonSerializerContext
{
}
