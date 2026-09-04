namespace BillPayment.UnitTests.Lookups;

using BillPayment.Domain.Lookups;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.Lookups.Mothers;

public class LookupResultTests
{
    // Consulta resolvida carrega o retrato e nenhum motivo.
    [Fact]
    public void Resolved_ShouldCarryTheSnapshotAndNoReason()
    {
        var result = BillLookupResult.Resolved(LookupMother.BankSlip(), LookupMother.ConsultedAt);

        Assert.True(result.IsResolved);
        Assert.False(result.IsRetryable);
        Assert.NotNull(result.Snapshot);
        Assert.Null(result.ReasonCode);
    }

    // "O provedor não conhece este título" é resposta normal, não falha — e retentar devolve o
    // mesmo. Foi o que aconteceu com as 12 linhas de cobrança do corpus em sandbox.
    [Fact]
    public void Unresolved_ShouldCarryTheReasonWithoutBeingRetryable()
    {
        var result = BillLookupResult.Unresolved(
            "unregistered_bank_slip", "Boleto não registrado na rede bancária.", LookupMother.ConsultedAt);

        Assert.False(result.IsResolved);
        Assert.False(result.IsRetryable);
        Assert.Equal("unregistered_bank_slip", result.ReasonCode);
        Assert.Null(result.Snapshot);
    }

    // "Não respondi" é fato sobre a infraestrutura: nada foi aprendido sobre o documento e
    // vale consultar de novo.
    [Fact]
    public void Unavailable_ShouldBeRetryable()
    {
        var result = PixLookupResult.Unavailable("timeout", null, LookupMother.ConsultedAt);

        Assert.False(result.IsResolved);
        Assert.True(result.IsRetryable);
        Assert.Equal("timeout", result.ReasonCode);
    }

    // Resultado sem retrato precisa dizer por quê — é essa string que vira evidência — BLP.LKP03.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Unresolved_WithoutAReasonCode_ShouldThrow_BLP_LKP03(string reasonCode)
    {
        var ex = Assert.Throws<DomainException>(
            () => BillLookupResult.Unresolved(reasonCode, null, LookupMother.ConsultedAt));

        Assert.Equal("BLP.LKP03", ex.Id);
    }

    // Resultado resolvido sem retrato é contradição — BLP.LKP04.
    [Fact]
    public void Resolved_WithoutASnapshot_ShouldThrow_BLP_LKP04()
    {
        var ex = Assert.Throws<DomainException>(
            () => PixLookupResult.Resolved(null!, LookupMother.ConsultedAt));

        Assert.Equal("BLP.LKP04", ex.Id);
    }

    // Mensagem enorme do provedor é aparada em vez de derrubar a consulta — o texto é evidência,
    // não decide nada.
    [Fact]
    public void Unavailable_WithOverlongProviderMessage_ShouldClampIt()
    {
        var result = BillLookupResult.Unavailable(
            "provider_error",
            new string('x', LookupResult.PROVIDER_MESSAGE_MAX_LENGTH + 200),
            LookupMother.ConsultedAt);

        Assert.Equal(LookupResult.PROVIDER_MESSAGE_MAX_LENGTH, result.ProviderMessage!.Length);
    }

    // O trilho Pix tem o mesmo contrato de resultado do trilho boleto.
    [Fact]
    public void Resolved_ForPix_ShouldCarryThePixSnapshot()
    {
        var result = PixLookupResult.Resolved(LookupMother.PixDynamic(), LookupMother.ConsultedAt);

        Assert.True(result.IsResolved);
        Assert.Equal(LookupMother.BENEFICIARY_CNPJ, result.Snapshot!.Receiver.TaxId!.Value);
    }
}
