namespace BillPayment.Application.Bills.Commands;

using System.Security.Cryptography;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Importa um documento de cobrança a partir do texto cru dos instrumentos, do arquivo do
/// boleto, ou dos dois.
/// </summary>
/// <remarks>
/// <para>
/// Pelo menos um entre <see cref="DigitableLine"/>, <see cref="PixPayload"/> e
/// <see cref="Document"/> tem que vir preenchido. Linha e QR juntos são o caso comum de boleto
/// híbrido; linha e arquivo juntos são o caso comum de quem cola os dígitos e anexa o papel.
/// </para>
/// <para>
/// <strong>O arquivo é fonte de instrumento <em>e</em> evidência.</strong> A cascata
/// determinística tenta tirar dele o código de barras e o BR Code; achando ou não, o documento
/// fica guardado — é o papel contra o qual o aprovador confere as verificações, e é ele que
/// coloca o boleto na fila da leitura por IA.
/// </para>
/// </remarks>
/// <param name="Document">
/// Os bytes do arquivo. Já lidos — o teto de tamanho é conferido na borda.
/// </param>
public sealed record ImportBillCommand(
    Guid TenantId,
    string? DigitableLine,
    string? PixPayload,
    string SourceKind,
    DateTime ReceivedAt,
    Guid? SourceId,
    string? SenderAddress,
    string? ExternalMessageId,
    ReadOnlyMemory<byte> Document = default,
    string? DocumentContentType = null,
    string? DocumentFileName = null) : ITenantScopedCommand, IRequest<ImportBillResponse>, ISensitiveCommand;

public sealed record ImportBillResponse(Guid Id, string Kind, string Rail);

public sealed class ImportBillCommandHandler(
    IBillRepository repository,
    IPayerProfileRepository payerProfiles,
    ICaptureSourceRepository sources,
    IBoletoDocumentParser parser,
    IAttachmentStorage storage,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ImportBillCommand, ImportBillResponse>
{
    public async Task<ImportBillResponse> Handle(ImportBillCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var now = clock.GetUtcNow().UtcDateTime;

        // Tradução de input: string para Smart Enum é responsabilidade do handler.
        var sourceKind = Enumeration.FromDisplayName<BillSourceKind>(request.SourceKind);

        // Fonte de captura é referência a outro agregado, e vem do corpo: existe, e é deste
        // tenant. Sem isto a proveniência do boleto era forjável — "veio da caixa X" apontando
        // para uma fonte de outra conta, ou para nenhuma.
        if (request.SourceId is { } sourceId
            && !await sources.ExistsAsync(tenantId, CaptureSourceId.From(sourceId), cancellationToken))
        {
            throw CaptureSourceErrors.NotFound(sourceId);
        }

        var instruments = ReadInstruments(request, now);

        var document = await ReadDocumentAsync(request, tenantId, now, cancellationToken);
        if (document is not null)
        {
            Merge(instruments, document.Extraction.Instruments);

            // Sem instrumento nenhum não há boleto a criar — o agregado exige um por invariante.
            // A recusa diz o que aconteceu com o ARQUIVO, e não "informe a linha digitável": quem
            // anexou um papel precisa saber que ele não foi lido, e por quê.
            if (instruments.Count == 0)
                throw BillErrors.UnreadableInstrument(DescribeFailure(document.Extraction));
        }

        // Só grava depois de saber que há boleto: guardar o que não deu em nada transformaria o
        // balde num depósito de documento pessoal, que é a mesma regra da captura automática.
        var storageKey = document is null
            ? null
            : await storage.StoreAsync(
                tenantId, document.FileName, document.ContentType, request.Document, cancellationToken);

        try
        {
            var origin = BillOrigin.Create(
                sourceKind,
                request.ReceivedAt,
                request.SourceId,
                request.SenderAddress,
                request.ExternalMessageId,
                document?.ContentHash,
                storageKey);

            var bill = Bill.Capture(tenantId, instruments, origin, now);

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
        catch when (document is not null)
        {
            // O balde está FORA da transação do EF, então falhar fechado aqui é apagar à mão o
            // que já foi gravado. Sem isto, reenviar o mesmo boleto — que recusa com BLP.BIL02 e
            // é o engano mais comum — deixaria um arquivo órfão a cada tentativa. `RemoveAsync` é
            // idempotente, e o `CancellationToken.None` é de propósito: desistir da limpeza
            // porque o request foi cancelado é justamente o que produz o órfão.
            await storage.RemoveAsync(tenantId, storageKey!, CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// Converte o texto cru em instrumento. É decisão sobre <em>forma do input</em>, que é do
    /// handler — a composição do agregado continua sendo da factory. Erro de leitura vira
    /// BLP.BIL01 para o usuário receber "não consegui ler o documento" em vez do detalhe
    /// técnico do dígito verificador.
    /// </summary>
    private static List<PaymentInstrument> ReadInstruments(ImportBillCommand request, DateTime now)
    {
        var instruments = new List<PaymentInstrument>();

        if (!string.IsNullOrWhiteSpace(request.DigitableLine))
        {
            try
            {
                instruments.Add(PaymentInstrument.FromBarcode(
                    DigitableLine.Parse(request.DigitableLine, now)));
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

    /// <summary>
    /// Roda a cascata determinística sobre o arquivo anexado e calcula o hash do conteúdo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Só os degraus baratos.</strong> A extração por IA não é chamada aqui: ela é
    /// orquestrada pela faixa de visão da captura, atrás do teto de custo por tenant. O boleto
    /// nasce com <c>StorageKey</c>, então quem faz a leitura por IA é a fila — sem prender a
    /// requisição de quem está importando pela latência do provedor.
    /// </para>
    /// <para>
    /// O tipo de mídia é recusado <strong>antes</strong> de qualquer gravação, pela mesma razão
    /// do anexo manual da quarentena: arquivo que a leitura nunca abrirá não deve ocupar balde.
    /// </para>
    /// </remarks>
    private async Task<ImportedDocument?> ReadDocumentAsync(
        ImportBillCommand request,
        TenantId tenantId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (request.Document.IsEmpty)
            return null;

        if (!DocumentPayload.IsSupported(request.DocumentContentType))
            throw BillErrors.UnsupportedDocument(request.DocumentContentType ?? "desconhecido");

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);

        var extraction = await parser.ParseAsync(
            request.Document,
            request.DocumentContentType,
            PasswordDerivationService.Derive(profile),
            KnownTaxIdsOf(profile),
            DateOnly.FromDateTime(now),
            cancellationToken);

        return new ImportedDocument(
            string.IsNullOrWhiteSpace(request.DocumentFileName) ? "boleto" : request.DocumentFileName,
            request.DocumentContentType,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(request.Document.Span)),
            extraction);
    }

    /// <summary>
    /// Une o que saiu do arquivo ao que foi digitado, sem repetir instrumento.
    /// </summary>
    /// <remarks>
    /// Colar a linha digitável <em>e</em> anexar o PDF que a contém é o caso normal, e os dois
    /// caminhos produzem a mesma <c>NaturalKey</c> — que <c>Bill.Capture</c> recusa repetida.
    /// O digitado vence por já estar na lista; o do arquivo é descartado por ser o mesmo dado.
    /// </remarks>
    private static void Merge(List<PaymentInstrument> instruments, IReadOnlyList<PaymentInstrument> extracted)
    {
        // O conjunto acompanha o que já entrou, e não uma fotografia do início: um arquivo que
        // traga o mesmo instrumento duas vezes também precisa ser reduzido a um.
        var seen = instruments.Select(i => i.NaturalKey).ToHashSet(StringComparer.Ordinal);

        instruments.AddRange(extracted.Where(instrument => seen.Add(instrument.NaturalKey)));
    }

    private static string DescribeFailure(ExtractionResult extraction)
        => extraction.IsLocked
            ? "o arquivo está protegido por senha, e nenhuma senha derivada do cadastro o abriu"
            : "não encontrei código de barras nem QR Pix no arquivo enviado";

    private static IReadOnlyList<TaxId> KnownTaxIdsOf(PayerProfile? profile)
        => profile is null
            ? []
            : [profile.PrimaryTaxId, .. profile.AdditionalTaxIds];

    /// <summary>O arquivo anexado, já lido pela cascata e com o hash calculado.</summary>
    private sealed record ImportedDocument(
        string FileName,
        string? ContentType,
        string ContentHash,
        ExtractionResult Extraction);
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
