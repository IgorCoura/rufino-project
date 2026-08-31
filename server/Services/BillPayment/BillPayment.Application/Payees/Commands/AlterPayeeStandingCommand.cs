namespace BillPayment.Application.Payees.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Muda a marca de confiança do beneficiário (Normal, Whitelisted, Blacklisted). O efeito na
/// verificação é só da blacklist, e vale a partir da PRÓXIMA validação de cada boleto —
/// boletos já verificados mantêm o retrato até serem revalidados.
/// </summary>
public sealed record AlterPayeeStandingCommand(
    Guid TenantId,
    Guid PayeeId,
    string Standing) : ITenantScopedCommand, IRequest<AlterPayeeStandingResponse>;

public sealed record AlterPayeeStandingResponse(Guid Id);

public sealed class AlterPayeeStandingCommandHandler(
    IPayeeRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AlterPayeeStandingCommand, AlterPayeeStandingResponse>
{
    public async Task<AlterPayeeStandingResponse> Handle(
        AlterPayeeStandingCommand request,
        CancellationToken cancellationToken)
    {
        // Tradução de input: valor desconhecido lança EnumerationNotFoundException → 400.
        var standing = Enumeration.FromDisplayName<PayeeStanding>(request.Standing);

        var payee = await repository.GetAsync(
                TenantId.From(request.TenantId),
                PayeeId.From(request.PayeeId),
                cancellationToken)
            ?? throw PayeeErrors.NotFound(request.PayeeId);

        payee.SetStanding(standing, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new AlterPayeeStandingResponse(payee.Id.Value);
    }
}

public sealed class AlterPayeeStandingIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<AlterPayeeStandingIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<AlterPayeeStandingCommand, AlterPayeeStandingResponse>(mediator, requestManager, logger)
{
    protected override AlterPayeeStandingResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
