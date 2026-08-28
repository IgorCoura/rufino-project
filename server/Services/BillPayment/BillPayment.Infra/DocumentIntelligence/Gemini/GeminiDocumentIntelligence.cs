namespace BillPayment.Infra.DocumentIntelligence.Gemini;

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BillPayment.Domain.Extraction;
using BillPayment.Infra.Extraction;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Extrator de documentos sobre a API de geração do Gemini, por HTTP direto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Tudo o que é do provedor mora aqui</strong> — endpoint, autenticação, montagem do
/// request, structured output, contagem de tokens e <em>o prompt</em>. O prompt é detalhe de
/// implementação: provedores diferentes pedem prompts diferentes, e promovê-lo a configuração
/// vazaria o acoplamento que o ADR-013 existe para evitar.
/// </para>
/// <para>
/// <strong>Não lança por indisponibilidade.</strong> Timeout, <c>429</c>, <c>5xx</c> ou resposta
/// ilegível devolvem <see cref="ExtractedDocument.Empty"/>, e quem chama trata como "não
/// resolvi". Falha de provedor não pode travar a ingestão nem, pior, ser confundida com "este
/// documento não é boleto".
/// </para>
/// <para>
/// <strong>Sem retentativa automática.</strong> É a diferença em relação aos clientes do Asaas e
/// do Graph: aqui cada tentativa consome cota de uma conta com teto diário, e insistir num PDF
/// que o modelo recusou gastaria o dia em um documento só. O artefato volta amanhã pela fila de
/// quarentena, que é retentativa mais barata e visível.
/// </para>
/// </remarks>
internal sealed class GeminiDocumentIntelligence(
    IHttpClientFactory httpClientFactory,
    ExtractionBudget budget,
    IOptions<DocumentIntelligenceOptions> options,
    ILogger<GeminiDocumentIntelligence> logger) : IDocumentIntelligence
{
    internal const string CLIENT_NAME = "document-intelligence";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly DocumentIntelligenceOptions _options = options.Value;

    public bool IsEnabled => _options.IsConfigured;

    public async Task<ExtractionAttempt> ExtractAsync(
        DocumentPayload payload,
        ExtractionHints hints,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        ArgumentNullException.ThrowIfNull(hints);

        var reserved = await budget.TryReserveAsync(
            payload.TenantId, _options.MaxCallsPerTenantPerDay, _options.MinIntervalMs, cancellationToken);

        if (!reserved)
            return ExtractionAttempt.BudgetExhausted();

        // Só as primeiras páginas: boleto está na primeira ou na segunda, e mandar um relatório
        // de trinta páginas custa proporcional sem aumentar a chance de achar o código de barras.
        // Sem isto, chamadas batiam no timeout de 60s e a vazão caía de ~70 para ~8 por minuto.
        var content = string.Equals(payload.MediaType, DocumentPayload.PDF, StringComparison.Ordinal)
            ? PdfPageTrimmer.TakeFirstPages(payload.Content, _options.MaxPages, logger)
            : payload.Content;

        // O corpo do e-mail entra como parte de texto, já convertido de HTML aqui no adapter —
        // é dele que saem competência e descrição quando o documento não as traz.
        var parts = new List<GeminiPart>
        {
            GeminiPart.FromDocument(payload.MediaType, content),
        };

        if (payload.SupplementalText is { } supplemental)
        {
            var text = payload.SupplementalTextIsHtml ? HtmlText.ToPlainText(supplemental) : supplemental;
            if (!string.IsNullOrWhiteSpace(text))
                parts.Add(GeminiPart.FromText("CORPO DO E-MAIL QUE TROUXE O DOCUMENTO:\n" + text));
        }

        parts.Add(GeminiPart.FromText(GeminiPrompt.Build(hints)));

        var request = new GeminiRequest(
            [new GeminiContent(parts)],
            new GeminiGenerationConfig("application/json", GeminiPrompt.ResponseSchema, Temperature: 0));

        return await SendAsync(request, payload.MediaType, content.Length, cancellationToken);
    }

    /// <param name="mediaType">Tipo do artefato enviado — entra no diagnóstico do não-200.</param>
    /// <param name="contentBytes">
    /// Tamanho do artefato JÁ APARADO, em bytes. É o outro lado do diagnóstico: o provedor recusa
    /// requisição acima do teto de dados embutidos, e o corpo em base64 cresce um terço sobre isto.
    /// </param>
    private async Task<ExtractionAttempt> SendAsync(
        GeminiRequest request,
        string mediaType,
        int contentBytes,
        CancellationToken cancellationToken)
    {
        var http = httpClientFactory.CreateClient(CLIENT_NAME);
        var url = $"{Base}models/{_options.Model}:generateContent";

        try
        {
            using var response = await http.PostAsJsonAsync(url, request, Json, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // O corpo do erro NÃO entra no log: ele ecoa parte da requisição, e a requisição
                // carrega o documento do cliente. O que entra é a FORMA do artefato — tipo e
                // tamanho —, que é o suficiente para separar "recusa do provedor" de "artefato
                // grande demais" sem revelar nada do documento. Sem isto o 400 é indistinguível
                // do 503 no diagnóstico, e foi assim que 48 recusas ficaram sem causa conhecida
                // na medição de 2026-08-27.
                logger.LogWarning(
                    "O extrator de documentos respondeu {Status} para um artefato {MediaType} de "
                    + "{Bytes} bytes. O artefato vai para a quarentena.",
                    (int)response.StatusCode,
                    mediaType,
                    contentBytes);

                // 400 é fato sobre o ARTEFATO — repetir produz a mesma recusa. 5xx e 429 são
                // fato sobre o provedor, e nada foi aprendido sobre o documento: aí retentar é a
                // resposta certa, e foi a ausência desta separação que mandou 24 documentos bons
                // para a quarentena por 503 em 2026-08-27.
                return IsTransient(response.StatusCode)
                    ? ExtractionAttempt.Unavailable($"provider_{(int)response.StatusCode}")
                    : ExtractionAttempt.Rejected($"provider_{(int)response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiResponse>(Json, cancellationToken);
            var text = FirstText(payload);

            if (payload?.UsageMetadata is { } usage && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Extração por IA concluída: {InputTokens} tokens de entrada, {OutputTokens} de saída.",
                    usage.PromptTokenCount,
                    usage.CandidatesTokenCount);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                // O provedor RESPONDEU — só não veio texto. É desfecho sobre o documento, não
                // sobre a rede, e insistir gastaria cota para receber o mesmo vazio.
                logger.LogWarning("O extrator devolveu resposta vazia.");
                return ExtractionAttempt.Answered(ExtractedDocument.Empty);
            }

            var extraction = JsonSerializer.Deserialize<GeminiExtraction>(text, Json);

            return extraction is null
                ? ExtractionAttempt.Answered(ExtractedDocument.Empty)
                : ExtractionAttempt.Answered(Map(extraction));
        }
        catch (JsonException ex)
        {
            // Corpo fora do schema é resposta do provedor, não falha de transporte: o modelo
            // devolveu algo, e devolveria o mesmo na próxima. Não é retentável.
            logger.LogWarning(ex, "O extrator devolveu um corpo fora do formato esperado.");
            return ExtractionAttempt.Answered(ExtractedDocument.Empty);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(
                ex,
                "Não foi possível falar com o extrator de documentos: artefato {MediaType} de "
                + "{Bytes} bytes. A cota da chamada já foi reservada e se perde com ela.",
                mediaType,
                contentBytes);

            // Timeout e transporte caído são a definição de "nada foi aprendido": retentável.
            return ExtractionAttempt.Unavailable(
                ex is TaskCanceledException ? "provider_timeout" : "transport_failure");
        }
    }

    /// <summary>
    /// Falha do PROVEDOR, e não do artefato: 5xx e 429 melhoram com o tempo, 400 não.
    /// </summary>
    private static bool IsTransient(HttpStatusCode status)
        => status == HttpStatusCode.TooManyRequests || (int)status >= 500;

    /// <summary>
    /// O texto da primeira parte da primeira alternativa — que é onde o JSON imposto pelo
    /// <c>responseSchema</c> chega.
    /// </summary>
    private static string? FirstText(GeminiResponse? payload)
    {
        if (payload?.Candidates is not { Count: > 0 } candidates)
            return null;

        return candidates[0].Content?.Parts is { Count: > 0 } parts ? parts[0].Text : null;
    }

    /// <summary>
    /// Traduz a resposta do provedor no saco de candidatos do domínio.
    /// </summary>
    /// <remarks>
    /// <strong>Frouxo por necessidade.</strong> Valor e vencimento chegam como texto porque o
    /// modelo devolve o que está impresso — <c>"R$ 1.234,56"</c>, <c>"12/08/2026"</c>, ou vazio.
    /// Falhar a extração inteira porque uma data veio num formato inesperado descartaria as
    /// linhas digitáveis junto, que são o que interessa. É a mesma lição dos DTOs do Asaas.
    /// </remarks>
    private static ExtractedDocument Map(GeminiExtraction body)
        => ExtractedDocument.From(
            body.DigitableLines,
            body.PixPayloads,
            ParseKind(body.DocumentKind),
            body.PayerName,
            body.PayerTaxId,
            body.PayeeName,
            body.PayeeTaxId,
            body.AccountReference,
            ParseAmount(body.Amount),
            ParseDate(body.DueDate),
            body.Notes,
            body.BillingPeriod,
            body.Description);

    private static DocumentKind? ParseKind(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return Enumeration.GetAll<DocumentKind>()
            .FirstOrDefault(k => string.Equals(k.Name, value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static decimal? ParseAmount(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = new string(value.Where(c => char.IsAsciiDigit(c) || c is ',' or '.').ToArray());
        if (digits.Length == 0)
            return null;

        // "1.234,56" é o formato impresso no Brasil; "1234.56" é o que um modelo às vezes devolve.
        var normalized = digits.Contains(',', StringComparison.Ordinal)
            ? digits.Replace(".", string.Empty, StringComparison.Ordinal).Replace(',', '.')
            : digits;

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }

    private static DateOnly? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string[] formats = ["yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy"];

        return DateOnly.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
    }

    private string Base => _options.BaseUrl.EndsWith('/') ? _options.BaseUrl : _options.BaseUrl + "/";
}
