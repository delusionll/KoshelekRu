namespace KoshelekRuWebService;

using Domain;

using Npgsql;

using NpgsqlTypes;

internal sealed class MessageNpgRepository(IConfiguration config, ILogger<MessageNpgRepository> logger)
{
    private readonly string _connectionStr = config.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string not found");

    public async Task<int> InsertMessageAsync(Message mess)
    {
        try
        {
            // TODO disposeasync configureawait false???
            await using var connection = new NpgsqlConnection(_connectionStr);
            await connection.OpenAsync().ConfigureAwait(false);

            // TODO optimize
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO messages.messages (id, content, time, sernumber) VALUES (@Id, @Content, @Time, @SerNumber);";
            cmd.Parameters.AddWithValue("@Id", NpgsqlDbType.Uuid, mess.Id);
            cmd.Parameters.AddWithValue("@Content", NpgsqlDbType.Varchar, mess.Content);
            cmd.Parameters.AddWithValue("@Time", NpgsqlDbType.Timestamp, mess.Time);
            cmd.Parameters.AddWithValue("@SerNumber", NpgsqlDbType.Integer, mess.SerNumber);
            return await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // TODO log
            throw;
        }
    }
}
