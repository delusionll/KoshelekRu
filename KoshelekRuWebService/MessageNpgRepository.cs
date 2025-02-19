namespace KoshelekRuWebService;

using System.Data;

using Domain;

using Npgsql;

using NpgsqlTypes;

internal sealed class MessageNpgRepository(IConfiguration config, ILogger<MessageNpgRepository> logger) : IDisposable
{
    private readonly NpgsqlConnection _connection = new NpgsqlConnection(config.GetConnectionString("Default"));

    public async Task<int> InsertMessageAsync(Message mess)
    {
        try
        {
            // TODO optimize
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = "INSERT INTO messages.messages (id, content, time, sernumber) VALUES (@Id, @Content, @Time, @SerNumber);";
            cmd.Parameters.AddWithValue("@Id", NpgsqlDbType.Uuid, mess.Id);
            cmd.Parameters.AddWithValue("@Content", NpgsqlDbType.Varchar, mess.Content);
            cmd.Parameters.AddWithValue("@Time", NpgsqlDbType.Timestamp, mess.Time);
            cmd.Parameters.AddWithValue("@SerNumber", NpgsqlDbType.Integer, mess.SerNumber);

            if(_connection.FullState is ConnectionState.Closed)
            {
                await _connection.OpenAsync().ConfigureAwait(false);
            }

            return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        finally
        {
            await _connection.CloseAsync().ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
