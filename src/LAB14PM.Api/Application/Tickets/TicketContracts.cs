using LAB14PM.Api.Domain.Entities;
using LAB14PM.Api.Infrastructure.Persistence;
using MediatR;

namespace LAB14PM.Api.Application.Tickets;

public sealed record TicketDto(Guid Id, string Title, string Description, string Status, Guid CreatedByUserId, Guid? AssignedToUserId, DateTime CreatedAt, DateTime UpdatedAt)
{
    public static TicketDto From(Ticket ticket) => new(ticket.Id, ticket.Title, ticket.Description, ticket.Status, ticket.CreatedByUserId, ticket.AssignedToUserId, ticket.CreatedAt, ticket.UpdatedAt);
}

public sealed record TicketResponseDto(Guid Id, Guid TicketId, Guid UserId, string Message, DateTime CreatedAt)
{
    public static TicketResponseDto From(TicketResponse response) => new(response.Id, response.TicketId, response.UserId, response.Message, response.CreatedAt);
}

public sealed record CreateTicketRequest(string Title, string Description, Guid CreatedByUserId, Guid? AssignedToUserId);
public sealed record UpdateTicketStatusRequest(string Status);
public sealed record AddTicketResponseRequest(Guid UserId, string Message);

public interface ITicketRepository
{
    Task<IReadOnlyList<Ticket>> GetAllAsync(CancellationToken cancellationToken);
    Task<Ticket?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Ticket> CreateAsync(Ticket ticket, CancellationToken cancellationToken);
    Task<bool> UpdateStatusAsync(Guid id, string status, CancellationToken cancellationToken);
    Task<TicketResponse?> AddResponseAsync(Guid ticketId, Guid userId, string message, CancellationToken cancellationToken);
}
