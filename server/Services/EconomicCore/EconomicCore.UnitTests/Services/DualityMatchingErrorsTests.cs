namespace EconomicCore.UnitTests.Services;

using EconomicCore.Domain.Services;

public class DualityMatchingErrorsTests
{
    // NullEvent (DMS01) carrega o nome do argumento ofensor.
    [Fact]
    public void NullEvent_ShouldReturnECC_DMS01_WithParameterName()
    {
        var ex = DualityMatchingErrors.NullEvent("paymentEvent");

        Assert.Equal("ECC.DMS01", ex.Id);
        Assert.Single(ex.Parameters);
        Assert.Equal("paymentEvent", ex.Parameters[0]);
    }

    // TenantMismatch (DMS02) carrega os dois TenantIds.
    [Fact]
    public void TenantMismatch_ShouldReturnECC_DMS02_WithTwoGuids()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var ex = DualityMatchingErrors.TenantMismatch(a, b);

        Assert.Equal("ECC.DMS02", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // ConsumptionNotCoveredByCommitment (DMS03) — sem parâmetros, indica ausência de cobertura.
    [Fact]
    public void ConsumptionNotCoveredByCommitment_ShouldReturnECC_DMS03()
    {
        var ex = DualityMatchingErrors.ConsumptionNotCoveredByCommitment();

        Assert.Equal("ECC.DMS03", ex.Id);
        Assert.Empty(ex.Parameters);
    }

    // PaymentNotCoveredByCommitment (DMS04) — sem parâmetros, indica ausência de cobertura.
    [Fact]
    public void PaymentNotCoveredByCommitment_ShouldReturnECC_DMS04()
    {
        var ex = DualityMatchingErrors.PaymentNotCoveredByCommitment();

        Assert.Equal("ECC.DMS04", ex.Id);
        Assert.Empty(ex.Parameters);
    }

    // CurrencyMismatch (DMS05).
    [Fact]
    public void CurrencyMismatch_ShouldReturnECC_DMS05_WithBothCurrencyNames()
    {
        var ex = DualityMatchingErrors.CurrencyMismatch("BRL", "USD");

        Assert.Equal("ECC.DMS05", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }
}
