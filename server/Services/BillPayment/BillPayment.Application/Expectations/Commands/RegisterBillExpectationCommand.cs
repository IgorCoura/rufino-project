namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cadastra à mão o que o sistema deve esperar receber.
/// </summary>
/// <remarks>
/// <strong>É o caminho principal quando o tenant tem mais de uma conta do mesmo beneficiário</strong>
/// — medido no arquivo real: quatro instalações da EDP e três do DAE. A referência de conta está
/// no código de barras em arrecadação, mas em posição que muda por emissor, então o aprendizado
/// automático não a deduz: quem a informa é quem cadastra, e é ela que separa uma conta da outra.
/// </remarks>
public sealed record RegisterBillExpectationCommand(
    Guid TenantId,
    Guid PayeeId,
    string? AccountReference,
    string Label,
    string Recurrence,
    int ExpectedDueDay,
    int ObservedLeadDays,
    int? AlertLeadDays,
    DateOnly? FirstDueDate,
    Guid? HintSourceId) : ITenantScopedCommand, IRequest<RegisterBillExpectationResponse>;

public sealed record RegisterBillExpectationResponse(Guid Id, string Label, int AlertLeadDays);

public sealed class RegisterBillExpectationCommandHandler(
    IBillExpectationRepository expectations,
    IPayeeRepository payees,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterBillExpectationCommand, RegisterBillExpectationResponse>
{
    public async Task<RegisterBillExpectationResponse> Handle(
        RegisterBillExpectationCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var payeeId = PayeeId.From(request.PayeeId);

        if (await payees.GetAsync(tenantId, payeeId, cancellationToken) is null)
            throw PayeeErrors.NotFound(request.PayeeId);

        var reference = request.AccountReference?.Trim() ?? string.Empty;

        if (await expectations.ExistsAsync(tenantId, payeeId, reference, cancellationToken: cancellationToken))
            throw BillExpectationErrors.AlreadyExists();

        var expectation = BillExpectation.Register(
            tenantId,
            payeeId,
            reference,
            request.Label,
            Enumeration.FromDisplayName<Recurrence>(request.Recurrence),
            request.ExpectedDueDay,
            request.ObservedLeadDays,
            request.AlertLeadDays,
            request.FirstDueDate,
            request.HintSourceId is { } sourceId ? CaptureSourceId.From(sourceId) : null,
            clock.GetUtcNow().UtcDateTime);

        await expectations.AddAsync(expectation, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterBillExpectationResponse(
            expectation.Id.Value, expectation.Label, expectation.AlertLeadDays);
    }
}

public sealed class RegisterBillExpectationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterBillExpectationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterBillExpectationCommand, RegisterBillExpectationResponse>(
        mediator, requestManager, logger)
{
    protected override RegisterBillExpectationResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0);
}
