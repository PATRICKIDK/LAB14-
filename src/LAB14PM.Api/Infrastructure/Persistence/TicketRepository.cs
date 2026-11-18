using LAB14PM.Api.Application.Tickets;
using LAB14PM.Api.Domain.Entities;
using Npgsql;
using NpgsqlTypes;

namespace LAB14PM.Api.Infrastructure.Persistence;

public sealed class TicketRepository : ITicketRepository
{
    private readonly string _connectionString;

    public TicketRepository(string connectionString) => _connectionString = NormalizeConnectionString(connectionString);

    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at FROM tickets ORDER BY created_at DESC";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<Ticket>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadTicket(reader));
        return result;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at FROM tickets WHERE id = @id";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTicket(reader) : null;
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO tickets (id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at) VALUES (@id, @title, @description, @status, @createdBy, @assignedTo, NOW(), NOW())";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, ticket.Id);
        command.Parameters.AddWithValue("title", NpgsqlDbType.Varchar, ticket.Title);
        command.Parameters.AddWithValue("description", NpgsqlDbType.Text, ticket.Description);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, ticket.Status);
        command.Parameters.AddWithValue("createdBy", NpgsqlDbType.Uuid, ticket.CreatedByUserId);
        command.Parameters.AddWithValue("assignedTo", NpgsqlDbType.Uuid, (object?)ticket.AssignedToUserId ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return await GetByIdAsync(ticket.Id, cancellationToken) ?? ticket;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE tickets SET status = @status, updated_at = NOW() WHERE id = @id";
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("status", NpgsqlDbType.Varchar, status);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, id);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<TicketResponse?> AddResponseAsync(Guid ticketId, Guid userId, string message, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO ticket_responses (id, ticket_id, user_id, message, created_at) SELECT @id, t.id, @userId, @message, NOW() FROM tickets t WHERE t.id = @ticketId";
        var response = new TicketResponse(Guid.NewGuid(), ticketId, userId, message, DateTime.UtcNow);
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, response.Id);
        command.Parameters.AddWithValue("ticketId", NpgsqlDbType.Uuid, ticketId);
        command.Parameters.AddWithValue("userId", NpgsqlDbType.Uuid, userId);
        command.Parameters.AddWithValue("message", NpgsqlDbType.Text, message);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 0 ? null : response;
    }

    private static Ticket ReadTicket(NpgsqlDataReader reader) => new(
        reader.GetGuid(reader.GetOrdinal("id")),
        reader.GetString(reader.GetOrdinal("title")),
        reader.GetString(reader.GetOrdinal("description")),
        reader.GetString(reader.GetOrdinal("status")),
        reader.GetGuid(reader.GetOrdinal("created_by_user_id")),
        reader.IsDBNull(reader.GetOrdinal("assigned_to_user_id")) ? null : reader.GetGuid(reader.GetOrdinal("assigned_to_user_id")),
        reader.GetDateTime(reader.GetOrdinal("created_at")),
        reader.GetDateTime(reader.GetOrdinal("updated_at")));

    private static string NormalizeConnectionString(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
            return connectionString;

        var credentials = uri.UserInfo.Split(':', 2);
        if (credentials.Length != 2)
            throw new ArgumentException("La URL de PostgreSQL debe incluir usuario y contraseña.", nameof(connectionString));

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1])
        };

        var sslMode = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .FirstOrDefault(pair => pair.Length == 2 && Uri.UnescapeDataString(pair[0]).Equals("sslmode", StringComparison.OrdinalIgnoreCase));
        if (sslMode is { Length: 2 } && Uri.UnescapeDataString(sslMode[1]).Equals("require", StringComparison.OrdinalIgnoreCase))
            builder.SslMode = SslMode.Require;

        return builder.ConnectionString;
    }
}

