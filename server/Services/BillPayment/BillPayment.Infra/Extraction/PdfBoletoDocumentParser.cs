namespace BillPayment.Infra.Extraction;

using System.Text;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Ports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;

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
        DateOnly today,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(passwordCandidates);

        if (!LooksLikePdf(content.Span))
            return Task.FromResult(ExtractionResult.NotFound("not_a_pdf"));

        var scan = ScanDocument(content, passwordCandidates, today, cancellationToken);

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

        return Task.FromResult(ExtractionResult.Found(scan.Instruments, scan.Method!, scan.UnlockedBy));
    }

    private sealed record ScanOutcome(
        IReadOnlyList<PaymentInstrument> Instruments,
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

                return Harvest(document, unlockedBy, today, cancellationToken);
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
                return new ScanOutcome([], null, null, Locked: false, HadText: false);
            }
        }

        return new ScanOutcome([], null, null, Locked: true, HadText: false);
    }

    private ScanOutcome Harvest(
        PdfDocument document,
        string? unlockedBy,
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
