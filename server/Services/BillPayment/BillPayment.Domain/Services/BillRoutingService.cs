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

    /// <summary>
    /// Abriu com um prefixo curto do documento do tenant, e o documento não está impresso na
    /// página: atribui, mas sem a força do degrau 0 (2026-09-10).
    /// </summary>
    public const string REASON_PASSWORD_DERIVED_SHORT_PREFIX = "password_derived_short_prefix";

    /// <summary>Documento fiscal do tenant impresso no artefato (degrau 2).</summary>
    public const string REASON_PAYER_TAX_ID = "payer_tax_id";

    /// <summary>Beneficiário cadastrado só por este tenant (degrau 4).</summary>
    public const string REASON_EXCLUSIVE_PAYEE = "exclusive_payee";

    /// <summary>Documento sob rótulo de pagador, e não é de ninguém deste tenant.</summary>
    public const string REASON_PAYER_IS_ANOTHER = "payer_is_another";

    /// <summary>Nada no artefato disse de quem ele é (degrau 5).</summary>
    public const string REASON_PAYER_NOT_IDENTIFIED = "payer_not_identified";

    /// <summary>
    /// A consulta oficial do Pix dinâmico devolveu um documento do tenant como pagador (degrau 1).
    /// </summary>
    public const string REASON_OFFICIAL_PIX_PAYER = "official_pix_payer";

    /// <summary>
    /// A consulta oficial do Pix dinâmico devolveu como pagador um documento que não é do tenant.
    /// </summary>
    public const string REASON_OFFICIAL_PAYER_IS_ANOTHER = "official_payer_is_another";

    /// <summary>O número da conta cadastrado numa expectativa do tenant está no artefato (degrau 3).</summary>
    public const string REASON_ACCOUNT_REFERENCE = "account_reference";

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
    /// <param name="officialPayerTaxId">
    /// O pagador que a consulta oficial do QR Pix <strong>dinâmico</strong> devolveu inteiro e com
    /// DV válido (<c>PixLookupSnapshot.RegisteredPayerTaxId</c>, ADR-025). Nulo quando não houve
    /// Pix dinâmico, o tenant não tem conta vinculada, ou a consulta não respondeu — e aí a escada
    /// segue exatamente como seria sem ele.
    /// </param>
    /// <param name="accountReferenceMatch">
    /// A expectativa cujo número de conta foi achado no artefato, pelo
    /// <c>AccountReferenceMatchingService</c> (ADR-026). Nulo quando nenhuma — ou mais de uma — casou.
    /// </param>
    public static RoutingDecision Route(
        ExtractionResult extraction,
        PayerProfile? profile,
        IReadOnlyCollection<TaxId> exclusivePayeeTaxIds,
        TaxId? officialPayerTaxId = null,
        AccountReferenceMatch? accountReferenceMatch = null)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        // Degrau 1 negativo OFICIAL — vale antes de tudo (decisão do usuário, 2026-09-14). A
        // cobrança registrada é emitida CONTRA um documento, e quem o afirma é o PSP do trilho que
        // paga, não uma leitura de PDF: se ele não é de ninguém deste tenant, o boleto é de outra
        // pessoa, e o desfecho é o mesmo do rótulo de pagador — descarte, sem ninguém ficar
        // sabendo de quem era (ADR-008).
        if (officialPayerTaxId is not null && (profile is null || !Owns(profile, officialPayerTaxId)))
            return RoutingDecision.Foreign(REASON_OFFICIAL_PAYER_IS_ANOTHER, officialPayerTaxId);

        // O documento do tenant impresso no artefato. Serve aos degraus 0 e 2: no 0 como
        // identificação de quem é o pagador, no 2 como a própria prova de propriedade.
        var own = OwnParty(extraction, profile);

        // Degrau 0 — a senha é prova de propriedade, não conveniência. O emissor a derivou do
        // documento do pagador, e as candidatas saíram do PayerProfile DESTE tenant: se abriu,
        // o emissor endereçou o documento a ele. A senha vazia não conta e por isso não chega
        // aqui — o parser devolve UnlockedBy nulo quando o PDF só tinha owner password.
        //
        // O documento vai junto quando ele TAMBÉM está impresso na página. Sem isso, o degrau
        // mais forte da escada era o único que deixava o pagador em branco — e o check PayerMatch
        // respondia "o documento não traz o pagador" sobre um artefato que trazia (medido em
        // 2026-08-26, no boleto BBZ-COND). Nulo continua sendo desfecho válido: PDF que abre por
        // senha sem repetir o documento no corpo existe, e ali não há o que informar.
        // Prefixo CURTO é a exceção, e ela não é teórica: desde 2026-09-10 a derivação tenta 3 e 4
        // dígitos, e com três dígitos o espaço é de mil valores — "abriu" passa a poder ser
        // coincidência. Com o documento do tenant TAMBÉM impresso na página as duas evidências
        // juntas seguem conclusivas; sozinho, o prefixo curto não decide aqui e a escada continua,
        // para que o degrau 1 negativo ainda possa dizer "o pagador é outro". A atribuição fraca
        // espera no fim, no lugar da quarentena.
        var shortPrefixOnly = PasswordCandidate.IsShortPrefixProof(extraction.UnlockedBy) && own is null;

        if (!string.IsNullOrEmpty(extraction.UnlockedBy) && !shortPrefixOnly)
            return RoutingDecision.Promote(RoutingConfidence.Strong, REASON_PASSWORD_DERIVED, own?.TaxId);

        // Degrau 2 — o documento fiscal do tenant impresso no artefato. Cobre 93,3% do corpus.
        if (own is not null)
            return RoutingDecision.Promote(RoutingConfidence.Strong, REASON_PAYER_TAX_ID, own.TaxId);

        var labelled = extraction.Parties.FirstOrDefault(p => p.UnderPayerLabel);

        // Degrau 1 — o pagador oficial do Pix dinâmico é do tenant. Fica ABAIXO do degrau 2 só na
        // ordem do código: os dois promovem com a mesma força, e o 2 devolve o documento impresso,
        // que é o mesmo. Com um pagador de OUTRA pessoa sob rótulo no documento, o desfecho é o de
        // sempre — descarte: documento contradizendo a consulta oficial é anomalia (ADR-025), e
        // falhar fechado é o que preserva o isolamento.
        if (officialPayerTaxId is not null && labelled is null)
            return RoutingDecision.Promote(RoutingConfidence.Strong, REASON_OFFICIAL_PIX_PAYER, officialPayerTaxId);

        // Degrau 2 negativo — só com rótulo. Sem ele, o número tanto pode ser o pagador quanto a
        // concessionária, e a quarentena cega tiraria do usuário a chance de reivindicar.
        if (labelled is not null)
            return RoutingDecision.Foreign(REASON_PAYER_IS_ANOTHER, labelled.TaxId);

        // Degrau 3 — o número da conta que o tenant cadastrou está no artefato (ADR-026). Fica
        // DEPOIS dos negativos, de propósito: o número foi informado pelo tenant, e nunca desfaz uma
        // prova de que o boleto é de outra pessoa. Forte só dentro do código de barras, com número
        // longo; no texto, ou com número curto, a coincidência é plausível e a confiança é fraca.
        if (accountReferenceMatch is not null)
            return RoutingDecision.Promote(accountReferenceMatch.Confidence, REASON_ACCOUNT_REFERENCE);

        // Degrau 4 — beneficiário exclusivo. Nunca sobrepõe o degrau 1 negativo (doc 07): ele
        // reduz fila, não decide sozinho, e por isso a confiança é Weak e a aprovação humana
        // continua obrigatória.
        if (exclusivePayeeTaxIds.Count > 0
            && extraction.Parties.Any(p => exclusivePayeeTaxIds.Contains(p.TaxId)))
        {
            return RoutingDecision.Promote(RoutingConfidence.Weak, REASON_EXCLUSIVE_PAYEE);
        }

        // O prefixo curto que não decidiu no degrau 0 vale aqui: a senha saiu do cadastro DESTE
        // tenant e abriu, o que é mais do que nada — só não é conclusivo. Entra como Weak, depois
        // de o degrau 1 negativo ter tido a sua vez, e antes da quarentena: mandar para a fila de
        // reivindicação um documento que abriu com dado do próprio tenant seria pedir ao usuário
        // que reivindicasse o que o sistema já sabe ser provavelmente dele.
        if (shortPrefixOnly)
            return RoutingDecision.Promote(RoutingConfidence.Weak, REASON_PASSWORD_DERIVED_SHORT_PREFIX);

        // Degrau 5 — fila de reivindicação. Nenhum boleto vira Bill sem rota determinada; não
        // existe atribuição por default ao dono da fonte.
        return RoutingDecision.Unrouted(REASON_PAYER_NOT_IDENTIFIED);
    }

    /// <summary>
    /// O pagador que o documento identifica, para quem <strong>já sabe de quem o boleto é</strong>
    /// — a importação manual, onde a pessoa afirma a propriedade e a escada não tem o que decidir.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É o mesmo critério de <see cref="Route"/>, e é por isso que mora aqui.</strong>
    /// Documento do tenant impresso (degrau 2) ou, na falta dele, o que veio sob rótulo de pagador
    /// (degrau 2 negativo) — a mesma regra que preenche o <c>RoutingDecision.PayerTaxId</c>. O
    /// handler não pode repeti-la à mão: seria a mesma decisão sobre dois agregados escrita em dois
    /// lugares, e a cópia é que sairia do lugar quando a régua mudasse.
    /// </para>
    /// <para>
    /// <strong>Candidato sem rótulo e sem casar com o cadastro NÃO vale.</strong> Num boleto o CNPJ
    /// do beneficiário está impresso ao lado do CNPJ do pagador, e devolver "o primeiro que
    /// apareceu" faria o da concessionária virar pagador — o check <c>PayerMatch</c> então o leria
    /// como contradição ao cadastro e <strong>bloquearia</strong> um boleto legítimo (ADR-004).
    /// </para>
    /// <para>
    /// <strong>Devolver o pagador de OUTRA pessoa é desfecho desejado</strong>, não descuido: o
    /// documento que nomeia outro pagador sob rótulo tem de chegar ao check para bloquear. Quem
    /// importou à mão vê o motivo; a importação em si não é recusada aqui, porque recusar é
    /// decisão da validação, não desta leitura.
    /// </para>
    /// </remarks>
    public static TaxId? IdentifyPayer(ExtractionResult extraction, PayerProfile? profile)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        return OwnParty(extraction, profile)?.TaxId
            ?? extraction.Parties.FirstOrDefault(p => p.UnderPayerLabel)?.TaxId;
    }

    /// <summary>O documento do próprio tenant impresso no artefato, se houver.</summary>
    private static PartyCandidate? OwnParty(ExtractionResult extraction, PayerProfile? profile)
        => profile is null
            ? null
            : extraction.Parties.FirstOrDefault(p => Owns(profile, p.TaxId));

    /// <summary>
    /// A raiz do CNPJ entra só quando o tenant pediu: filial cujo boleto chega sem cadastro
    /// prévio é o caso que ela cobre, e ligá-la por default faria duas empresas do mesmo grupo
    /// econômico — que são tenants distintos — passarem uma pela outra.
    /// </summary>
    private static bool Owns(PayerProfile profile, TaxId candidate)
        => profile.Owns(candidate) || (profile.MatchByCnpjRoot && profile.OwnsByCnpjRoot(candidate));
}
