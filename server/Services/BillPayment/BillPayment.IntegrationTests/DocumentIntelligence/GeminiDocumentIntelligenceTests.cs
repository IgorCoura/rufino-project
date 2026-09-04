namespace BillPayment.IntegrationTests.DocumentIntelligence;

using System.Net;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.DocumentIntelligence;
using BillPayment.Infra.DocumentIntelligence.Gemini;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

/// <summary>
/// Tradução entre a resposta do extrator de IA e os VOs do domínio. Sem banco e sem rede — ver a
/// nota em <see cref="StubHttpMessageHandler"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Esta classe não tinha teste nenhum até 2026-08-27</strong>, e é a única porta de
/// integração do BC que estava nessa situação — o Asaas e o Graph sempre tiveram os seus. A
/// ausência custou caro: a medição daquele dia encontrou 614 chamadas reais com 96 mortas em
/// timeout, 48 recusadas com 400 e 24 com 503, e <em>nenhuma</em> delas era distinguível de "o
/// modelo leu e não achou boleto" para quem chama.
/// </para>
/// <para>
/// <strong>O que está sob teste é a tradução, e ela roda de verdade</strong> — o que se substitui
/// é o transporte. É a mesma disciplina dos testes do Asaas.
/// </para>
/// </remarks>
public sealed class GeminiDocumentIntelligenceTests
{
    private static readonly TenantId Tenant = TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001"));
    private static readonly DateTimeOffset Now = new(2026, 8, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>Um PDF mínimo e válido — o adapter apara páginas antes de enviar.</summary>
    private static readonly byte[] PdfBytes = "%PDF-1.4\n%%EOF\n"u8.ToArray();

    private const string ValidBankSlipLine = "34191234546789012345767890123457314880000061507";

    // Resposta completa, no formato que o `responseSchema` obriga o modelo a devolver.
    private const string FullBody = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  {
                    "text": "{\"digitableLines\":[\"34191234546789012345767890123457314880000061507\"],\"pixPayloads\":[],\"payerName\":\"RUFINO EMPREITEIRA\",\"payerTaxId\":\"11222333000181\",\"payeeName\":\"EDP SAO PAULO\",\"payeeTaxId\":null,\"amount\":\"615.07\",\"dueDate\":\"2026-09-10\",\"accountReference\":\"0000748299879\",\"billingPeriod\":\"08/2026\",\"description\":\"Conta de energia\"}"
                  }
                ]
              }
            }
          ]
        }
        """;

    // O modelo respondeu e não achou nada — desfecho legítimo, e o mais comum.
    private const string EmptyBody = """
        {
          "candidates": [
            {
              "content": {
                "parts": [
                  { "text": "{\"digitableLines\":[],\"pixPayloads\":[]}" }
                ]
              }
            }
          ]
        }
        """;

    // O modelo leu o documento e devolveu o que estava impresso.
    [Fact]
    public async Task ExtractAsync_WhenTheModelAnswers_ShouldMapEveryFieldOfTheReading()
    {
        var (result, _) = await ExtractAsync(StubHttpMessageHandler.Ok(FullBody));

        Assert.Contains(ValidBankSlipLine, result.Document.DigitableLineCandidates);
        Assert.Equal("RUFINO EMPREITEIRA", result.Document.PayerName);
        Assert.Equal("EDP SAO PAULO", result.Document.PayeeName);
        Assert.Equal("Conta de energia", result.Document.Description);
        Assert.Equal("08/2026", result.Document.BillingPeriod);
        Assert.Equal("0000748299879", result.Document.AccountReference);
    }

    // Resposta bem formada e sem candidatos é ausência, não falha: o documento simplesmente não
    // era boleto. É o desfecho que DEVE mandar o artefato para a quarentena.
    [Fact]
    public async Task ExtractAsync_WhenTheModelFindsNothing_ShouldReturnAnEmptyDocument()
    {
        var (result, _) = await ExtractAsync(StubHttpMessageHandler.Ok(EmptyBody));

        Assert.Same(ExtractionStatus.Empty, result.Status);
        Assert.True(result.ProviderAnswered);
        Assert.False(result.IsRetryable);
    }

    // A chave NUNCA vai na URL. Query string entra em log de proxy e em telemetria de cliente
    // HTTP, e segredo em log é segredo vazado (ADR-009).
    [Fact]
    public async Task ExtractAsync_ShouldNeverPutTheApiKeyInTheUrl()
    {
        var (_, handler) = await ExtractAsync(StubHttpMessageHandler.Ok(EmptyBody));

        Assert.NotNull(handler.LastRequestUri);
        Assert.DoesNotContain("key", handler.LastRequestUri.Query, StringComparison.OrdinalIgnoreCase);
    }

    // Temperatura zero e schema obrigatório: é o que torna a extração reproduzível e o que
    // impede o modelo de devolver prosa no lugar do JSON que o domínio sabe ler.
    [Fact]
    public async Task ExtractAsync_ShouldPinTheResponseShapeAndTemperature()
    {
        var (_, handler) = await ExtractAsync(StubHttpMessageHandler.Ok(EmptyBody));

        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("responseSchema", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("application/json", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("\"temperature\":0", handler.LastRequestBody.Replace(" ", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    // O corpo do e-mail viaja junto como parte de texto — é dele que saem competência e descrição
    // quando o documento não as traz.
    [Fact]
    public async Task ExtractAsync_WithSupplementalText_ShouldSendItAlongsideTheDocument()
    {
        var handler = StubHttpMessageHandler.Ok(EmptyBody);

        await ExtractAsync(handler, supplemental: "Sua fatura de agosto chegou");

        Assert.Contains("CORPO DO E-MAIL", handler.LastRequestBody!, StringComparison.Ordinal);
        Assert.Contains("fatura de agosto", handler.LastRequestBody, StringComparison.Ordinal);
    }

    // Cota esgotada não chama o provedor — o teto existe para proteger a conta, e gastar a
    // chamada para descobrir que não podia gastá-la inverteria o propósito.
    [Fact]
    public async Task ExtractAsync_WhenTheBudgetIsExhausted_ShouldNotCallTheProvider()
    {
        var handler = StubHttpMessageHandler.Ok(FullBody);

        var (result, _) = await ExtractAsync(handler, maxCallsPerDay: 0);

        Assert.Null(handler.LastRequestUri);
        Assert.Same(ExtractionStatus.BudgetExhausted, result.Status);

        // Retentável, mas amanhã: quem espaça é a espera da própria fila.
        Assert.True(result.IsRetryable);
    }

    /// <summary>
    /// TESTE-ÂNCORA da separação: as três falhas medidas em produção em 2026-08-27.
    /// </summary>
    /// <remarks>
    /// <strong>5xx e 429 são fato sobre o PROVEDOR; 400 é fato sobre o ARTEFATO.</strong> Antes
    /// desta separação as três chegavam ao chamador como "não achei boleto", e o efeito medido
    /// foi documento bom indo para a quarentena por 503 — sem retentativa, porque a fila nunca
    /// recebia o sinal de que valia tentar de novo.
    /// </remarks>
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    public async Task ExtractAsync_WhenTheProviderFails_ShouldSayWhetherItIsWorthRetrying(
        HttpStatusCode status, bool retryable)
    {
        var (result, _) = await ExtractAsync(new StubHttpMessageHandler(status, "{}"));

        Assert.Equal(retryable, result.IsRetryable);
        Assert.False(result.ProviderAnswered);
        Assert.Empty(result.Document.DigitableLineCandidates);
        Assert.Equal($"provider_{(int)status}", result.ReasonCode);
    }

    // Falha do provedor NUNCA pode ser confundida com "o modelo leu e não achou nada": a primeira
    // pede retentativa, a segunda manda o artefato para a quarentena. É a distinção inteira.
    [Fact]
    public async Task ExtractAsync_ProviderFailure_ShouldNotLookLikeAnEmptyReading()
    {
        var (unavailable, _) = await ExtractAsync(new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}"));
        var (empty, _) = await ExtractAsync(StubHttpMessageHandler.Ok(EmptyBody));

        Assert.NotEqual(empty.Status, unavailable.Status);
        Assert.True(unavailable.IsRetryable);
        Assert.False(empty.IsRetryable);
    }

    // Timeout de transporte não pode escapar do adapter: falha de rede é modelada, nunca lançada
    // na cara de quem processa a fila.
    [Fact]
    public async Task ExtractAsync_WhenTheTransportTimesOut_ShouldNotThrow()
    {
        var handler = StubHttpMessageHandler.Throwing(
            new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout"));

        var (result, _) = await ExtractAsync(handler);

        Assert.Same(ExtractionStatus.Unavailable, result.Status);
        Assert.True(result.IsRetryable);
        Assert.Equal("provider_timeout", result.ReasonCode);
    }

    // Resposta bem-sucedida com corpo ilegível também é ausência, não exceção — o modelo pode
    // devolver texto fora do schema, e isso não pode derrubar o worker.
    [Fact]
    public async Task ExtractAsync_WhenTheBodyIsNotTheExpectedShape_ShouldNotThrow()
    {
        var (result, _) = await ExtractAsync(StubHttpMessageHandler.Ok("""{"candidates":[]}"""));

        // O provedor RESPONDEU — insistir devolveria o mesmo corpo e gastaria cota.
        Assert.True(result.ProviderAnswered);
        Assert.False(result.IsRetryable);
    }

    private static async Task<(ExtractionAttempt Result, StubHttpMessageHandler Handler)> ExtractAsync(
        StubHttpMessageHandler handler,
        string? supplemental = null,
        int maxCallsPerDay = 100)
    {
        var options = Options.Create(new DocumentIntelligenceOptions
        {
            Provider = "Gemini",
            ApiKey = "chave-de-teste",
            Model = "gemini-3.1-flash-lite",
            MaxCallsPerTenantPerDay = maxCallsPerDay,
            MinIntervalMs = 0,
            MaxPages = 5,
        });

        using var budget = new ExtractionBudget(
            new FixedTimeProvider(Now), NullLogger<ExtractionBudget>.Instance);

        // O adapter monta a URL absoluta a partir de `BaseUrl`, então o cliente não precisa de
        // BaseAddress — o que o teste substitui é só o transporte.
        var service = new GeminiDocumentIntelligence(
            new StubHttpClientFactory(handler),
            budget,
            options,
            NullLogger<GeminiDocumentIntelligence>.Instance);

        var payload = DocumentPayload.From(
            Tenant, PdfBytes, DocumentPayload.PDF, supplemental, supplementalTextIsHtml: false);

        var result = await service.ExtractAsync(payload, ExtractionHints.None, CancellationToken.None);

        return (result, handler);
    }
}
