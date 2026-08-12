namespace BillPayment.IntegrationTests.Extraction;

using BillPayment.Infra.Extraction;

/// <summary>
/// A leitura do documento fiscal impresso no artefato — o insumo do degrau 1 do roteamento.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Esta varredura faz o OPOSTO da de linha digitável, de propósito.</strong> O
/// <c>CandidateScanner</c> gera todas as janelas e deixa o DV reprovar, porque a linha tem
/// quatro dígitos verificadores e um acerto por acaso é improvável. O CNPJ tem dois, e um
/// código de barras de 44 posições oferece trinta e uma janelas — medido em 2026-08-12 sobre
/// 714 documentos reais: <strong>a regra deslizante fabricaria um CNPJ aparentemente válido
/// dentro do código de barras em 46,9% deles</strong>.
/// </para>
/// <para>
/// O texto destes testes é o que o PdfPig entrega de um boleto de verdade — blocos concatenados
/// sem espaço, com o rótulo colado no valor.
/// </para>
/// </remarks>
public sealed class TaxIdScannerTests
{
    private const string ValidCnpj = "11222333000181";
    private const string BankSlipBarcode = "34191234546789012345767890123457314880000061507";

    // TESTE ÂNCORA. Um código de barras é uma sequência longa de dígitos e NÃO pode produzir
    // documento fiscal nenhum — se produzisse, o número inventado poderia cair ao lado de um
    // rótulo de pagador e mandar para a quarentena cega uma conta legítima.
    [Fact]
    public void Scan_OverADigitableLine_ShouldNotFabricateAnyTaxId()
    {
        Assert.Empty(TaxIdScanner.Scan($"LinhaDigitavel{BankSlipBarcode}Vencimento31/01/2025"));
    }

    // O formato que o emissor imprime, com a formatação do documento — e é a letra do rótulo
    // seguinte que encerra a sequência.
    [Fact]
    public void Scan_WithAFormattedCnpj_ShouldReadIt()
    {
        var found = Assert.Single(TaxIdScanner.Scan("CPF/CNPJ11.222.333/0001-81Registro2506564"));

        Assert.Equal(ValidCnpj, found.TaxId.Value);
    }

    // O rótulo de pagador é o que autoriza a afirmação negativa da escada, e ele vem ANTES do
    // número no layout de todo boleto medido.
    [Fact]
    public void Scan_WhenTheDocumentFollowsAPayerLabel_ShouldFlagIt()
    {
        var found = Assert.Single(TaxIdScanner.Scan(
            "PagadorRUFINO EMPREITEIRA LTDACPF/CNPJ11.222.333/0001-81Registro25"));

        Assert.True(found.UnderPayerLabel);
    }

    // E o rótulo do OUTRO lado desempata: num boleto os dois blocos são vizinhos, e sem isto o
    // CNPJ do credor seria lido como o do devedor por simples proximidade.
    [Fact]
    public void Scan_WhenTheNearestLabelIsThePayees_ShouldNotFlagIt()
    {
        var found = Assert.Single(TaxIdScanner.Scan(
            "PagadorFULANOBeneficiarioCONCESSIONARIACNPJ11.222.333/0001-81Vencimento"));

        Assert.False(found.UnderPayerLabel);
    }

    // O documento sem rótulo nenhum por perto continua sendo lido — ele só não autoriza a
    // recusa. Atribuir depende de casar com o cadastro, não de rótulo.
    [Fact]
    public void Scan_WithoutAnyLabel_ShouldStillReadTheDocument()
    {
        var found = Assert.Single(TaxIdScanner.Scan("Rodape11.222.333/0001-81Fim"));

        Assert.False(found.UnderPayerLabel);
    }

    // Dígito verificador errado não vira candidato: a varredura propõe e o VO dispõe (ADR-011).
    [Fact]
    public void Scan_WithAnInvalidCheckDigit_ShouldReadNothing()
    {
        Assert.Empty(TaxIdScanner.Scan("CPF/CNPJ11.222.333/0001-82Registro"));
    }

    // Pagador e beneficiário no mesmo documento saem os dois, cada um com seu rótulo — quem
    // decide o papel de cada número é o BillRoutingService, contra o cadastro.
    [Fact]
    public void Scan_WithBothParties_ShouldReturnEachWithItsOwnLabel()
    {
        var found = TaxIdScanner.Scan(
            "BeneficiarioCONCESSIONARIACNPJ11.444.777/0001-61PagadorEMPRESACNPJ11.222.333/0001-81Valor");

        Assert.Equal(2, found.Count);
        Assert.True(Assert.Single(found, f => f.TaxId.Value == ValidCnpj).UnderPayerLabel);
        Assert.False(Assert.Single(found, f => f.TaxId.Value == "11444777000161").UnderPayerLabel);
    }

    // CPF é lido pelo mesmo caminho — tenant pessoa física não tem tratamento à parte (doc 07).
    [Fact]
    public void Scan_WithACpf_ShouldReadIt()
    {
        var found = Assert.Single(TaxIdScanner.Scan("CNPJ/CPF:082.441.818-26Rua"));

        Assert.Equal("08244181826", found.TaxId.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Boleto sem documento fiscal nenhum")]
    public void Scan_WithoutAnyDocument_ShouldReturnEmpty(string text)
    {
        Assert.Empty(TaxIdScanner.Scan(text));
    }
}
