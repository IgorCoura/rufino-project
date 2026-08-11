namespace BillPayment.IntegrationTests.Asaas;

using System.Net;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Infra.Asaas;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
/// Tradução da resposta de <c>POST /v3/bill/simulate</c> para <see cref="LookupSnapshot"/>.
/// Sem banco e sem rede — ver a nota em <see cref="StubHttpMessageHandler"/>.
/// </summary>
public sealed class AsaasBillLookupServiceTests
{
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";
    private static readonly DateTime Today = new(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset Now = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    // Resposta completa de cobrança bancária, como produção deve responder.
    private const string FullBankSlipBody = """
        {
          "fee": 1.99,
          "minimumScheduleDate": "2026-08-06",
          "bankSlipInfo": {
            "beneficiaryName": "PADARIA SAO JOSE LTDA",
            "beneficiaryCpfCnpj": "11222333000181",
            "companyName": "PADARIA SAO JOSE",
            "bank": { "code": "341", "name": "ITAU UNIBANCO" },
            "value": 153.20,
            "originalValue": 150.00,
            "interestValue": 3.20,
            "fineValue": 0,
            "discountValue": 0,
            "allowChangeValue": false,
            "dueDate": "2026-07-30",
            "isOverdue": true
          }
        }
        """;

    // A resposta que a sprint 1.0 realmente mediu em arrecadação: nome comercial e valor,
    // sem documento, sem banco e sem vencimento.
    private const string UtilityBody = """
        {
          "bankSlipInfo": {
            "companyName": "SABESP",
            "value": 89.34,
            "originalValue": 89.34,
            "isOverdue": false
          }
        }
        """;

    // Todo campo devolvido pelo provedor chega ao retrato, incluindo os que só servem de evidência.
    [Fact]
    public async Task SimulateAsync_WithFullBankSlipResponse_ShouldMapEveryField()
    {
        var result = await SimulateAsync(StubHttpMessageHandler.Ok(FullBankSlipBody));

        Assert.True(result.IsResolved);

        var snapshot = result.Snapshot!;
        Assert.Equal("11222333000181", snapshot.Beneficiary.TaxId!.Value);
        Assert.Equal("PADARIA SAO JOSE LTDA", snapshot.Beneficiary.Name);
        Assert.Equal("PADARIA SAO JOSE", snapshot.Beneficiary.TradingName);
        Assert.Equal("341", snapshot.BankCode!.Value);
        Assert.Equal(153.20m, snapshot.Amount!.Amount);
        Assert.Equal(150.00m, snapshot.OriginalAmount!.Amount);
        Assert.Equal(3.20m, snapshot.Interest!.Amount);
        Assert.Equal(new DateOnly(2026, 7, 30), snapshot.DueDate);
        Assert.True(snapshot.IsOverdue);
        Assert.Equal(1.99m, snapshot.Fee!.Amount);
        Assert.Equal(new DateOnly(2026, 8, 6), snapshot.MinimumScheduleDate);
        Assert.Equal(Now, snapshot.ConsultedAt);
    }

    // Arrecadação resolve sem documento, sem banco e sem vencimento — se o adapter exigisse
    // esses campos, 100% desse tipo de documento ficaria sem consulta.
    [Fact]
    public async Task SimulateAsync_WithUtilityResponse_ShouldResolveWithNameOnly()
    {
        var result = await SimulateAsync(StubHttpMessageHandler.Ok(UtilityBody));

        Assert.True(result.IsResolved);
        Assert.False(result.Snapshot!.Beneficiary.HasTaxId);
        Assert.Null(result.Snapshot.BankCode);
        Assert.Null(result.Snapshot.DueDate);
        Assert.Equal("SABESP", result.Snapshot.Beneficiary.TradingName);
        Assert.Equal(89.34m, result.Snapshot.Amount!.Amount);
    }

    // Título não registrado é fato sobre o documento: não retentável, com o código do provedor
    // preservado. Foi assim que as 12 linhas de cobrança do corpus responderam em sandbox.
    [Fact]
    public async Task SimulateAsync_WhenBillIsNotRegistered_ShouldReturnUnresolvedWithTheProviderCode()
    {
        var handler = StubHttpMessageHandler.Error(
            HttpStatusCode.BadRequest, "unregistered_bank_slip", "Boleto não registrado na rede bancária.");

        var result = await SimulateAsync(handler);

        Assert.False(result.IsResolved);
        Assert.False(result.IsRetryable);
        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("unregistered_bank_slip", result.ReasonCode);
        Assert.Equal("Boleto não registrado na rede bancária.", result.ProviderMessage);
    }

    // 403 é o achado da sprint 1.0 (a consulta exige permissão de saque). Não se aprendeu nada
    // sobre o documento, então é indisponibilidade — nunca reprovação do boleto.
    [Fact]
    public async Task SimulateAsync_WhenCredentialLacksPermission_ShouldReturnUnavailable()
    {
        var handler = StubHttpMessageHandler.Error(
            HttpStatusCode.Forbidden, "insufficient_permission", "Chave sem permissão de saque.");

        var result = await SimulateAsync(handler);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.True(result.IsRetryable);
        Assert.Null(result.Snapshot);
    }

    // Falha do provedor e limite de taxa são retentáveis; 4xx genérico não é.
    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    public async Task SimulateAsync_ShouldClassifyRetryabilityByStatus(HttpStatusCode status, bool expectedRetryable)
    {
        var result = await SimulateAsync(new StubHttpMessageHandler(status, "{}"));

        Assert.False(result.IsResolved);
        Assert.Equal(expectedRetryable, result.IsRetryable);
    }

    // Sem resposta da rede nada foi aprendido sobre o documento — indisponível, retentável.
    [Fact]
    public async Task SimulateAsync_WhenTransportFails_ShouldReturnUnavailable()
    {
        var handler = StubHttpMessageHandler.Throwing(new HttpRequestException("conexão recusada"));

        var result = await SimulateAsync(handler);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.Equal("transport_error", result.ReasonCode);
    }

    // Corpo 200 sem bankSlipInfo é resposta do provedor, não falha de rede: não retentável.
    [Fact]
    public async Task SimulateAsync_WithSuccessButNoBankSlipInfo_ShouldReturnUnresolved()
    {
        var result = await SimulateAsync(StubHttpMessageHandler.Ok("""{"fee":1.99}"""));

        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("empty_bank_slip_info", result.ReasonCode);
    }

    // Corpo 200 que não é o contrato esperado não vira retrato e não adianta retentar.
    [Fact]
    public async Task SimulateAsync_WithMalformedBody_ShouldReturnUnresolved()
    {
        var result = await SimulateAsync(StubHttpMessageHandler.Ok("isto não é json"));

        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("malformed_response", result.ReasonCode);
    }

    // Resposta que resolve mas não identifica beneficiário nenhum não vira retrato: o check de
    // beneficiário compararia vazio com vazio.
    [Fact]
    public async Task SimulateAsync_WithoutAnyBeneficiaryIdentifier_ShouldReturnUnresolved()
    {
        var result = await SimulateAsync(StubHttpMessageHandler.Ok("""{"bankSlipInfo":{"value":10.0}}"""));

        Assert.Equal(LookupStatus.Unresolved, result.Status);
        Assert.Equal("beneficiary_not_identified", result.ReasonCode);
    }

    // O provedor recebe a linha digitável no campo que ele espera, e o caminho é o do simulate.
    [Fact]
    public async Task SimulateAsync_ShouldPostTheDigitableLineToTheSimulateEndpoint()
    {
        var handler = StubHttpMessageHandler.Ok(FullBankSlipBody);

        await SimulateAsync(handler);

        Assert.EndsWith("/bill/simulate", handler.LastRequestUri!.AbsolutePath, StringComparison.Ordinal);
        Assert.Contains(BankSlipLine, handler.LastRequestBody, StringComparison.Ordinal);
    }

    // Código de banco não atribuído vira ausência em vez de derrubar a consulta — o VO recusa "000".
    [Fact]
    public async Task SimulateAsync_WithUnassignedBankCode_ShouldLeaveTheBankAbsent()
    {
        var body = """
            {"bankSlipInfo":{"companyName":"X","bank":"000","value":10.0}}
            """;

        var result = await SimulateAsync(StubHttpMessageHandler.Ok(body));

        Assert.True(result.IsResolved);
        Assert.Null(result.Snapshot!.BankCode);
    }

    // O provedor pode devolver o banco como string simples em vez de objeto; as duas formas
    // são aceitas, porque o campo preenchido nunca foi observado e adivinhar errado
    // derrubaria a resposta inteira.
    [Fact]
    public async Task SimulateAsync_WithBankAsPlainString_ShouldStillReadTheCode()
    {
        var body = """
            {"bankSlipInfo":{"companyName":"X","bank":"237","value":10.0}}
            """;

        var result = await SimulateAsync(StubHttpMessageHandler.Ok(body));

        Assert.Equal("237", result.Snapshot!.BankCode!.Value);
    }

    private static async Task<BillLookupResult> SimulateAsync(StubHttpMessageHandler handler)
    {
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api-sandbox.asaas.com/v3/") };

        var service = new AsaasBillLookupService(
            http, new FixedTimeProvider(Now), NullLogger<AsaasBillLookupService>.Instance);

        return await service.SimulateAsync(DigitableLine.Parse(BankSlipLine, Today), CancellationToken.None);
    }
}
