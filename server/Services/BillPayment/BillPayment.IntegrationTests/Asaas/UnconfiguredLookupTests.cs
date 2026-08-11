namespace BillPayment.IntegrationTests.Asaas;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// A suíte roda sem chave do provedor de propósito — testes não devem ter credencial capaz de
/// pagar contas. O que esta classe garante é que a ausência dela seja registrada como
/// <strong>"não foi possível verificar"</strong>, e nunca confundida com verificação concluída.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class UnconfiguredLookupTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string BankSlipLine = "34191234546789012345767890123457314880000061507";

    private const string DynamicPix =
        "00020101021226760014br.gov.bcb.pix2554pix.example.com/qr/v2/9d36b84fc70b478fb95c12729b90ca255204000053039865802BR5912EDP TESTE SA6007TAUBATE62120508TXID00026304E47A";

    private static readonly DateTime Today = new(2026, 8, 5, 0, 0, 0, DateTimeKind.Utc);

    // Sem credencial, a consulta de boleto é indisponível — com motivo explícito, não silêncio.
    [Fact]
    public async Task SimulateAsync_WithoutAConfiguredApiKey_ShouldReturnUnavailable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillLookupService>();

        var result = await service.SimulateAsync(DigitableLine.Parse(BankSlipLine, Today), CancellationToken.None);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.Equal("provider_not_configured", result.ReasonCode);
        Assert.Null(result.Snapshot);
    }

    // O trilho Pix degrada do mesmo jeito — nunca resolve vazio "com sucesso".
    [Fact]
    public async Task DecodeAsync_WithoutAConfiguredApiKey_ShouldReturnUnavailable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IPixLookupService>();

        var result = await service.DecodeAsync(PixPayload.Parse(DynamicPix), null, CancellationToken.None);

        Assert.Equal(LookupStatus.Unavailable, result.Status);
        Assert.False(result.IsResolved);
    }

    // Indisponível é retentável: quando a credencial for configurada, a consulta volta a valer.
    [Fact]
    public async Task SimulateAsync_WithoutAConfiguredApiKey_ShouldBeRetryable()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBillLookupService>();

        var result = await service.SimulateAsync(DigitableLine.Parse(BankSlipLine, Today), CancellationToken.None);

        Assert.True(result.IsRetryable);
    }
}
