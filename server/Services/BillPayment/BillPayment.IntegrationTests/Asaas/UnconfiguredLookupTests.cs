namespace BillPayment.IntegrationTests.Asaas;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A consulta oficial é POR TENANT (2026-08-31), e a suíte roda sem nenhuma chave configurada —
/// testes não devem ter credencial capaz de pagar contas. O que esta classe garante é que o
/// tenant SEM chave própria tenha a ausência registrada como <strong>"não foi possível
/// verificar"</strong>, com motivo acionável, e nunca confundida com verificação concluída.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class UnconfiguredLookupTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private static readonly DateTime Today = new(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

    // Tenant sem chave: a consulta de boleto é indisponível, com o motivo que aponta a solução.
    [Fact]
    public async Task SimulateAsync_WithoutATenantKey_ShouldReturnUnavailable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillLookupService>();

        var result = await service.SimulateAsync(
            credential: null, DigitableLine.Parse(BankSlipLine, Today), CancellationToken.None);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.Equal("tenant_key_not_configured", result.ReasonCode);
        Assert.Null(result.Snapshot);
    }

    // O trilho Pix degrada do mesmo jeito — nunca resolve vazio "com sucesso".
    [Fact]
    public async Task DecodeAsync_WithoutATenantKey_ShouldReturnUnavailable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPixLookupService>();

        var result = await service.DecodeAsync(
            credential: null, PixPayload.Parse(DynamicPix), null, CancellationToken.None);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.False(result.IsResolved);
    }

    // Indisponível é retentável: quando o tenant configurar a chave, a consulta volta a valer.
    [Fact]
    public async Task SimulateAsync_WithoutATenantKey_ShouldBeRetryable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillLookupService>();

        var result = await service.SimulateAsync(
            credential: null, DigitableLine.Parse(BankSlipLine, Today), CancellationToken.None);

        Assert.True(result.IsRetryable);
    }
}
