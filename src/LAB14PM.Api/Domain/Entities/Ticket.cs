namespace LAB14PM.Api.Domain.Entities;

public sealed record Ticket(
    Guid Id,
    string Title,
    string Description,
    string Status,
    Guid CreatedByUserId,
    Guid? AssignedToUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record TicketResponse(
    Guid Id,
    Guid TicketId,
    Guid UserId,
    string Message,
    DateTime CreatedAt);
