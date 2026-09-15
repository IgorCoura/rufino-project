namespace BillPayment.Application.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.Ports;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <param name="Read">
/// O documento foi aberto e relido. Falso significa "não deu para perguntar" — nunca "o
/// documento não diz".
/// </param>
/// <param name="Payer">
/// O pagador que o documento identifica, apurado pelo <c>BillRoutingService</c> contra o cadastro
/// fiscal do tenant <strong>de hoje</strong>. Nulo com <see cref="Read"/> verdadeiro é afirmação:
/// o papel não identifica ninguém.
/// </param>
/// <param name="ReasonCode">Por que não foi relido. Nulo quando foi.</param>
public sealed record BillDocumentOutcome(bool Read, PartyInfo? Payer, string? ReasonCode);

/// <summary>
/// Relê o documento original de um boleto pela cascata <strong>determinística</strong>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o irmão barato do <see cref="IBillReadingSource"/>.</strong> Aquele chama o extrator
/// de IA, custa dinheiro e por isso o retrato dele fica guardado; este abre o PDF, lê o texto e
/// varre os documentos fiscais — medido em 33 ms sobre um boleto de uma página —, e por isso pode
/// rodar a cada validação, como a consulta oficial roda.
/// </para>
/// <para>
/// <strong>Existe para a verificação 8 parar de depender da captura.</strong> Enquanto o pagador
/// vinha congelado no agregado, revalidar não corrigia leitura nenhuma e cada caminho novo de
/// entrada precisava lembrar de preencher o campo — três lugares criam <c>Bill</c> e o esquecimento
/// de um deles foi exatamente o defeito do RUF101 (2026-09-15). Aqui a leitura é função do arquivo
/// guardado e do cadastro de hoje, e nenhuma fonte precisa saber disso.
/// </para>
/// <para>
/// <strong>Nada do conteúdo lido é logado</strong> — o texto do documento carrega a linha
/// digitável e o documento fiscal do pagador. O único registro é a falha de leitura, sem o que
/// estava dentro do arquivo.
/// </para>
/// </remarks>
public interface IBillDocumentSource
{
    Task<BillDocumentOutcome> ReadAsync(Bill bill, PayerProfile? profile, TenantId tenantId, CancellationToken cancellationToken);
}

internal sealed class BillDocumentSource(
    IAttachmentStorage storage,
    IBoletoDocumentParser parser,
    TimeProvider clock,
    ILogger<BillDocumentSource> logger) : IBillDocumentSource
{
    /// <summary>
    /// <strong>Nunca deixa a validação cair por causa do documento.</strong> O balde fora do ar,
    /// a chave órfã e o PDF corrompido viram "não foi relido", e a verificação 8 segue com o que o
    /// agregado guardou — derrubar a apuração inteira das catorze porque o arquivo não abriu
    /// trocaria um retrato velho por nenhuma resposta.
    /// </summary>
    public async Task<BillDocumentOutcome> ReadAsync(
        Bill bill,
        PayerProfile? profile,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bill);

        try
        {
            return await ReadCoreAsync(bill, profile, tenantId, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Não foi possível reler o documento do boleto na validação.");
            return NotRead("document_unavailable");
        }
    }

    private async Task<BillDocumentOutcome> ReadCoreAsync(
        Bill bill,
        PayerProfile? profile,
        TenantId tenantId,
        CancellationToken cancellationToken)
    {
        // Boleto importado só com os dígitos não tem documento para reler — ausência, não falha.
        if (string.IsNullOrEmpty(bill.Origin.StorageKey))
            return NotRead("no_stored_document");

        using var artifact = await storage.OpenAsync(tenantId, bill.Origin.StorageKey, cancellationToken);
        if (artifact is null)
            return NotRead("document_unavailable");

        using var buffer = new MemoryStream();
        await artifact.Content.CopyToAsync(buffer, cancellationToken);
        var content = new ReadOnlyMemory<byte>(buffer.ToArray());

        if (content.IsEmpty)
            return NotRead("document_unavailable");

        if (!DocumentPayload.IsSupported(artifact.ContentType))
            return NotRead("unsupported_media_type");

        // As candidatas de senha e os documentos do cadastro saem do perfil de AGORA: é o que faz
        // o boleto lido antes do cadastro fiscal existir passar a ser lido depois dele.
        IReadOnlyList<TaxId> knownTaxIds = profile is null
            ? []
            : [profile.PrimaryTaxId, .. profile.AdditionalTaxIds];

        var extraction = await parser.ParseAsync(
            content,
            artifact.ContentType,
            PasswordDerivationService.Derive(profile),
            knownTaxIds,
            DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime),
            cancellationToken);

        // PDF que nenhuma senha derivada abre não foi lido — dizer que ele não traz pagador seria
        // afirmar sobre um arquivo que ninguém conseguiu ver.
        if (extraction.IsLocked)
            return NotRead("pdf_locked");

        // Documento sem instrumento nenhum ainda pode trazer o bloco do pagador, mas a cascata
        // devolve NotFound sem as partes — não há o que afirmar a partir dele.
        if (!extraction.Resolved)
            return NotRead(extraction.ReasonCode ?? "no_instrument_in_document");

        var payer = BillRoutingService.IdentifyPayer(extraction, profile);
        if (payer is null)
            return new BillDocumentOutcome(Read: true, Payer: null, ReasonCode: null);

        // O nome só acompanha quando a leitura por IA nomeou o MESMO documento fiscal — nome de um
        // e documento de outro descreveria uma pessoa que não existe. É a mesma guarda da promoção.
        var name = bill.Reading?.PayerTaxId is { } read && read.Equals(payer)
            ? bill.Reading.PayerName
            : null;

        return new BillDocumentOutcome(Read: true, PartyInfo.Of(name, payer), ReasonCode: null);
    }

    private static BillDocumentOutcome NotRead(string reasonCode)
        => new(Read: false, Payer: null, reasonCode);
}
