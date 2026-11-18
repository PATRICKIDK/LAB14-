using LAB14PM.Api.Application.Tickets;
using LAB14PM.Api.Domain.Entities;
using MediatR;

namespace LAB14PM.Api.Application.Tickets.Queries;

public sealed record GetTicketsQuery : IRequest<IReadOnlyList<TicketDto>>;
public sealed record GetTicketByIdQuery(Guid Id) : IRequest<TicketDto?>;

public sealed class GetTicketsHandler(ITicketRepository repository) : IRequestHandler<GetTicketsQuery, IReadOnlyList<TicketDto>>
{
    public async Task<IReadOnlyList<TicketDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken) =>
        (await repository.GetAllAsync(cancellationToken)).Select(TicketDto.From).ToArray();
}

public sealed class GetTicketByIdHandler(ITicketRepository repository) : IRequestHandler<GetTicketByIdQuery, TicketDto?>
{
    public async Task<TicketDto?> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await repository.GetByIdAsync(request.Id, cancellationToken);
        return ticket is null ? null : TicketDto.From(ticket);
    }
}
