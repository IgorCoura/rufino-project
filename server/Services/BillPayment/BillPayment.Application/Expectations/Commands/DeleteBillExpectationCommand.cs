namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Remoção física. Os ciclos vão junto — são coleção owned da raiz.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É segura porque a <c>Bill</c> não referencia a expectativa</strong>; o ponteiro é o
/// inverso (<c>ExpectationCycle.FulfilledByBillId</c>), então nenhum boleto pago fica apontando
/// para o vazio. O que se perde é o histórico de cumprimento daquela conta.
/// </para>
/// <para>
/// <strong>Excluir não é "nunca mais".</strong> Uma expectativa de origem <c>Learned</c> pode ser
/// reaprendida pelo <c>LearnBillExpectationsCommand</c> no próximo boleto aprovado daquele
/// beneficiário — é a auto-cura do ADR-014, não defeito. Quem quer parar de monitorar de vez usa
/// <c>AlterBillExpectationWatch</c> para desativar, que é o caminho que deixa a decisão registrada.
/// </para>
/// </remarks>
public sealed record DeleteBillExpectationCommand(
    Guid TenantId,
    Guid ExpectationId) : ITenantScopedCommand, IRequest<DeleteBillExpectationResponse>;

public sealed record DeleteBillExpectationResponse(Guid Id);

public sealed class DeleteBillExpectationCommandHandler(
    IBillExpectationRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteBillExpectationCommand, DeleteBillExpectationResponse>
{
    public async Task<DeleteBillExpectationResponse> Handle(
        DeleteBillExpectationCommand request,
        CancellationToken cancellationToken)
    {
        var expectation = await repository.GetAsync(
                TenantId.From(request.TenantId),
                BillExpectationId.From(request.ExpectationId),
                cancellationToken)
            ?? throw BillExpectationErrors.NotFound(request.ExpectationId);

        repository.Remove(expectation);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new DeleteBillExpectationResponse(request.ExpectationId);
    }
}

public sealed class DeleteBillExpectationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<DeleteBillExpectationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<DeleteBillExpectationCommand, DeleteBillExpectationResponse>(
        mediator, requestManager, logger)
{
    protected override DeleteBillExpectationResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
}
