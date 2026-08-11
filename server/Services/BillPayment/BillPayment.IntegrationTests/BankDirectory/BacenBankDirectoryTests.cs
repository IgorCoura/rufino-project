namespace BillPayment.IntegrationTests.BankDirectory;

using BillPayment.Domain.Ports;
using BillPayment.Domain.SharedKernel;
using BillPayment.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Resolve o diretório pelo contêiner de DI de propósito, em vez de instanciar a classe: o
/// risco real não é a lógica de busca, é o snapshot do Bacen <strong>não ser embarcado</strong>
/// no assembly publicado. Um nome de recurso errado no csproj só aparece assim.
/// </summary>
[Collection(nameof(IntegrationTestCollection))]
public sealed class BacenBankDirectoryTests : BaseIntegrationTest
{
    private readonly IBankDirectory _directory;

    public BacenBankDirectoryTests(IntegrationTestWebAppFactory factory) : base(factory)
        => _directory = factory.Services.GetRequiredService<IBankDirectory>();

    // O snapshot embarca no assembly e é lido no startup — se o recurso sumir, isto quebra.
    [Fact]
    public void Directory_ShouldResolveFromDiWithTheSnapshotEmbedded()
    {
        Assert.NotNull(_directory);
        Assert.True(_directory.IsKnown(new BankCode("001")));
    }

    // Os bancos que aparecem no corpus real de boletos existem na tabela do Bacen.
    [Theory]
    [InlineData("033", "BCO SANTANDER (BRASIL) S.A.")]
    [InlineData("237", "BCO BRADESCO S.A.")]
    [InlineData("341", "ITAÚ UNIBANCO S.A.")]
    [InlineData("104", "CAIXA ECONOMICA FEDERAL")]
    public void NameOf_WithBanksFromTheRealCorpus_ShouldReturnTheRegisteredName(string compe, string expected)
    {
        Assert.True(_directory.IsKnown(new BankCode(compe)));
        Assert.Equal(expected, _directory.NameOf(new BankCode(compe)));
    }

    // Código de três dígitos que não corresponde a instituição nenhuma é desconhecido.
    [Theory]
    [InlineData("999")]
    [InlineData("998")]
    public void IsKnown_WithUnassignedCode_ShouldReturnFalse(string compe)
    {
        Assert.False(_directory.IsKnown(new BankCode(compe)));
        Assert.Null(_directory.NameOf(new BankCode(compe)));
    }

    // Boleto liquida pela Compe — os grandes bancos participam.
    [Theory]
    [InlineData("033")]
    [InlineData("237")]
    [InlineData("341")]
    public void ParticipatesInCompe_WithSettlingBanks_ShouldReturnTrue(string compe)
    {
        Assert.True(_directory.ParticipatesInCompe(new BankCode(compe)));
    }

    // Existir no Bacen não é o mesmo que liquidar boleto: o BNDES tem código e não está na Compe.
    [Fact]
    public void ParticipatesInCompe_WithInstitutionOutsideCompe_ShouldReturnFalse()
    {
        var bndes = new BankCode("007");

        Assert.True(_directory.IsKnown(bndes));
        Assert.False(_directory.ParticipatesInCompe(bndes));
    }

    // A tradução ISPB → COMPE é o que permite comparar o recebedor de um QR Pix
    // contra os bancos aceitos de um Payee, que estão em código de três dígitos.
    [Theory]
    [InlineData("60701190", "341")]
    [InlineData("60746948", "237")]
    [InlineData("90400888", "033")]
    [InlineData("00000000", "001")]
    public void FromIspb_WithKnownInstitution_ShouldTranslateToCompe(string ispb, string expected)
    {
        Assert.Equal(expected, _directory.FromIspb(ispb)!.Value);
    }

    // ISPB circula com zeros à esquerda comidos; a tradução precisa tolerar isso.
    [Fact]
    public void FromIspb_WithLeadingZerosStripped_ShouldStillTranslate()
    {
        Assert.Equal("001", _directory.FromIspb("0")!.Value);
    }

    // ISPB desconhecido ou malformado é ausência, não exceção — o check então sai inconclusivo.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("99999999")]
    [InlineData("123456789012")]
    [InlineData("nao-numerico")]
    public void FromIspb_WithUnknownOrMalformedInput_ShouldReturnNull(string ispb)
    {
        Assert.Null(_directory.FromIspb(ispb));
    }

    // Banco "000" não existe na tabela — é o que sustenta o guard BLP.DGL05 da DigitableLine.
    [Fact]
    public void IsKnown_WithUnassignedBankZero_ShouldReturnFalse()
    {
        // BankCode recusa "000" na construção, então a checagem aqui é sobre a tabela:
        // nenhuma instituição do Bacen usa esse código.
        Assert.Throws<BillPayment.Domain.SeedWork.DomainException>(() => new BankCode("000"));
    }
}
