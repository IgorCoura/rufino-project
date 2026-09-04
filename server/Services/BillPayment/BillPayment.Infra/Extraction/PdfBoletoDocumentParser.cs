namespace BillPayment.Infra.Extraction;

using System.Text;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

/// <summary>
/// A cascata de extração sobre PDF — degraus 0, 2 e 2b do doc 09.
/// </summary>
/// <remarks>
/// <para>
/// Cobre a <strong>derivação de senha</strong>, o <strong>texto embutido</strong> e a
/// <strong>leitura de QR</strong>. O extrator de visão entra na 2.4, e por isso este parser
/// devolve <c>ExtractionResult.NotFound</c> em vez de decidir sozinho que não há boleto — quem
/// decide o destino é o <c>CaptureTriageService</c>.
/// </para>
/// <para>
/// <strong>Nada aqui loga o texto do documento nem a senha.</strong> O texto carrega a linha
/// digitável, que é instrumento de pagamento; a senha é segredo derivado do cadastro do tenant
/// (ADR-009). O log só registra contagens e o rótulo do campo que abriu.
/// </para>
/// </remarks>
internal sealed class PdfBoletoDocumentParser(
    IOptions<ExtractionOptions> options,
    ILogger<PdfBoletoDocumentParser> logger) : IBoletoDocumentParser
{
    /// <summary>Assinatura de um PDF. Validar os bytes, não a promessa do provedor.</summary>
    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    private readonly ExtractionOptions _options = options.Value;

    public Task<ExtractionResult> ParseAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        IReadOnlyList<TaxId> knownTaxIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passwordCandidates);

        if (!LooksLikePdf(content.Span))
            return Task.FromResult(ExtractionResult.NotFound("not_a_pdf"));

        var scan = ScanDocument(content, passwordCandidates, knownTaxIds, today, cancellationToken);

        if (scan.Locked)
            return Task.FromResult(ExtractionResult.Locked());

        if (scan.Instruments.Count == 0)
        {
            // "Sem camada de texto" e "tem texto e não há boleto nele" são coisas diferentes: a
            // primeira é o caso que o extrator de visão existe para resolver, e a métrica da
            // cascata precisa distinguir as duas para saber se ele está sendo necessário.
            var reason = scan.HadText ? "no_instrument_in_document" : "no_text_layer";
            return Task.FromResult(ExtractionResult.NotFound(reason));
        }

        return Task.FromResult(
            ExtractionResult.Found(scan.Instruments, scan.Method!, scan.UnlockedBy, scan.Parties));
    }

    /// <summary>
    /// Reescreve o PDF cifrado como um PDF que abre sem senha, página por página.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>A senha vazia decide se há trabalho a fazer</strong> — é o mesmo critério que faz
    /// o <c>ScanDocument</c> devolver <c>UnlockedBy</c> nulo. Abrindo com ela, o documento já é
    /// legível por qualquer leitor e reescrevê-lo só arriscaria degradá-lo à toa.
    /// </para>
    /// <para>
    /// <strong>Falhar aqui devolve <c>null</c>, nunca lança.</strong> O chamador então decide o
    /// que fazer com um documento que não abre — e a decisão dele é não gastar a chamada, que é a
    /// mesma regra de sempre para PDF cifrado. Um erro subindo daqui derrubaria o processamento
    /// de um artefato que a cascata determinística já tinha resolvido.
    /// </para>
    /// </remarks>
    public Task<ReadOnlyMemory<byte>?> UnlockAsync(
        ReadOnlyMemory<byte> content,
        string? contentType,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passwordCandidates);

        if (!LooksLikePdf(content.Span))
            return Task.FromResult<ReadOnlyMemory<byte>?>(null);

        var bytes = content.ToArray();

        if (OpensWithoutPassword(bytes))
            return Task.FromResult<ReadOnlyMemory<byte>?>(null);

        foreach (var candidate in passwordCandidates.Take(_options.MaxPasswordCandidates))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var document = PdfDocument.Open(bytes, new ParsingOptions
                {
                    Password = candidate.Value,
                    UseLenientParsing = true,
                });

                var builder = new PdfDocumentBuilder();
                for (var page = 1; page <= document.NumberOfPages; page++)
                    builder.AddPage(document, page);

                var clear = builder.Build();

                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation(
                        "Cópia sem senha produzida a partir de PDF aberto por senha derivada de {Field}.",
                        candidate.DerivedFrom);
                }

                return Task.FromResult<ReadOnlyMemory<byte>?>(clear);
            }
            catch (Exception ex) when (IsWrongPassword(ex))
            {
                // Candidata errada é o caso comum: segue para a próxima.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Não foi possível reescrever o PDF cifrado sem senha.");
                return Task.FromResult<ReadOnlyMemory<byte>?>(null);
            }
        }

        return Task.FromResult<ReadOnlyMemory<byte>?>(null);
    }

    private static bool OpensWithoutPassword(byte[] bytes)
    {
        try
        {
            using var document = PdfDocument.Open(bytes, new ParsingOptions { UseLenientParsing = true });
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    private sealed record ScanOutcome(
        IReadOnlyList<PaymentInstrument> Instruments,
        IReadOnlyList<PartyCandidate> Parties,
        ExtractionMethod? Method,
        string? UnlockedBy,
        bool Locked,
        bool HadText);

    /// <summary>
    /// Abre o documento e roda os dois degraus baratos numa passagem só.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Texto e QR rodam os dois, sempre</strong> — não em cascata excludente. Num boleto
    /// híbrido a linha digitável vem do texto e o BR Code vem da imagem, e é a presença
    /// <em>simultânea</em> dos dois trilhos que sustenta o check <c>PixBarcodeConsistency</c>,
    /// a defesa contra QR adulterado colado sobre boleto verdadeiro. Parar no primeiro que
    /// resolve desligaria essa defesa em todo documento híbrido.
    /// </para>
    /// <para>
    /// O <c>seen</c> compartilhado deduplica o mesmo instrumento achado pelos dois caminhos.
    /// </para>
    /// </remarks>
    private ScanOutcome ScanDocument(
        ReadOnlyMemory<byte> content,
        IReadOnlyList<PasswordCandidate> passwordCandidates,
        IReadOnlyList<TaxId> knownTaxIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        // Senha vazia primeiro: cobre o PDF que só tem owner password (bloqueia edição, não
        // leitura), que é o caso mais comum e o mais barato.
        var candidates = new List<PasswordCandidate> { PasswordCandidate.Empty };
        candidates.AddRange(passwordCandidates.Take(_options.MaxPasswordCandidates));

        var bytes = content.ToArray();

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var document = PdfDocument.Open(bytes, new ParsingOptions
                {
                    Password = candidate.Value,
                    UseLenientParsing = true,
                });

                // Só a senha vazia não conta como "destravado por": não houve derivação nenhuma.
                var unlockedBy = ReferenceEquals(candidate, PasswordCandidate.Empty) ? null : candidate.DerivedFrom;

                if (unlockedBy is not null && logger.IsEnabled(LogLevel.Information))
                    logger.LogInformation("PDF aberto por senha derivada de {Field}.", unlockedBy);

                return Harvest(document, unlockedBy, knownTaxIds, today, cancellationToken);
            }
            catch (Exception ex) when (IsWrongPassword(ex))
            {
                // Candidata errada é o caso comum: segue para a próxima sem registrar nada.
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // PDF corrompido ou fora do padrão não é documento cifrado — não adianta tentar
                // outra senha, e insistir só gastaria o teto à toa.
                logger.LogWarning(ex, "Não foi possível abrir o PDF para extração.");
                return new ScanOutcome([], [], null, null, Locked: false, HadText: false);
            }
        }

        return new ScanOutcome([], [], null, null, Locked: true, HadText: false);
    }

    private ScanOutcome Harvest(
        PdfDocument document,
        string? unlockedBy,
        IReadOnlyList<TaxId> knownTaxIds,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var reference = today.ToDateTime(TimeOnly.MinValue);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var instruments = new List<PaymentInstrument>();
        var text = new StringBuilder();
        var qrCount = 0;
        var pages = 0;

        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (++pages > _options.MaxPages)
                break;

            // Quebra de linha entre páginas: sem ela, o fim de uma página emendaria no começo da
            // outra e produziria dígitos que não existem no documento.
            text.Append(page.Text).Append('\n');

            var fromImages = QrCodeScanner.Scan(page.GetImages(), seen, reference, logger, cancellationToken);
            qrCount += fromImages.Count;
            instruments.AddRange(fromImages);
        }

        var body = text.ToString();
        var fromText = CandidateScanner.Scan(body, reference, seen);
        instruments.AddRange(fromText);

        // O degrau relatado é o mais barato que contribuiu: a métrica que interessa é "o passo
        // gratuito bastou?", e não qual deles achou mais.
        ExtractionMethod? method = null;
        if (fromText.Count > 0)
            method = ExtractionMethod.EmbeddedText;
        else if (qrCount > 0)
            method = ExtractionMethod.QrCode;

        return new ScanOutcome(
            instruments,
            // Os documentos fiscais saem da MESMA passagem de texto que já foi feita: reabrir o
            // PDF só para procurá-los dobraria o custo do degrau mais barato da cascata.
            TaxIdScanner.Scan(body, knownTaxIds),
            method,
            unlockedBy,
            Locked: false,
            HadText: !string.IsNullOrWhiteSpace(body));
    }

    /// <summary>
    /// O PdfPig sinaliza senha errada por exceção, e o tipo mudou entre versões — casar pela
    /// mensagem é frágil, mas a alternativa (tratar tudo como corrompido) desistiria de abrir
    /// documento que uma candidata seguinte abriria.
    /// </summary>
    private static bool IsWrongPassword(Exception ex)
        => ex.GetType().Name.Contains("PdfDocumentEncrypted", StringComparison.Ordinal)
            || ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikePdf(ReadOnlySpan<byte> content)
        => content.Length >= PdfMagic.Length && content[..PdfMagic.Length].SequenceEqual(PdfMagic);
}
