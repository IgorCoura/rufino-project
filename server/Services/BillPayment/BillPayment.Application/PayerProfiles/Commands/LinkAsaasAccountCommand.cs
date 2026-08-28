namespace BillPayment.Application.PayerProfiles.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Vincula a subconta do provedor ao tenant. <c>AccountRef</c> é um <em>ponteiro</em> para a
/// chave no cofre, nunca a chave — ela não trafega por command, log ou resposta de API.
/// </summary>
public sealed record LinkAsaasAccountCommand(
    Guid TenantId,
    string? AccountRef) : ITenantScopedCommand, IRequest<LinkAsaasAccountResponse>, ISensitiveCommand;

public sealed record LinkAsaasAccountResponse(Guid Id, bool CanSchedulePayments);

public sealed class LinkAsaasAccountCommandHandler(
    IPayerProfileRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LinkAsaasAccountCommand, LinkAsaasAccountResponse>
{
    public async Task<LinkAsaasAccountResponse> Handle(
        LinkAsaasAccountCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var profile = await repository.GetByTenantAsync(tenantId, cancellationToken)
            ?? throw PayerProfileErrors.NotFound(request.TenantId);

        profile.LinkAsaasAccount(request.AccountRef ?? string.Empty, DateTime.UtcNow);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new LinkAsaasAccountResponse(profile.Id.Value, profile.CanSchedulePayments);
    }
}

public sealed class LinkAsaasAccountIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<LinkAsaasAccountIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<LinkAsaasAccountCommand, LinkAsaasAccountResponse>(mediator, requestManager, logger)
{
    protected override LinkAsaasAccountResponse CreateResultForDuplicateRequest() => new(Guid.Empty, false);
}
