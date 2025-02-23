namespace KoshelekRuWebService;

using Domain;

using Npgsql;

using NpgsqlTypes;

internal sealed class MessageNpgRepository(IConfiguration config, ILogger<MessageNpgRepository> logger)
{
    private const string Ins = @"INSERT INTO messages.messages (id, content, time, sernumber) VALUES (@Id, @Content, @Time, @SerNumber);";
    private readonly string _connectionStr = config.GetConnectionString("DefaultConnection")
                                             ?? throw new InvalidOperationException("Connection string not found");

    public async Task<int> InsertMessageAsync(Message mess)
    {
        try
        {
            using NpgsqlConnection connection = await GetConnectionAsync().ConfigureAwait(false)
                ?? throw new InvalidOperationException("no connection");

            using NpgsqlCommand cmd = GenerateInsertCmd(mess, connection);
            int res = await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            MyLogger.Info(logger, $"insert message {mess.Id} to db.");
            return res;
        }
        catch (Exception ex)
        {
            MyLogger.Error(logger, $"error while inserting message {mess.Id} to db", ex);
            throw;
        }
    }

    internal async IAsyncEnumerable<Message> GetRawAsync<T>(
        string rawQuery, IEnumerable<(string Param, T Value)> parameters)
        where T : struct
    {
        using NpgsqlConnection connection = await GetConnectionAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("no connection");
        using NpgsqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = rawQuery;
        foreach ((string Param, T Value) p in parameters)
        {
            cmd.Parameters.AddWithValue(p.Param, p.Value);
        }

        NpgsqlDataReader res = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await res.ReadAsync().ConfigureAwait(false))
        {
            var m = new Message()
            {
                Time = res.GetDateTime(res.GetOrdinal("time")),
                SerNumber = res.GetInt32(res.GetOrdinal("sernumber")),
                Content = res.GetString(res.GetOrdinal("content")),
            };
            yield return m;
        }
    }

    private static NpgsqlCommand GenerateInsertCmd(Message mess, NpgsqlConnection connection)
    {
        // TODO optimize
        NpgsqlCommand cmd = connection.CreateCommand();
        cmd.CommandText = Ins;
        cmd.Parameters.AddWithValue("@Id", NpgsqlDbType.Uuid, mess.Id);
        cmd.Parameters.AddWithValue("@Content", NpgsqlDbType.Varchar, mess.Content);
        cmd.Parameters.AddWithValue("@Time", NpgsqlDbType.Timestamp, mess.Time);
        cmd.Parameters.AddWithValue("@SerNumber", NpgsqlDbType.Integer, mess.SerNumber);
        return cmd;
    }

    private async Task<NpgsqlConnection?> GetConnectionAsync()
    {
        try
        {
            var connection = new NpgsqlConnection(_connectionStr);
            await connection.OpenAsync().ConfigureAwait(false);
            return connection;
        }
        catch (Exception ex)
        {
            MyLogger.Error(logger, $"unable to establish npgsql connection.", ex);
            return null;
        }
    }
}
