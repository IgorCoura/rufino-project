namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Corrige o cadastro de uma expectativa.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O beneficiário não está aqui, e é decisão de produto.</strong> Trocá-lo mudaria de
/// quem o sistema espera receber — outra expectativa, não a mesma corrigida —, e os ciclos já
/// abertos passariam a esperar uma conta que nunca teve relação com eles. Quem quer outro
/// beneficiário exclui e cadastra de novo.
/// </para>
/// <para>
/// A vigilância (pausar, retomar, desativar) também não está: ela tem o
/// <c>AlterBillExpectationWatchCommand</c>. Dois comandos escrevendo o mesmo estado é como um
/// deles envelhece.
/// </para>
/// </remarks>
public sealed record EditBillExpectationCommand(
    Guid TenantId,
    Guid ExpectationId,
    string? AccountReference,
    string Label,
    string Recurrence,
    int ExpectedDueDay,
    int ObservedLeadDays,
    int? AlertLeadDays,
    DateOnly? FirstDueDate) : ITenantScopedCommand, IRequest<EditBillExpectationResponse>;

public sealed record EditBillExpectationResponse(Guid Id, string Label, int AlertLeadDays);

public sealed class EditBillExpectationCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<EditBillExpectationCommand, EditBillExpectationResponse>
{
    public async Task<EditBillExpectationResponse> Handle(
        EditBillExpectationCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var expectationId = BillExpectationId.From(request.ExpectationId);

        var expectation = await expectations.GetAsync(tenantId, expectationId, cancellationToken)
            ?? throw BillExpectationErrors.NotFound(request.ExpectationId);

        var reference = request.AccountReference?.Trim() ?? string.Empty;

        // A chave é (tenant, beneficiário, referência), e só a referência é editável — mas ela
        // basta para colidir com uma conta irmã do mesmo beneficiário.
        if (await expectations.ExistsAsync(
                tenantId, expectation.PayeeId, reference, expectationId, cancellationToken))
        {
            throw BillExpectationErrors.AlreadyExists();
        }

        expectation.Reconfigure(
            reference,
            request.Label,
            Enumeration.FromDisplayName<Recurrence>(request.Recurrence),
            request.ExpectedDueDay,
            request.ObservedLeadDays,
            request.AlertLeadDays,
            request.FirstDueDate,
            clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new EditBillExpectationResponse(
            expectation.Id.Value, expectation.Label, expectation.AlertLeadDays);
    }
}

public sealed class EditBillExpectationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<EditBillExpectationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<EditBillExpectationCommand, EditBillExpectationResponse>(
        mediator, requestManager, logger)
{
    protected override EditBillExpectationResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, 0);
}
