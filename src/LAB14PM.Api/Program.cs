using LAB14PM.Api.Application.Tickets;
using LAB14PM.Api.Application.Tickets.Commands;
using LAB14PM.Api.Application.Tickets.Queries;
using LAB14PM.Api.Infrastructure.Persistence;
using MediatR;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Falta configurar ConnectionStrings:Default.");
builder.Services.AddSingleton(new TicketRepository(connectionString));
builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok", project = "LAB14PM" }))
   .WithName("HealthCheck");

var tickets = app.MapGroup("/api/tickets").WithTags("Tickets");
tickets.MapGet("/", async (ISender sender, CancellationToken ct) =>
    Results.Ok(await sender.Send(new GetTicketsQuery(), ct)))
    .WithName("GetTickets");

tickets.MapGet("/{id:guid}", async Task<IResult> (Guid id, ISender sender, CancellationToken ct) =>
{
    var ticket = await sender.Send(new GetTicketByIdQuery(id), ct);
    return ticket is null ? Results.NotFound() : Results.Ok(ticket);
}).WithName("GetTicketById");

tickets.MapPost("/", async Task<IResult> (CreateTicketRequest request, ISender sender, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Description))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["Título y descripción son obligatorios."] });
    var created = await sender.Send(new CreateTicketCommand(request.Title, request.Description, request.CreatedByUserId, request.AssignedToUserId), ct);
    return Results.Created($"/api/tickets/{created.Id}", created);
}).WithName("CreateTicket");

tickets.MapPatch("/{id:guid}/status", async Task<IResult> (Guid id, UpdateTicketStatusRequest request, ISender sender, CancellationToken ct) =>
{
    var allowed = new[] { "open", "in_progress", "resolved", "closed" };
    if (!allowed.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Usa open, in_progress, resolved o closed."] });
    var updated = await sender.Send(new UpdateTicketStatusCommand(id, request.Status.ToLowerInvariant()), ct);
    return updated ? Results.NoContent() : Results.NotFound();
}).WithName("UpdateTicketStatus");

tickets.MapPost("/{id:guid}/responses", async Task<IResult> (Guid id, AddTicketResponseRequest request, ISender sender, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["message"] = ["El mensaje es obligatorio."] });
    var response = await sender.Send(new AddTicketResponseCommand(id, request.UserId, request.Message), ct);
    return response is null ? Results.NotFound() : Results.Created($"/api/tickets/{id}/responses/{response.Id}", response);
}).WithName("AddTicketResponse");

app.Run();

public partial class Program { }


