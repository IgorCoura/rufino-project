namespace BillPayment.UnitTests.Secrets;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Secrets;

public class CredentialRefTests
{
    private static readonly Guid SecretId = new("0198a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b");

    // A referência do cofre local nasce com o esquema no prefixo — é ele que torna a migração
    // para outro cofre auditável por grep.
    [Fact]
    public void ForLocalVault_ShouldProduceASchemePrefixedReference()
    {
        var reference = CredentialRef.ForLocalVault(SecretId);

        Assert.Equal(CredentialRef.LOCAL_VAULT_SCHEME, reference.Scheme);
        Assert.True(reference.IsLocalVault);
        Assert.Equal($"bpv1:{SecretId:N}", reference.ToString());
    }

    // Ida e volta pela forma canônica é o que a coluna do banco guarda e relê.
    [Fact]
    public void Parse_OfItsOwnCanonicalForm_ShouldRoundTrip()
    {
        var original = CredentialRef.ForLocalVault(SecretId);

        var parsed = CredentialRef.Parse(original.ToString());

        Assert.Equal(original, parsed);
        Assert.Equal(SecretId, parsed.AsLocalVaultId());
    }

    // Guid vazio não referencia credencial nenhuma — BLP.SEC01.
    [Fact]
    public void ForLocalVault_WithEmptyGuid_ShouldThrow_BLP_SEC01()
    {
        var ex = Assert.Throws<DomainException>(() => CredentialRef.ForLocalVault(Guid.Empty));

        Assert.Equal("BLP.SEC01", ex.Id);
    }

    // Referência ausente é erro distinto de referência malformada — BLP.SEC01.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_WithAbsentValue_ShouldThrow_BLP_SEC01(string? value)
    {
        var ex = Assert.Throws<DomainException>(() => CredentialRef.Parse(value!));

        Assert.Equal("BLP.SEC01", ex.Id);
    }

    // Formato fora do padrão é recusado — e a mensagem não ecoa o valor, porque uma referência
    // malformada pode ser um segredo colado no campo errado.
    [Theory]
    [InlineData("sem-separador")]
    [InlineData(":sem-esquema")]
    [InlineData("bpv1:")]
    [InlineData("bpv1:tem:separador:demais")]
    [InlineData("bpv1:tem espaco")]
    public void Parse_WithMalformedValue_ShouldThrow_BLP_SEC02(string value)
    {
        var ex = Assert.Throws<DomainException>(() => CredentialRef.Parse(value));

        Assert.Equal("BLP.SEC02", ex.Id);
    }

    // Referência acima do limite da coluna é recusada antes de chegar ao banco — BLP.SEC02.
    [Fact]
    public void Parse_WithOverlongValue_ShouldThrow_BLP_SEC02()
    {
        var ex = Assert.Throws<DomainException>(
            () => CredentialRef.Parse("bpv1:" + new string('a', CredentialRef.MAX_LENGTH)));

        Assert.Equal("BLP.SEC02", ex.Id);
    }

    // Esquema desconhecido é referência válida em forma, mas não é do cofre local — quem
    // resolve é que decide o que fazer com ela.
    [Fact]
    public void Parse_WithAnotherScheme_ShouldParseButNotBeLocalVault()
    {
        var reference = CredentialRef.Parse("infisical:/rufino/bill-payment/asaas");

        Assert.False(reference.IsLocalVault);
        Assert.Equal("infisical", reference.Scheme);
    }

    // Pedir o Guid de uma referência que não é do cofre local é uso indevido — BLP.SEC02.
    [Fact]
    public void AsLocalVaultId_ForAnotherScheme_ShouldThrow_BLP_SEC02()
    {
        var reference = CredentialRef.Parse("infisical:/rufino/asaas");

        var ex = Assert.Throws<DomainException>(() => reference.AsLocalVaultId());

        Assert.Equal("BLP.SEC02", ex.Id);
    }

    // TryParse é a versão não-lançante para leitura de coluna possivelmente suja.
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("lixo", false)]
    [InlineData("bpv1:0198a1b2c3d47e5f8a9b0c1d2e3f4a5b", true)]
    public void TryParse_ShouldReportSuccessWithoutThrowing(string? value, bool expected)
    {
        Assert.Equal(expected, CredentialRef.TryParse(value, out _));
    }

    // Igualdade por valor: duas referências ao mesmo segredo são a mesma referência.
    [Fact]
    public void Equals_WithSameSchemeAndKey_ShouldBeTrue()
    {
        Assert.Equal(CredentialRef.ForLocalVault(SecretId), CredentialRef.ForLocalVault(SecretId));
    }
}
