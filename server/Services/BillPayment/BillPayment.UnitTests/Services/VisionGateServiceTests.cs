namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Services;
using BillPayment.Domain.TrustedOrigins;
using BillPayment.UnitTests.TrustedOrigins.Mothers;

/// <summary>
/// O portão que decide se vale pagar o extrator de visão.
/// </summary>
/// <remarks>
/// A regra que estes testes protegem é a distinção que custou caro descobrir: <strong>palavra-chave
/// decide GASTAR, nunca descartar</strong>. Errar para mais custa centavos; errar para menos custa
/// um boleto não pago.
/// </remarks>
public class VisionGateServiceTests
{
    // Remetente cadastrado basta sozinho: é gente de quem o tenant declarou esperar conta, e o
    // parser falhando ali significa provavelmente falha do parser, não "não era boleto".
    [Fact]
    public void ShouldAttempt_WhenSenderIsKnown_ShouldSpendEvenWithoutAnyKeyword()
    {
        var origin = TrustedOriginMother.Register();

        Assert.True(VisionGateService.ShouldAttempt(origin, "assunto qualquer", "anexo.pdf"));
    }

    // Remetente explicitamente banido não ganha gasto — o tenant já disse que não quer.
    [Fact]
    public void ShouldAttempt_WhenSenderIsBlocked_ShouldFallBackToTheKeywords()
    {
        var blocked = TrustedOriginMother.Register(decision: TrustDecision.Blocked);

        Assert.False(VisionGateService.ShouldAttempt(blocked, "assunto qualquer", "anexo.pdf"));
        Assert.True(VisionGateService.ShouldAttempt(blocked, "seu boleto chegou", "anexo.pdf"));
    }

    // Assunto de cobrança vale gasto mesmo de remetente desconhecido — inclusive quando não
    // contém "boleto" nem "conta", que foi o caso medido na caixa real.
    [Theory]
    [InlineData("Sua fatura chegou")]
    [InlineData("FGTS 07/2026 RUFINO E RBC2")]
    [InlineData("Boleto próximo do vencimento")]
    [InlineData("[SECONCI-SP] - BOLETOS A VENCER - 25082026")]
    [InlineData("Enel - Conta por email")]
    [InlineData("2ª via disponível")]
    [InlineData("Aviso de vencimento")]
    [InlineData("CONTRIBUIÇÕES SINDICATO PARA PAGAMENTO")]
    public void ShouldAttempt_WithABillingSignalInTheSubject_ShouldSpend(string subject)
        => Assert.True(VisionGateService.ShouldAttempt(origin: null, subject, "anexo.pdf"));

    // O nome do arquivo também conta: o assunto às vezes é só "ENC:" e o sinal está no anexo.
    [Fact]
    public void ShouldAttempt_WithABillingSignalInTheFileName_ShouldSpend()
        => Assert.True(VisionGateService.ShouldAttempt(origin: null, "ENC:", "boleto-agosto.pdf"));

    // Sem remetente conhecido e sem sinal nenhum, não gasta. Foi o que manteve o custo
    // proporcional: 250 dos 404 anexos medidos não tinham sinal algum de cobrança.
    [Theory]
    [InlineData("DOCUMENTOS RUFINO - JOSÉ VAGNER", "cnh.pdf")]
    [InlineData("APRESENTAÇÃO EUROBRAS CONDUTORES", "catalogo.pdf")]
    [InlineData("assinou o documento FICHA DE REGISTRO", "ficha.pdf")]
    public void ShouldAttempt_WithoutAnySignal_ShouldNotSpend(string subject, string artifactKey)
        => Assert.False(VisionGateService.ShouldAttempt(origin: null, subject, artifactKey));

    // Acento e caixa não podem decidir gasto: o assunto real chega em MAIÚSCULAS, com e sem acento.
    [Theory]
    [InlineData("CONTRIBUICAO SINDICAL")]
    [InlineData("contribuição sindical")]
    [InlineData("CONDOMÍNIO EDIFÍCIO")]
    [InlineData("condominio edificio")]
    public void ShouldAttempt_ShouldIgnoreCaseAndAccents(string subject)
        => Assert.True(VisionGateService.ShouldAttempt(origin: null, subject, artifactKey: null));

    // Assunto e nome ausentes não estouram — mensagem sem assunto existe.
    [Fact]
    public void ShouldAttempt_WithNothingAtAll_ShouldNotSpend()
        => Assert.False(VisionGateService.ShouldAttempt(origin: null, subject: null, artifactKey: null));
}
