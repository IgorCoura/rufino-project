namespace BillPayment.IntegrationTests.Asaas;

using System.Net;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Asaas;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Tradução dos adapters de PAGAMENTO (<c>POST /v3/bill</c> e vizinhos) para os resultados de
/// gateway do domínio. Sem banco e sem rede, no molde de <see cref="AsaasBillLookupServiceTests"/>
/// — e pelo mesmo motivo: a classificação Refused × Unavailable decide entre "desistir da ordem"
/// e "retentar", e errá-la mata uma ordem boa ou insiste contra uma recusa.
/// </summary>
public sealed class AsaasPaymentGatewayTests
{
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private const string ExternalReference = "0195a1f0-0000-7000-8000-00000000dead";
    private static readonly DateTime Today = new(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

    private static readonly CredentialRef TenantCredential =
        CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-00000000c0fe"));

    // Resposta de agendamento aceito, como o contrato medido da consulta sugere para o pague-contas.
    private const string AcceptedBody = """
        {
          "id": "pay_123",
          "status": "PENDING",
          "value": 615.07,
          "fee": 1.99,
          "scheduleDate": "2026-08-10",
          "externalReference": "0195a1f0-0000-7000-8000-00000000dead"
        }
        """;

    // Agendamento aceito vira Accepted com o retrato completo: id, status mapeado, data e taxa.
    [Fact]
    public async Task ScheduleAsync_WithAnAcceptedResponse_ShouldMapTheSnapshot()
    {
        var handler = StubHttpMessageHandler.Ok(AcceptedBody);

        var result = await ScheduleAsync(handler);

        Assert.True(result.IsAccepted);
        Assert.Equal("pay_123", result.Snapshot!.ProviderOrderId);
        Assert.Equal(PaymentOrderStatus.Pending, result.Snapshot.Status);
        Assert.Equal("PENDING", result.Snapshot.RawStatus);
        Assert.Equal(new DateOnly(2026, 8, 10), result.Snapshot.EffectiveScheduleDate);
        Assert.Equal(1.99m, result.Snapshot.Fee!.Amount);
    }

    // O provedor recebe a linha, a NOSSA referência e o valor no endpoint do pague-contas — e a
    // chave do tenant viaja no access_token daquela chamada, nunca num cliente compartilhado.
    [Fact]
    public async Task ScheduleAsync_ShouldPostTheLineAndTheReferenceWithTheTenantsKey()
    {
        var handler = StubHttpMessageHandler.Ok(AcceptedBody);

        await ScheduleAsync(handler);

        Assert.EndsWith("/bill", handler.LastRequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains(BankSlipLine, handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains(ExternalReference, handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Contains("615.07", handler.LastRequestBody, StringComparison.Ordinal);
        Assert.Equal("chave-do-tenant", handler.LastRequestHeaders["access_token"]);
    }

    // Recusa do provedor (4xx com código) é Refused com o motivo preservado — a ordem desiste
    // com o porquê visível, em vez de retentar contra uma recusa determinística.
    [Fact]
    public async Task ScheduleAsync_WhenTheProviderRefuses_ShouldReturnRefusedWithTheReason()
    {
        var handler = StubHttpMessageHandler.Error(
            HttpStatusCode.BadRequest, "invalid_bank_slip", "Boleto inválido para pagamento.");

        var result = await ScheduleAsync(handler);

        Assert.False(result.IsAccepted);
        Assert.False(result.IsRetryable);
        Assert.Equal("invalid_bank_slip", result.ReasonCode);
        Assert.Equal("Boleto inválido para pagamento.", result.ProviderMessage);
    }

    // Falha do lado do provedor e limite de taxa são Unavailable (a fila retenta pela consulta
    // de referência); 4xx genérico é Refused (retentar devolveria a mesma recusa).
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    public async Task ScheduleAsync_ShouldClassifyRetryabilityByStatus(HttpStatusCode status, bool expectedRetryable)
    {
        var result = await ScheduleAsync(new StubHttpMessageHandler(status, "{}"));

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedRetryable, result.IsRetryable);
    }

    // Sem resposta da rede nada foi aprendido — Unavailable, e a retentativa é da fila.
    [Fact]
    public async Task ScheduleAsync_WhenTransportFails_ShouldReturnUnavailable()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("conexão recusada"));

        var result = await ScheduleAsync(handler);

        Assert.False(result.IsAccepted);
        Assert.True(result.IsRetryable);
        Assert.Equal("transport_error", result.ReasonCode);
    }

    // 200 sem id não é aceite: sem o identificador do provedor não há como conciliar nem
    // cancelar, e tratar como aceito deixaria uma ordem órfã do lado de lá.
    [Fact]
    public async Task ScheduleAsync_WithSuccessButNoProviderId_ShouldReturnRefused()
    {
        var result = await ScheduleAsync(StubHttpMessageHandler.Ok("""{"status":"PENDING"}"""));

        Assert.False(result.IsAccepted);
        Assert.False(result.IsRetryable);
        Assert.Equal("missing_provider_id", result.ReasonCode);
    }

    // Status que o provedor inventar amanhã cai em Pending, com o nome cru preservado na
    // evidência — a conciliação segue vigiando em vez de declarar desfecho por chute.
    [Fact]
    public async Task ScheduleAsync_WithAnUnknownStatus_ShouldFallBackToPendingKeepingTheRawName()
    {
        var body = """{"id":"pay_9","status":"AWAITING_SOMETHING_NEW"}""";

        var result = await ScheduleAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsAccepted);
        Assert.Equal(PaymentOrderStatus.Pending, result.Snapshot!.Status);
        Assert.Equal("AWAITING_SOMETHING_NEW", result.Snapshot.RawStatus);
    }

    // Tenant sem chave: Unavailable com o motivo próprio e NENHUMA requisição sai — dinheiro
    // nunca se move com a chave de outra pessoa.
    [Fact]
    public async Task ScheduleAsync_WithoutATenantCredential_ShouldDegradeWithoutTouchingTheNetwork()
    {
        var handler = StubHttpMessageHandler.Ok(AcceptedBody);

        var result = await ScheduleAsync(handler, credential: null);

        Assert.False(result.IsAccepted);
        Assert.True(result.IsRetryable);
        Assert.Equal(AsaasClientProvider.TENANT_KEY_NOT_CONFIGURED, result.ReasonCode);
        Assert.Equal(0, handler.RequestCount);
    }

    // A consulta de idempotência: lista vazia é NotFound — o sinal de que o reenvio é seguro.
    [Fact]
    public async Task FindByExternalReference_WithAnEmptyList_ShouldReturnNotFound()
    {
        var handler = StubHttpMessageHandler.Ok("""{"totalCount":0,"data":[]}""");

        var result = await FindAsync(handler);

        Assert.False(result.IsFound);
        Assert.False(result.IsUnavailable);
        Assert.Contains(
            $"externalReference={ExternalReference}",
            handler.LastRequestUri!.Query,
            StringComparison.Ordinal);
    }

    // A consulta que encontra a ordem devolve o retrato — é a adoção que impede o reenvio.
    [Fact]
    public async Task FindByExternalReference_WhenTheOrderExists_ShouldReturnTheSnapshot()
    {
        var body = """{"totalCount":1,"data":[{"id":"pay_found","status":"BANK_PROCESSING"}]}""";

        var result = await FindAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsFound);
        Assert.Equal("pay_found", result.Snapshot!.ProviderOrderId);
        Assert.Equal(PaymentOrderStatus.BankProcessing, result.Snapshot.Status);
    }

    // Provedor fora do ar na consulta de idempotência é Unavailable — trava o reenvio, o lado
    // seguro: um NotFound aqui autorizaria submeter de novo sem saber se a primeira pegou.
    [Fact]
    public async Task FindByExternalReference_WhenTheProviderIsDown_ShouldReturnUnavailable()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "{}");

        var result = await FindAsync(handler);

        Assert.True(result.IsUnavailable);
        Assert.False(result.IsFound);
    }

    // O retrato do pagamento concluído carrega a URL do comprovante — que fica no snapshot para
    // consumo imediato, nunca persistida (credencial ao portador).
    [Fact]
    public async Task GetAsync_WithAPaidResponse_ShouldExtractTheReceiptUrl()
    {
        var body = """
            {
              "id": "pay_123",
              "status": "PAID",
              "paymentDate": "2026-08-11",
              "transactionReceiptUrl": "https://www.asaas.com/comprovantes/000123"
            }
            """;

        var result = await GetAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsFound);
        Assert.Equal(PaymentOrderStatus.Paid, result.Snapshot!.Status);
        Assert.Equal(new DateOnly(2026, 8, 11), result.Snapshot.PaidAt);
        Assert.Equal("https://www.asaas.com/comprovantes/000123", result.Snapshot.ReceiptUrl);
    }

    // Os failReasons chegam em formato não documentado ("array" de quê?) e são lidos com
    // frouxidão: string, objeto com description, e o resto vira texto cru.
    [Fact]
    public async Task GetAsync_WithFailedResponse_ShouldReadTheFailReasons()
    {
        var body = """
            {
              "id": "pay_123",
              "status": "FAILED",
              "failReasons": ["saldo insuficiente", {"description": "conta bloqueada"}]
            }
            """;

        var result = await GetAsync(StubHttpMessageHandler.Ok(body));

        Assert.Equal(PaymentOrderStatus.Failed, result.Snapshot!.Status);
        Assert.Collection(
            result.Snapshot.FailReasons,
            reason => Assert.Equal("saldo insuficiente", reason),
            reason => Assert.Equal("conta bloqueada", reason));
    }

    // Cancelamento confirmado pelo provedor (status CANCELLED no corpo) é Cancelled.
    [Fact]
    public async Task CancelAsync_WhenTheProviderConfirms_ShouldReturnCancelled()
    {
        var handler = StubHttpMessageHandler.Ok("""{"id":"pay_123","status":"CANCELLED"}""");

        var result = await CancelAsync(handler);

        Assert.True(result.IsCancelled);
        Assert.EndsWith("/bill/pay_123/cancel", handler.LastRequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    // 200 sem o status CANCELLED não é cancelamento: o estado local só muda com confirmação, e
    // fingir que cancelou deixaria a ordem "cancelada" pagando de verdade.
    [Fact]
    public async Task CancelAsync_WhenTheStatusDidNotChange_ShouldReturnRefused()
    {
        var result = await CancelAsync(StubHttpMessageHandler.Ok("""{"id":"pay_123","status":"BANK_PROCESSING"}"""));

        Assert.False(result.IsCancelled);
        Assert.False(result.IsRetryable);
        Assert.Equal("not_cancellable", result.ReasonCode);
    }

    // Recusa explícita × provedor fora do ar: a primeira desiste (409 na borda), a segunda
    // convida a tentar de novo — colapsá-las faria rede instável virar "não cancelável".
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    public async Task CancelAsync_ShouldClassifyFailuresByRetryability(HttpStatusCode status, bool expectedRetryable)
    {
        var result = await CancelAsync(new StubHttpMessageHandler(status, "{}"));

        Assert.False(result.IsCancelled);
        Assert.Equal(expectedRetryable, result.IsRetryable);
    }

    // O trilho Pix consulta a transação no caminho próprio, e DONE é o Paid de lá.
    [Fact]
    public async Task PixGetAsync_WithADoneTransaction_ShouldMapToPaid()
    {
        var handler = StubHttpMessageHandler.Ok("""
            {"id":"pix_9","status":"DONE","effectiveDate":"2026-08-12","transactionReceiptUrl":"https://www.asaas.com/comprovantes/pix9"}
            """);

        var result = await PixGetAsync(handler);

        Assert.True(result.IsFound);
        Assert.Equal(PaymentOrderStatus.Paid, result.Snapshot!.Status);
        Assert.Equal(new DateOnly(2026, 8, 12), result.Snapshot.PaidAt);
        Assert.EndsWith("/pix/transactions/pix_9", handler.LastRequestUri!.AbsolutePath, StringComparison.Ordinal);
    }

    // O catálogo Pix é maior que o do boleto, e a recusa dele (REFUSED/ERROR) é Failed — o
    // desconhecido continua caindo em Pending, nunca em desfecho por chute.
    [Theory]
    [InlineData("AWAITING_BALANCE_VALIDATION", "Pending")]
    [InlineData("REQUESTED", "BankProcessing")]
    [InlineData("DONE", "Paid")]
    [InlineData("REFUSED", "Failed")]
    [InlineData("ERROR", "Failed")]
    [InlineData("CANCELLED", "Cancelled")]
    [InlineData("REFUNDED", "Refunded")]
    [InlineData("SOMETHING_NEW", "Pending")]
    public void FromPixPayment_ShouldTranslateTheProvidersCatalogue(string raw, string expected)
    {
        Assert.Equal(expected, AsaasPaymentStatusMap.FromPixPayment(raw).Name);
    }

    private static Task<PaymentSubmissionResult> ScheduleAsync(StubHttpMessageHandler handler)
        => ScheduleAsync(handler, TenantCredential);

    private static async Task<PaymentSubmissionResult> ScheduleAsync(
        StubHttpMessageHandler handler, CredentialRef? credential)
    {
        var gateway = BuildBillGateway(handler);

        return await gateway.ScheduleAsync(
            credential,
            DigitableLine.Parse(BankSlipLine, Today),
            new Money(615.07m, Currency.BRL),
            dueDate: new DateOnly(2026, 8, 14),
            scheduleDate: new DateOnly(2026, 8, 10),
            ExternalReference,
            description: null,
            CancellationToken.None);
    }

    private static async Task<PaymentFetchResult> FindAsync(StubHttpMessageHandler handler)
        => await BuildBillGateway(handler)
            .FindByExternalReferenceAsync(TenantCredential, ExternalReference, CancellationToken.None);

    private static async Task<PaymentFetchResult> GetAsync(StubHttpMessageHandler handler)
        => await BuildBillGateway(handler).GetAsync(TenantCredential, "pay_123", CancellationToken.None);

    private static async Task<PaymentCancellationResult> CancelAsync(StubHttpMessageHandler handler)
        => await BuildBillGateway(handler).CancelAsync(TenantCredential, "pay_123", CancellationToken.None);

    private static async Task<PaymentFetchResult> PixGetAsync(StubHttpMessageHandler handler)
        => await BuildPixGateway(handler).GetAsync(TenantCredential, "pix_9", CancellationToken.None);

    private static AsaasBillPaymentGateway BuildBillGateway(StubHttpMessageHandler handler)
        => new(BuildClientProvider(handler), NullLogger<AsaasBillPaymentGateway>.Instance);

    private static AsaasPixPaymentGateway BuildPixGateway(StubHttpMessageHandler handler)
        => new(BuildClientProvider(handler), NullLogger<AsaasPixPaymentGateway>.Instance);

    private static AsaasClientProvider BuildClientProvider(StubHttpMessageHandler handler)
        => new(
            new StubHttpClientFactory(handler, new Uri("https://api-sandbox.asaas.com/v3/")),
            new FakeSecretVault("chave-do-tenant"),
            NullLogger<AsaasClientProvider>.Instance);
}
