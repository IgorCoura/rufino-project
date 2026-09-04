namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

public sealed record RegisterPayeeCommand(
    Guid TenantId,
    string LegalName,
    string TaxId,
    string AmountPolicyKind,
    decimal? ExpectedAmount,
    decimal? TolerancePercent,
    decimal? MinAmount,
    decimal? MaxAmount) : ITenantScopedCommand, IRequest<RegisterPayeeResponse>;

public sealed record RegisterPayeeResponse(Guid Id);

public sealed class RegisterPayeeCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RegisterPayeeCommand, RegisterPayeeResponse>
{
    public async Task<RegisterPayeeResponse> Handle(
        RegisterPayeeCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        // Tradução de input: converter string em Smart Enum é responsabilidade do handler.
        var policyKind = Enumeration.FromDisplayName<AmountPolicyKind>(request.AmountPolicyKind);

        // O documento define a identidade do beneficiário, então a duplicata é checada por
        // ele. Parse aqui é leitura, não composição de estado: o cadastro em si é montado
        // pela factory do agregado logo abaixo.
        var taxId = TaxId.Parse(request.TaxId);

        if (await repository.ExistsAsync(tenantId, taxId, cancellationToken))
            throw PayeeErrors.AlreadyRegistered(taxId.Formatted());

        var payee = Payee.Register(
            tenantId,
            request.LegalName,
            request.TaxId,
            policyKind,
            request.ExpectedAmount,
            request.TolerancePercent,
            request.MinAmount,
            request.MaxAmount,
            DateTime.UtcNow);

        await repository.AddAsync(payee, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new RegisterPayeeResponse(payee.Id.Value);
    }
}

public sealed class RegisterPayeeIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<RegisterPayeeIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<RegisterPayeeCommand, RegisterPayeeResponse>(mediator, requestManager, logger)
{
    protected override RegisterPayeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
