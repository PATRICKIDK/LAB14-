using LAB14PM.Api.Application.Tickets;
using LAB14PM.Api.Domain.Entities;
using MySqlConnector;

namespace LAB14PM.Api.Infrastructure.Persistence;

public sealed class TicketRepository(string connectionString) : ITicketRepository
{
    public async Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at FROM tickets ORDER BY created_at DESC";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<Ticket>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(ReadTicket(reader));
        return result;
    }

    public async Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at FROM tickets WHERE id = @id";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id.ToString());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTicket(reader) : null;
    }

    public async Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO tickets (id, title, description, status, created_by_user_id, assigned_to_user_id, created_at, updated_at) VALUES (@id, @title, @description, @status, @createdBy, @assignedTo, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", ticket.Id.ToString());
        command.Parameters.AddWithValue("@title", ticket.Title);
        command.Parameters.AddWithValue("@description", ticket.Description);
        command.Parameters.AddWithValue("@status", ticket.Status);
        command.Parameters.AddWithValue("@createdBy", ticket.CreatedByUserId.ToString());
        command.Parameters.AddWithValue("@assignedTo", (object?)ticket.AssignedToUserId?.ToString() ?? DBNull.Value);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return await GetByIdAsync(ticket.Id, cancellationToken) ?? ticket;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE tickets SET status = @status, updated_at = UTC_TIMESTAMP(6) WHERE id = @id";
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@status", status);
        command.Parameters.AddWithValue("@id", id.ToString());
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<TicketResponse?> AddResponseAsync(Guid ticketId, Guid userId, string message, CancellationToken cancellationToken)
    {
        const string sql = "INSERT INTO ticket_responses (id, ticket_id, user_id, message, created_at) SELECT @id, t.id, @userId, @message, UTC_TIMESTAMP(6) FROM tickets t WHERE t.id = @ticketId";
        var response = new TicketResponse(Guid.NewGuid(), ticketId, userId, message, DateTime.UtcNow);
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", response.Id.ToString());
        command.Parameters.AddWithValue("@ticketId", ticketId.ToString());
        command.Parameters.AddWithValue("@userId", userId.ToString());
        command.Parameters.AddWithValue("@message", message);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 0 ? null : response;
    }

    private static Ticket ReadTicket(MySqlDataReader reader) => new(
        Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
        reader.GetString(reader.GetOrdinal("title")),
        reader.GetString(reader.GetOrdinal("description")),
        reader.GetString(reader.GetOrdinal("status")),
        Guid.Parse(reader.GetString(reader.GetOrdinal("created_by_user_id"))),
        reader.IsDBNull(reader.GetOrdinal("assigned_to_user_id")) ? null : Guid.Parse(reader.GetString(reader.GetOrdinal("assigned_to_user_id"))),
        reader.GetDateTime(reader.GetOrdinal("created_at")),
        reader.GetDateTime(reader.GetOrdinal("updated_at")));
}

