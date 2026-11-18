using LAB14PM.Api.Application.Tickets;
using LAB14PM.Api.Domain.Entities;
using MediatR;

namespace LAB14PM.Api.Application.Tickets.Commands;

public sealed record CreateTicketCommand(string Title, string Description, Guid CreatedByUserId, Guid? AssignedToUserId) : IRequest<TicketDto>;
public sealed record UpdateTicketStatusCommand(Guid Id, string Status) : IRequest<bool>;
public sealed record AddTicketResponseCommand(Guid TicketId, Guid UserId, string Message) : IRequest<TicketResponseDto?>;

public sealed class CreateTicketHandler(ITicketRepository repository) : IRequestHandler<CreateTicketCommand, TicketDto>
{
    public async Task<TicketDto> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var ticket = new Ticket(Guid.NewGuid(), request.Title.Trim(), request.Description.Trim(), "open", request.CreatedByUserId, request.AssignedToUserId, now, now);
        return TicketDto.From(await repository.CreateAsync(ticket, cancellationToken));
    }
}

public sealed class UpdateTicketStatusHandler(ITicketRepository repository) : IRequestHandler<UpdateTicketStatusCommand, bool>
{
    public Task<bool> Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken) =>
        repository.UpdateStatusAsync(request.Id, request.Status, cancellationToken);
}

public sealed class AddTicketResponseHandler(ITicketRepository repository) : IRequestHandler<AddTicketResponseCommand, TicketResponseDto?>
{
    public async Task<TicketResponseDto?> Handle(AddTicketResponseCommand request, CancellationToken cancellationToken)
    {
        var response = await repository.AddResponseAsync(request.TicketId, request.UserId, request.Message.Trim(), cancellationToken);
        return response is null ? null : TicketResponseDto.From(response);
    }
}
