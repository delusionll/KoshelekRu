namespace KoshelekRuWebService;

using System.Text.Json.Serialization;

using Domain;

[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(IList<Message>))]
public partial class MyJsonContext : JsonSerializerContext
{
}
