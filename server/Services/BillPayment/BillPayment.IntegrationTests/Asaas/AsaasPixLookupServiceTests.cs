namespace BillPayment.IntegrationTests.Asaas;

using System.Net;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Secrets;
using BillPayment.Domain.SharedKernel;
using BillPayment.Infra.Asaas;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Tradução da resposta de <c>POST /v3/pix/qrCodes/decode</c> para <see cref="PixLookupSnapshot"/>.
/// </summary>
public sealed class AsaasPixLookupServiceTests
{
    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    private const string FullDecodeBody = """
        {
          "type": "DYNAMIC",
          "receiver": {
            "name": "EDP SAO PAULO DISTRIBUICAO DE ENERGIA S.A.",
            "tradingName": "EDP SP",
            "cpfCnpj": "11222333000181",
            "ispb": "60701190",
            "ispbName": "ITAU UNIBANCO S.A.",
            "personType": "JURIDICA",
            "accountType": "CHECKING"
          },
          "payer": { "name": "F*** J***", "cpfCnpj": "***.982.247-**" },
          "value": 150.00,
          "totalValue": 153.20,
          "interest": 3.20,
          "fine": 0,
          "discount": 0,
          "dueDate": "2026-08-20",
          "expirationDate": "2026-08-25T23:59:59Z",
          "canBePaid": true,
          "canBePaidWithDifferentValue": false,
          "conciliationIdentifier": "CONC-123"
        }
        """;

    // O decode é a única fonte do documento do recebedor — o BR Code carrega chave e nome, nunca CPF/CNPJ.
    [Fact]
    public async Task DecodeAsync_WithFullResponse_ShouldMapReceiverAmountsAndInstitution()
    {
        var result = await DecodeAsync(StubHttpMessageHandler.Ok(FullDecodeBody));

        Assert.True(result.IsResolved);

        var snapshot = result.Snapshot!;
        Assert.Equal("11222333000181", snapshot.Receiver.TaxId!.Value);
        Assert.Equal("EDP SP", snapshot.Receiver.TradingName);
        Assert.Equal("60701190", snapshot.ReceiverIspb);
        Assert.Same(TaxIdKind.CNPJ, snapshot.ReceiverKind);
        Assert.True(snapshot.IsDynamic);
        Assert.Equal(150.00m, snapshot.Amount!.Amount);
        Assert.Equal(153.20m, snapshot.TotalAmount!.Amount);
        Assert.Equal(new DateOnly(2026, 8, 20), snapshot.DueDate);
        Assert.Equal("CONC-123", snapshot.ConciliationIdentifier);
        Assert.Equal(Now, snapshot.ConsultedAt);
    }

    // O pagador chega mascarado e é guardado só para poder contradizer (ADR-004).
    [Fact]
    public async Task DecodeAsync_ShouldKeepTheMaskedPayerForContradiction()
    {
        var result = await DecodeAsync(StubHttpMessageHandler.Ok(FullDecodeBody));

        var payer = result.Snapshot!.Payer!;
        Assert.Equal("***982247**", payer.MaskedTaxId);
        Assert.True(payer.IsCompatibleWith(TaxId.Parse("52998224725")));
        Assert.False(payer.IsCompatibleWith(TaxId.Parse("11144477735")));
    }

    // A instituição recalcula encargos para a data prevista: mandar hoje quando o pagamento
    // será na semana que vem devolveria um valor que não é o que será debitado.
    [Fact]
    public async Task DecodeAsync_WithExpectedPaymentDate_ShouldSendItToTheProvider()
    {
        var handler = StubHttpMessageHandler.Ok(FullDecodeBody);

        await DecodeAsync(handler, new DateOnly(2026, 8, 12));

        Assert.EndsWith("/pix/qrCodes/decode", handler.LastRequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains("2026-08-12", handler.LastRequestBody, StringComparison.Ordinal);
    }

    // Sem data prevista o campo é omitido, e o provedor assume hoje.
    [Fact]
    public async Task DecodeAsync_WithoutExpectedPaymentDate_ShouldOmitTheField()
    {
        var handler = StubHttpMessageHandler.Ok(FullDecodeBody);

        await DecodeAsync(handler);

        Assert.DoesNotContain("expectedPaymentDate", handler.LastRequestBody, StringComparison.Ordinal);
    }

    // QR estático não traz valor nem vencimento — os checks correspondentes saem pulados.
    [Fact]
    public async Task DecodeAsync_ForStaticQr_ShouldResolveWithoutAmountOrDueDate()
    {
        var body = """
            {"type":"STATIC","receiver":{"name":"SABESP","cpfCnpj":"11222333000181"}}
            """;

        var result = await DecodeAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsResolved);
        Assert.False(result.Snapshot!.IsDynamic);
        Assert.Null(result.Snapshot.PayableAmount);
        Assert.False(result.Snapshot.SupportsAmountCheck);
    }

    // Porteira: QR que o provedor já sabe que não paga chega marcado, com o motivo dele.
    [Fact]
    public async Task DecodeAsync_WhenProviderRefusesTheQr_ShouldResolveWithCanBePaidFalse()
    {
        var body = """
            {"receiver":{"name":"SABESP"},"canBePaid":false,"cannotBePaidReason":"QR_CODE_EXPIRED"}
            """;

        var result = await DecodeAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsResolved);
        Assert.False(result.Snapshot!.CanBePaid);
        Assert.Equal("QR_CODE_EXPIRED", result.Snapshot.CannotBePaidReason);
    }

    // Payload que o provedor não decodifica é fato sobre o QR: não retentável.
    [Fact]
    public async Task DecodeAsync_WhenPayloadIsRejected_ShouldReturnUnresolved()
    {
        var handler = StubHttpMessageHandler.Error(
            HttpStatusCode.BadRequest, "invalid_qr_code", "QR Code inválido.");

        var result = await DecodeAsync(handler);

        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("invalid_qr_code", result.ReasonCode);
    }

    // Sem recebedor não há retrato: o trilho Pix ficaria sem check de beneficiário.
    [Fact]
    public async Task DecodeAsync_WithoutReceiver_ShouldReturnUnresolved()
    {
        var result = await DecodeAsync(StubHttpMessageHandler.Ok("""{"type":"DYNAMIC"}"""));

        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("empty_receiver", result.ReasonCode);
    }

    // Máscara sem dígito visível não contradiz nada; vira pagador só com nome, em vez de
    // derrubar a consulta inteira.
    [Fact]
    public async Task DecodeAsync_WithFullyMaskedPayerDocument_ShouldKeepOnlyThePayerName()
    {
        var body = """
            {"receiver":{"name":"SABESP"},"payer":{"name":"F*** J***","cpfCnpj":"***.***.***-**"}}
            """;

        var result = await DecodeAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsResolved);
        Assert.Equal("F*** J***", result.Snapshot!.Payer!.Name);
        Assert.Null(result.Snapshot.Payer.MaskedTaxId);
    }

    // Indisponibilidade do provedor não diz nada sobre o QR.
    [Fact]
    public async Task DecodeAsync_WhenProviderFails_ShouldReturnUnavailable()
    {
        var result = await DecodeAsync(new StubHttpMessageHandler(HttpStatusCode.BadGateway, "{}"));

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.True(result.IsRetryable);
    }

    // Tenant sem chave configurada degrada sem tocar a rede, com o motivo próprio.
    [Fact]
    public async Task DecodeAsync_WithoutATenantCredential_ShouldDegradeWithoutTouchingTheNetwork()
    {
        var handler = StubHttpMessageHandler.Ok("{}");

        var result = await DecodeAsync(handler, credential: null);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.Equal("tenant_key_not_configured", result.ReasonCode);
        Assert.Equal(0, handler.RequestCount);
    }

    private static readonly CredentialRef TenantCredential =
        CredentialRef.ForLocalVault(new Guid("0195a1f0-0000-7000-8000-00000000c0fe"));

    private static Task<PixLookupResult> DecodeAsync(
        StubHttpMessageHandler handler,
        DateOnly? expectedPaymentDate = null)
        => DecodeAsync(handler, TenantCredential, expectedPaymentDate);

    private static async Task<PixLookupResult> DecodeAsync(
        StubHttpMessageHandler handler,
        CredentialRef? credential,
        DateOnly? expectedPaymentDate = null)
    {
        var clientProvider = new AsaasClientProvider(
            new StubHttpClientFactory(handler, new Uri("https://api-sandbox.asaas.com/v3/")),
            new FakeSecretVault("chave-do-tenant"),
            NullLogger<AsaasClientProvider>.Instance);

        var service = new AsaasPixLookupService(
            clientProvider, new FixedTimeProvider(Now), NullLogger<AsaasPixLookupService>.Instance);

        return await service.DecodeAsync(
            credential, PixPayload.Parse(DynamicPix), expectedPaymentDate, CancellationToken.None);
    }
}
