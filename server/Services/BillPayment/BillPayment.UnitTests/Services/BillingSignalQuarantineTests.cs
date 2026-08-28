namespace BillPayment.UnitTests.Services;

using BillPayment.Domain.Extraction;
using BillPayment.Domain.Services;
using BillPayment.Domain.TrustedOrigins;

/// <summary>
/// A segunda exceção ao descarte: evidência forte de cobrança guarda o artefato.
/// </summary>
/// <remarks>
/// <para>
/// Antes de 2026-08-26 a única exceção era remetente cadastrado, e por isso o sistema **nunca
/// descobria emissor novo**: a conta chegava sem anexo, com o boleto atrás de um link para host
/// sem receita, e sumia sem deixar item, quarentena nem aviso. O caso medido foi uma cobrança da
/// Asaas — assunto "Olá, uma cobrança foi gerada para você".
/// </para>
/// <para>
/// A regra que sustenta tudo isto: o sinal decide <strong>guardar</strong>, nunca descartar.
/// </para>
/// </remarks>
public class BillingSignalQuarantineTests
{
    private const string BillingSubject = "Olá, uma cobrança foi gerada para você";
    private const string NeutralSubject = "72x num seminovo que vale a pena";

    private static readonly DateTime OccurredAt = new(2026, 8, 26, 12, 0, 0, DateTimeKind.Utc);

    private static ExtractionResult NothingFound => ExtractionResult.NotFound("no_instrument_in_document");

    private static TrustedOrigin Registered(TrustDecision decision) => TrustedOrigin.Register(
        Domain.SharedKernel.TenantId.From(new Guid("0195a1f0-0000-7000-8000-000000000001")),
        OriginKind.EmailAddress,
        "faturas@fornecedor.com.br",
        decision,
        Domain.SharedKernel.UserId.From(new Guid("0195a1f0-0000-7000-8000-0000000000a1")),
        note: null,
        OccurredAt);

    // TESTE ÂNCORA: remetente desconhecido, nada encontrado, mas o assunto grita cobrança —
    // o artefato fica para uma pessoa conferir em vez de sumir.
    [Fact]
    public void Decide_WhenTheSubjectSignalsBilling_ShouldQuarantineInsteadOfDropping()
        => Assert.Equal(
            CaptureTriageDecision.Quarantine,
            CaptureTriageService.Decide(NothingFound, origin: null, BillingSubject));

    // A contraprova que mantém a fila utilizável: sem sinal, descartar continua sendo o padrão.
    // Sem ela, a mudança viraria "guarde tudo" e a quarentena deixaria de ser olhada.
    [Fact]
    public void Decide_WithoutAnyBillingSignal_ShouldStillDrop()
        => Assert.Equal(
            CaptureTriageDecision.Drop,
            CaptureTriageService.Decide(NothingFound, origin: null, NeutralSubject));

    // Origem banida não ressuscita por palavra no assunto: o tenant já disse que não quer nada
    // dali, e guardar contrariaria a decisão dele em vez de protegê-lo.
    [Fact]
    public void Decide_WhenTheOriginIsBlocked_ShouldDropEvenWithABillingSubject()
        => Assert.Equal(
            CaptureTriageDecision.Drop,
            CaptureTriageService.Decide(NothingFound, Registered(TrustDecision.Blocked), BillingSubject));

    // A exceção antiga continua valendo: remetente cadastrado guarda mesmo sem sinal nenhum.
    [Fact]
    public void Decide_WhenTheSenderIsRegistered_ShouldQuarantineWithoutAnySignal()
        => Assert.Equal(
            CaptureTriageDecision.Quarantine,
            CaptureTriageService.Decide(NothingFound, Registered(TrustDecision.Trusted), NeutralSubject));

    // O sinal é substring, e por isso o ENDEREÇO do remetente não pode alimentá-lo: "conta"
    // casa dentro de "contato@" e "contabilidade@" — este último é o endereço do contador,
    // medido como origem de 72 dos 95 itens de quarentena do corpus. Aqui se prova o efeito
    // (um endereço desses vira sinal se alguém o passar); quem prova que o call site NÃO o
    // passa é `CapturedMessageRegistryTests`, na suíte de integração, com remetente
    // "faturas@fornecedor.com.br" e desfecho Drop.
    [Theory]
    [InlineData("contato@contato.autocompara.com.br")]
    [InlineData("contabilidade@escritorio.com.br")]
    public void IsPresentIn_ShouldMatchInsideUnrelatedWords_WhichIsWhySendersAreNeverFed(string address)
        => Assert.True(BillingSignal.IsPresentIn(address));

    // O portão do corpo: link para host SEM receita passa a valer quando o assunto sinaliza
    // cobrança. É o que faz a cobrança da Asaas virar item em vez de desaparecer.
    [Fact]
    public void ShouldCapture_WithAnUnknownHostAndABillingSubject_ShouldCapture()
        => Assert.True(BodyCaptureGateService.ShouldCapture(
            plainText: "clique para ver sua cobranca",
            links: [DocumentLink.TryCreate("https://www.asaas.com/i/55p08vsad5vci3g7")!],
            resolvableHosts: ["file-pdf.7az.com.br"],
            subject: BillingSubject));

    // E a contraprova de volume: propaganda com link e sem sinal continua fora.
    [Fact]
    public void ShouldCapture_WithAnUnknownHostAndNoSignal_ShouldNotCapture()
        => Assert.False(BodyCaptureGateService.ShouldCapture(
            plainText: "aproveite nossa promocao",
            links: [DocumentLink.TryCreate("https://www.loja.com.br/oferta")!],
            resolvableHosts: ["file-pdf.7az.com.br"],
            subject: NeutralSubject));

    // Sinal de cobrança SEM link nenhum não captura: não há para onde ir buscar, e o item
    // nasceria vazio. A mensagem continua visível no livro-caixa como "sem documento".
    [Fact]
    public void ShouldCapture_WithABillingSubjectButNoLinks_ShouldNotCapture()
        => Assert.False(BodyCaptureGateService.ShouldCapture(
            plainText: "sua fatura venceu",
            links: [],
            resolvableHosts: ["file-pdf.7az.com.br"],
            subject: BillingSubject));

    // O host com receita continua bastando sozinho, sem depender do assunto — é o sinal que
    // existia antes e que sustenta SABESP e condomínio.
    [Fact]
    public void ShouldCapture_WithAResolvableHost_ShouldCaptureRegardlessOfTheSubject()
        => Assert.True(BodyCaptureGateService.ShouldCapture(
            plainText: "segue o documento",
            links: [DocumentLink.TryCreate("https://file-pdf.7az.com.br/dx/abc")!],
            resolvableHosts: ["file-pdf.7az.com.br"],
            subject: NeutralSubject));
}
