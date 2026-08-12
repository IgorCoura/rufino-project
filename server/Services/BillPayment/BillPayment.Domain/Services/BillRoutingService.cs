namespace BillPayment.Domain.Services;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Extraction;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>Onde um artefato com boleto válido para depois da escada de roteamento.</summary>
public sealed class RoutingOutcome : Enumeration
{
    /// <summary>É deste tenant. Vira <c>Bill</c>.</summary>
    public static readonly RoutingOutcome Promote = new(1, nameof(Promote));

    /// <summary>O documento diz, sob rótulo, que o pagador é outro. Não vira boleto e não expõe valor.</summary>
    public static readonly RoutingOutcome Foreign = new(2, nameof(Foreign));

    /// <summary>Nada resolveu. Vai para a fila de reivindicação do dono da fonte.</summary>
    public static readonly RoutingOutcome Unrouted = new(3, nameof(Unrouted));

    private RoutingOutcome(int id, string name) : base(id, name) { }
}

/// <summary>O desfecho da escada, com o degrau que o produziu.</summary>
public sealed class RoutingDecision : ValueObject
{
    public RoutingOutcome Outcome { get; }

    /// <summary>Por qual degrau. Preenchido apenas em <see cref="RoutingOutcome.Promote"/>.</summary>
    public RoutingConfidence? Confidence { get; }

    /// <summary>Código estável do motivo, para métrica e para a fila operacional.</summary>
    public string Reason { get; }

    /// <summary>
    /// O documento fiscal que a escada reconheceu como sendo o do pagador — e <strong>só</strong>
    /// quando ela o reconheceu de fato.
    /// </summary>
    /// <remarks>
    /// Vira o <c>Bill.ExtractedPayer</c>, que alimenta o check <c>PayerMatch</c>. Preenchê-lo com
    /// um candidato qualquer do artefato faria o CNPJ da concessionária ser lido como pagador e
    /// <strong>bloquearia o boleto</strong> por contradizer o cadastro — o oposto do que o campo
    /// existe para fazer. Por isso ele só é preenchido quando o número casou com o perfil (degrau
    /// 1) ou quando veio sob rótulo de pagador.
    /// </remarks>
    public TaxId? PayerTaxId { get; }

    private RoutingDecision(
        RoutingOutcome outcome,
        RoutingConfidence? confidence,
        string reason,
        TaxId? payerTaxId)
    {
        Outcome = outcome;
        Confidence = confidence;
        Reason = reason;
        PayerTaxId = payerTaxId;
    }

    internal static RoutingDecision Promote(
        RoutingConfidence confidence, string reason, TaxId? payerTaxId = null)
        => new(RoutingOutcome.Promote, confidence, reason, payerTaxId);

    internal static RoutingDecision Foreign(string reason, TaxId? payerTaxId = null)
        => new(RoutingOutcome.Foreign, confidence: null, reason, payerTaxId);

    internal static RoutingDecision Unrouted(string reason)
        => new(RoutingOutcome.Unrouted, confidence: null, reason, payerTaxId: null);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Outcome;
        yield return Confidence;
        yield return Reason;
        yield return PayerTaxId;
    }
}

/// <summary>
/// De quem é este boleto — a escada de roteamento do doc 07, corrigida pela medição.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É Domain Service porque cruza três Aggregates</strong> — o artefato capturado, o
/// perfil fiscal do tenant e os beneficiários que ele cadastrou. Estático e puro, como
/// <c>BillValidationService</c> e <c>CaptureTriageService</c>: sem estado, sem I/O, sem relógio.
/// Quem carrega os agregados é o handler; aqui só entram valores.
/// </para>
/// <para>
/// <strong>A escada mudou de forma depois de medida (2026-08-12, 714 documentos, 14 meses).</strong>
/// O doc 07 previa que o degrau 1 (documento fiscal do pagador) cobriria ~38% e que o grosso
/// viria de uma <c>RoutingRule</c> aprendida por <c>(beneficiário, referência de conta)</c>. Os
/// dois números estavam errados:
/// </para>
/// <list type="number">
/// <item>
/// <strong>O degrau 1 cobre 93,3%</strong>, não 38%. É o cavalo de batalha da escada.
/// </item>
/// <item>
/// <strong>Não existe referência de conta estável no código de barras.</strong> O que se repete
/// entre meses é a agência/conta do <em>beneficiário</em>; o que varia é o nosso número. Medido:
/// dois pagadores diferentes do mesmo emissor têm campo livre com as <em>mesmas</em> posições
/// estáveis (DESPACON 19/25 idênticas, SECONCI 17/25 idênticas). Uma regra com essa chave casaria
/// com o boleto dos dois e roteria o do outro tenant — a falha exata que o ADR-008 existe para
/// impedir. Por isso <c>RoutingRule</c> <strong>não foi criada</strong>, e o aprendizado passou a
/// ser a vinculação do <c>Payee</c> ao tenant, que é chave que de fato distingue.
/// </item>
/// </list>
/// <para>
/// <strong>A assimetria entre afirmar e negar é deliberada.</strong> Atribuir exige casar com o
/// cadastro do próprio tenant, o que é seguro por construção. Negar — dizer "isto é de outra
/// pessoa" — exige rótulo de pagador ao lado do número, porque sem rótulo não há como distinguir
/// o CNPJ do pagador do CNPJ da concessionária, e um engano aqui manda para a quarentena cega
/// (onde não se pode reivindicar) uma conta que era do usuário.
/// </para>
/// </remarks>
public static class BillRoutingService
{
    /// <summary>Abriu o PDF com senha derivada do documento do tenant (degrau 0, doc 09).</summary>
    public const string REASON_PASSWORD_DERIVED = "password_derived";

    /// <summary>Documento fiscal do tenant impresso no artefato (degrau 1).</summary>
    public const string REASON_PAYER_TAX_ID = "payer_tax_id";

    /// <summary>Beneficiário cadastrado só por este tenant (degrau 3).</summary>
    public const string REASON_EXCLUSIVE_PAYEE = "exclusive_payee";

    /// <summary>Documento sob rótulo de pagador, e não é de ninguém deste tenant.</summary>
    public const string REASON_PAYER_IS_ANOTHER = "payer_is_another";

    /// <summary>Nada no artefato disse de quem ele é (degrau 4).</summary>
    public const string REASON_PAYER_NOT_IDENTIFIED = "payer_not_identified";

    /// <param name="extraction">
    /// O que a cascata leu. Traz os documentos fiscais do artefato — do pagador <em>e</em> do
    /// beneficiário — e o campo que derivou a senha, quando houve.
    /// </param>
    /// <param name="profile">
    /// O perfil fiscal do tenant da fonte. Nulo é estado válido: sem cadastro não há contra o quê
    /// comparar, e a escada cai para os degraus que não dependem dele.
    /// </param>
    /// <param name="exclusivePayeeTaxIds">
    /// Documentos de beneficiários que <strong>só este tenant</strong> cadastrou. A exclusividade
    /// é apurada pelo handler, com a travessia de tenant autorizada — que devolve <c>bool</c> e
    /// nada mais (ADR-008).
    /// </param>
    public static RoutingDecision Route(
        ExtractionResult extraction,
        PayerProfile? profile,
        IReadOnlyCollection<TaxId> exclusivePayeeTaxIds)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        // Degrau 0 — a senha é prova de propriedade, não conveniência. O emissor a derivou do
        // documento do pagador, e as candidatas saíram do PayerProfile DESTE tenant: se abriu,
        // o emissor endereçou o documento a ele. A senha vazia não conta e por isso não chega
        // aqui — o parser devolve UnlockedBy nulo quando o PDF só tinha owner password.
        if (!string.IsNullOrEmpty(extraction.UnlockedBy))
            return RoutingDecision.Promote(RoutingConfidence.Strong, REASON_PASSWORD_DERIVED);

        // Degrau 1 — o documento fiscal do tenant impresso no artefato. Cobre 93,3% do corpus.
        var own = profile is null
            ? null
            : extraction.Parties.FirstOrDefault(p => Owns(profile, p.TaxId));

        if (own is not null)
            return RoutingDecision.Promote(RoutingConfidence.Strong, REASON_PAYER_TAX_ID, own.TaxId);

        // Degrau 1 negativo — só com rótulo. Sem ele, o número tanto pode ser o pagador quanto a
        // concessionária, e a quarentena cega tiraria do usuário a chance de reivindicar.
        var labelled = extraction.Parties.FirstOrDefault(p => p.UnderPayerLabel);

        if (labelled is not null)
            return RoutingDecision.Foreign(REASON_PAYER_IS_ANOTHER, labelled.TaxId);

        // Degrau 3 — beneficiário exclusivo. Nunca sobrepõe o degrau 1 negativo (doc 07): ele
        // reduz fila, não decide sozinho, e por isso a confiança é Weak e a aprovação humana
        // continua obrigatória.
        if (exclusivePayeeTaxIds.Count > 0
            && extraction.Parties.Any(p => exclusivePayeeTaxIds.Contains(p.TaxId)))
        {
            return RoutingDecision.Promote(RoutingConfidence.Weak, REASON_EXCLUSIVE_PAYEE);
        }

        // Degrau 4 — fila de reivindicação. Nenhum boleto vira Bill sem rota determinada; não
        // existe atribuição por default ao dono da fonte.
        return RoutingDecision.Unrouted(REASON_PAYER_NOT_IDENTIFIED);
    }

    /// <summary>
    /// A raiz do CNPJ entra só quando o tenant pediu: filial cujo boleto chega sem cadastro
    /// prévio é o caso que ela cobre, e ligá-la por default faria duas empresas do mesmo grupo
    /// econômico — que são tenants distintos — passarem uma pela outra.
    /// </summary>
    private static bool Owns(PayerProfile profile, TaxId candidate)
        => profile.Owns(candidate) || (profile.MatchByCnpjRoot && profile.OwnsByCnpjRoot(candidate));
}
