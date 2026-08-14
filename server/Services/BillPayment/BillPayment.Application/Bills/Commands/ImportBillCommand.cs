namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Importa um documento de cobrança a partir do texto cru dos instrumentos.
/// </summary>
/// <remarks>
/// Pelo menos um entre <see cref="DigitableLine"/> e <see cref="PixPayload"/> tem que vir
/// preenchido. Os dois juntos são o caso comum de boleto híbrido.
/// </remarks>
public sealed record ImportBillCommand(
    Guid TenantId,
    string? DigitableLine,
    string? PixPayload,
    string SourceKind,
    DateTime ReceivedAt,
    Guid? SourceId,
    string? SenderAddress,
    string? ExternalMessageId,
    string? ContentHash,
    string? StorageKey) : IRequest<ImportBillResponse>, ISensitiveCommand;

public sealed record ImportBillResponse(Guid Id, string Kind, string Rail);

public sealed class ImportBillCommandHandler(
    IBillRepository repository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportBillCommand, ImportBillResponse>
{
    public async Task<ImportBillResponse> Handle(ImportBillCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        // Tradução de input: string para Smart Enum é responsabilidade do handler.
        var sourceKind = Enumeration.FromDisplayName<BillSourceKind>(request.SourceKind);

        var instruments = ReadInstruments(request);

        var origin = BillOrigin.Create(
            sourceKind,
            request.ReceivedAt,
            request.SourceId,
            request.SenderAddress,
            request.ExternalMessageId,
            request.ContentHash,
            request.StorageKey);

        var bill = Bill.Capture(tenantId, instruments, origin, DateTime.UtcNow);

        // Unicidade global da chave de instrumento — travessia autorizada pelo ADR-008.
        // A checagem aqui evita o round-trip no caso comum; quem resolve a corrida é o índice
        // único parcial, e o erro que sobe dele é o mesmo BLP.BIL02.
        if (bill.DedupKey is not null
            && await repository.ExistsActiveByDedupKeyAsync(bill.DedupKey, cancellationToken))
        {
            throw BillErrors.AlreadyCaptured();
        }

        await repository.AddAsync(bill, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ImportBillResponse(bill.Id.Value, bill.Kind.Name, bill.Rail.Name);
    }

    /// <summary>
    /// Converte o texto cru em instrumento. É decisão sobre <em>forma do input</em>, que é do
    /// handler — a composição do agregado continua sendo da factory. Erro de leitura vira
    /// BLP.BIL01 para o usuário receber "não consegui ler o documento" em vez do detalhe
    /// técnico do dígito verificador.
    /// </summary>
    private static List<PaymentInstrument> ReadInstruments(ImportBillCommand request)
    {
        var instruments = new List<PaymentInstrument>();

        if (!string.IsNullOrWhiteSpace(request.DigitableLine))
        {
            try
            {
                instruments.Add(PaymentInstrument.FromBarcode(
                    DigitableLine.Parse(request.DigitableLine, DateTime.UtcNow)));
            }
            catch (DomainException ex)
            {
                throw BillErrors.UnreadableInstrument(ex.Message);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PixPayload))
        {
            try
            {
                instruments.Add(PaymentInstrument.FromPixQr(Domain.Instruments.PixPayload.Parse(request.PixPayload)));
            }
            catch (DomainException ex)
            {
                throw BillErrors.UnreadableInstrument(ex.Message);
            }
        }

        return instruments;
    }
}

public sealed class ImportBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ImportBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ImportBillCommand, ImportBillResponse>(mediator, requestManager, logger)
{
    protected override ImportBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, string.Empty);
}
