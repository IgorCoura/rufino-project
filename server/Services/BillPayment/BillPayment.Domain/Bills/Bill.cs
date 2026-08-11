namespace BillPayment.Domain.Bills;

using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O boleto e toda a sua história — o Aggregate central do BC.
/// </summary>
/// <remarks>
/// O agregado sabe nascer (<see cref="Capture"/>), receber o retrato da consulta oficial
/// (<see cref="AttachLookups"/>) e ser verificado (<see cref="RecordChecks"/>). Aprovação e
/// pagamento entram nas sprints 1.5 e 3.x; a máquina de estados completa já está declarada em
/// <see cref="BillStatus"/> para que nenhuma transição seja inventada no caminho.
/// </remarks>
public sealed class Bill : AggregateRoot<BillId>
{
    private readonly List<PaymentInstrument> _instruments = [];
    private readonly List<BillCheck> _checks = [];
    private readonly List<BillLookupRecord> _lookupHistory = [];

    public TenantId TenantId { get; private set; }
    public BillStatus Status { get; private set; } = default!;

    /// <summary>Natureza do documento, derivada do código de barras — nunca informada por quem chama.</summary>
    public BillKind Kind { get; private set; } = default!;

    /// <summary>Trilho escolhido entre os instrumentos disponíveis (ADR-010).</summary>
    public PaymentRail Rail { get; private set; } = default!;

    public BillOrigin Origin { get; private set; } = default!;

    public IReadOnlyCollection<PaymentInstrument> Instruments => _instruments.AsReadOnly();

    /// <summary>
    /// Chave usada na deduplicação global. Nula quando nenhum instrumento é de uso único —
    /// caso de documento que só traz QR Pix estático, que é reutilizável por natureza.
    /// </summary>
    public string? DedupKey { get; private set; }

    /// <summary>Retrato corrente da consulta oficial do código de barras. Nulo até a consulta rodar.</summary>
    public LookupSnapshot? Lookup { get; private set; }

    /// <summary>Retrato corrente do decode do QR Pix. Nulo quando não há QR ou a consulta não resolveu.</summary>
    public PixLookupSnapshot? PixLookup { get; private set; }

    /// <summary>
    /// Pagador lido do documento. <strong>Não autoritativo</strong> — só serve para contradizer
    /// (ADR-004). Nulo é o caso majoritário por medição.
    /// </summary>
    public PartyInfo? ExtractedPayer { get; private set; }

    /// <summary>Beneficiário cadastrado resolvido pela consulta. Nulo enquanto ninguém casar.</summary>
    public PayeeId? PayeeId { get; private set; }

    /// <summary>Degrau da escada que atribuiu este boleto ao tenant. Nulo em importação manual.</summary>
    public RoutingConfidence? Routing { get; private set; }

    /// <summary>As doze verificações apuradas na última validação. Substituídas inteiras a cada rodada.</summary>
    public IReadOnlyCollection<BillCheck> Checks => _checks.AsReadOnly();

    /// <summary>Toda tentativa de consulta, em ordem. Só cresce — ver <see cref="BillLookupRecord"/>.</summary>
    public IReadOnlyList<BillLookupRecord> LookupHistory => _lookupHistory.AsReadOnly();

    /// <summary>Quem decidiu, quando e por quê. Nulo enquanto ninguém decidiu.</summary>
    public ApprovalRecord? Approval { get; private set; }

    /// <summary>Data pedida na aprovação. A data efetiva é do agendamento, na fase 3.</summary>
    public DateOnly? ScheduledFor { get; private set; }

    /// <summary>
    /// O valor que será debitado, pelo trilho que vai pagar — com o outro trilho como reserva.
    /// Nulo enquanto a consulta oficial não resolveu.
    /// </summary>
    public Money? PayableAmount => Rail == PaymentRail.Pix
        ? PixLookup?.PayableAmount ?? Lookup?.Amount
        : Lookup?.Amount ?? PixLookup?.PayableAmount;

    /// <summary>
    /// Quem receberá o dinheiro, pelo trilho que vai pagar — com o outro trilho como reserva.
    /// Nulo enquanto a consulta oficial não resolveu.
    /// </summary>
    /// <remarks>
    /// A precedência é a mesma de <see cref="PayableAmount"/>, e por um motivo: quem paga o
    /// valor é quem paga para o beneficiário. Ler o valor de um trilho e o beneficiário do
    /// outro descreveria um pagamento que não existe.
    /// </remarks>
    public LookupParty? Beneficiary => Rail == PaymentRail.Pix
        ? PixLookup?.Receiver ?? Lookup?.Beneficiary
        : Lookup?.Beneficiary ?? PixLookup?.Receiver;

    /// <summary>Instante do retrato mais recente entre os dois trilhos.</summary>
    public DateTimeOffset? LastConsultedAt
    {
        get
        {
            var bankSlip = Lookup?.ConsultedAt;
            var pix = PixLookup?.ConsultedAt;

            if (bankSlip is null)
                return pix;

            return pix is null || pix < bankSlip ? bankSlip : pix;
        }
    }

    private Bill() { }

    private Bill(BillId id) : base(id) { }

    /// <summary>
    /// Nasce um boleto a partir dos instrumentos já lidos e provados. Deriva a natureza do
    /// documento, escolhe o trilho e emite <see cref="BillCapturedDomainEvent"/>.
    /// </summary>
    /// <remarks>
    /// Os instrumentos chegam <strong>já validados</strong>: <c>DigitableLine</c> e
    /// <c>PixPayload</c> não existem em estado inválido. A conversão de texto cru para
    /// instrumento é da borda, não daqui.
    /// </remarks>
    public static Bill Capture(
        TenantId tenantId,
        IReadOnlyCollection<PaymentInstrument> instruments,
        BillOrigin origin,
        DateTime occurredAt,
        PartyInfo? extractedPayer = null,
        RoutingConfidence? routing = null)
    {
        if (instruments is null || instruments.Count == 0)
            throw BillErrors.InstrumentRequired();
        if (origin is null)
            throw BillErrors.OriginSourceKindRequired();

        var accepted = new List<PaymentInstrument>();
        foreach (var instrument in instruments)
        {
            if (instrument is null)
                throw BillErrors.InstrumentRequired();
            if (accepted.Exists(a => a.NaturalKey == instrument.NaturalKey))
                throw BillErrors.DuplicateInstrument();

            accepted.Add(instrument);
        }

        var bill = new Bill(BillId.New())
        {
            TenantId = tenantId,
            Origin = origin,
            Status = BillStatus.Captured,
            Kind = DeriveKind(accepted),
            Rail = ChooseRail(accepted),
            DedupKey = ChooseDedupKey(accepted),
            ExtractedPayer = extractedPayer,
            Routing = routing,
        };

        bill._instruments.AddRange(accepted);
        bill.CreatedAt = occurredAt;
        bill.UpdatedAt = occurredAt;

        bill.AddDomainEvent(new BillCapturedDomainEvent(
            bill.Id, tenantId, bill.Kind.Name, bill.Rail.Name, occurredAt));

        return bill;
    }

    /// <summary>
    /// Guarda o resultado das consultas oficiais: substitui os retratos correntes pelos que
    /// resolveram e registra <strong>toda</strong> tentativa no histórico.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Consulta que não resolveu não apaga o retrato anterior.</strong> Ela entra no
    /// histórico e o check <c>LookupAvailability</c> reprova — apagar deixaria o boleto sem
    /// evidência nenhuma justamente quando a rede falhou, que é quando a evidência antiga é a
    /// única que existe.
    /// </para>
    /// </remarks>
    public void AttachLookups(BillLookupResult? bankSlip, PixLookupResult? pix, DateTime occurredAt)
    {
        EnsureAcceptsValidation();

        if (bankSlip is not null)
        {
            _lookupHistory.Add(BillLookupRecord.ForBankSlip(bankSlip));
            if (bankSlip.Snapshot is not null)
                Lookup = bankSlip.Snapshot;
        }

        if (pix is not null)
        {
            _lookupHistory.Add(BillLookupRecord.ForPix(pix));
            if (pix.Snapshot is not null)
                PixLookup = pix.Snapshot;
        }

        UpdatedAt = occurredAt;
    }

    /// <summary>Vincula (ou desvincula) o beneficiário cadastrado que a consulta resolveu.</summary>
    public void ResolvePayee(PayeeId? payeeId, DateTime occurredAt)
    {
        EnsureAcceptsValidation();

        PayeeId = payeeId;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Substitui o conjunto de verificações e <strong>decide o próximo status</strong>. É o
    /// único ponto do sistema que muda status por validação.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Exige o catálogo <strong>completo</strong>: gravar um conjunto parcial deixaria pergunta
    /// sem resposta parecendo respondida, e a aprovação da sprint 1.5 confere justamente a
    /// cobertura. Verificação que não se aplica entra como <c>Skipped</c> com motivo.
    /// </para>
    /// <para>
    /// <strong>Revalidar um boleto já aprovado derruba a aprovação</strong> — para
    /// <c>AwaitingApproval</c> ou <c>Rejected</c>, conforme o resultado. O doc 03 condiciona
    /// isso a "quando o valor muda"; aqui é incondicional, de propósito: o consentimento foi
    /// dado contra um retrato que acabou de ser substituído, e reconfirmar um pagamento é
    /// barato perto de pagar o valor errado por causa de uma comparação de snapshot que
    /// silenciosamente não pegou a diferença.
    /// </para>
    /// </remarks>
    public ValidationOutcome RecordChecks(IReadOnlyCollection<CheckResult> results, DateTime occurredAt)
    {
        EnsureAcceptsValidation();

        var accepted = new List<CheckResult>();
        foreach (var result in results ?? [])
        {
            if (result is null)
                throw BillErrors.CheckTypeRequired();
            if (accepted.Exists(a => a.Type == result.Type))
                throw BillErrors.DuplicateCheckType(result.Type.Name);

            accepted.Add(result);
        }

        var missing = Enumeration.GetAll<CheckType>()
            .Where(type => !accepted.Exists(a => a.Type == type))
            .Select(type => type.Name)
            .ToList();

        if (missing.Count > 0)
            throw BillErrors.IncompleteCheckCoverage(string.Join(", ", missing));

        _checks.Clear();
        _checks.AddRange(accepted.Select(r => BillCheck.From(r, occurredAt)));

        var blocking = accepted.Where(r => r.IsBlockingFailure).ToList();
        var attention = accepted.Count(r => r.Outcome.RequiresAttention);
        var target = blocking.Count > 0 ? BillStatus.Rejected : BillStatus.AwaitingApproval;

        TransitionTo(target);
        UpdatedAt = occurredAt;

        AddDomainEvent(blocking.Count > 0
            ? new BillRejectedDomainEvent(
                Id, TenantId, blocking.Select(b => b.ReasonCode ?? b.Type.Name).ToList(), occurredAt)
            : new BillValidatedDomainEvent(Id, TenantId, attention, occurredAt));

        return ValidationOutcome.Of(target, blocking.Count, attention);
    }

    /// <summary>
    /// Um humano autoriza o pagamento. <strong>É o único caminho para o dinheiro sair</strong>
    /// (ADR-007).
    /// </summary>
    /// <remarks>
    /// A ordem das guardas é a ordem em que elas ajudam quem está na tela: primeiro "isto ainda
    /// nem foi verificado", depois "isto está reprovado", depois "a informação está velha",
    /// depois "esta data não serve", e só então "isto passa da sua alçada". Reprovar por alçada
    /// alguém que na verdade estava olhando um boleto bloqueado seria a mensagem errada.
    /// </remarks>
    public void Approve(
        UserId approvedBy,
        DateOnly scheduleFor,
        string? note,
        ApprovalPolicy policy,
        DateOnly today,
        DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(policy);
        EnsureDecidable(ApprovalDecision.Approved, BillStatus.Approved);

        EnsureChecksAreComplete();
        EnsureNoBlockingFailure();
        EnsureSnapshotIsFresh(policy, occurredAt);
        EnsureScheduleDateIsAllowed(scheduleFor, today);

        if (PayableAmount is { } amount && !policy.Allows(amount))
            throw BillErrors.AboveApprovalLimit(amount.Amount);

        Approval = ApprovalRecord.Approve(approvedBy, occurredAt, note);
        ScheduledFor = scheduleFor;
        Status = BillStatus.Approved;
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillApprovedDomainEvent(Id, TenantId, approvedBy, scheduleFor, occurredAt));
    }

    /// <summary>
    /// O humano recusa o boleto. O motivo é obrigatório — é o desvio, e é dele que alguém vai
    /// querer entender a razão depois.
    /// </summary>
    public void Deny(UserId deniedBy, string reason, DateTime occurredAt)
    {
        EnsureDecidable(ApprovalDecision.Denied, BillStatus.Denied);

        Approval = ApprovalRecord.Deny(deniedBy, occurredAt, reason);
        Status = BillStatus.Denied;
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillDeniedDomainEvent(Id, TenantId, deniedBy, occurredAt));
    }

    /// <summary>
    /// Tira o boleto do fluxo. Diferente de recusar: alcança documento que nem chegou a ser
    /// verificado, e libera a chave natural para o documento poder ser reimportado.
    /// </summary>
    public void Cancel(UserId cancelledBy, string reason, DateTime occurredAt)
    {
        EnsureDecidable(ApprovalDecision.Cancelled, BillStatus.Cancelled);

        Approval = ApprovalRecord.Cancel(cancelledBy, occurredAt, reason);
        Status = BillStatus.Cancelled;
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillCancelledDomainEvent(Id, TenantId, cancelledBy, occurredAt));
    }

    /// <summary>O retrato da consulta já passou do prazo de validade neste instante?</summary>
    public bool IsLookupStaleAt(DateTimeOffset instant, TimeSpan maxAge)
        => LastConsultedAt is { } consultedAt && instant - consultedAt > maxAge;

    private void EnsureDecidable(ApprovalDecision decision, BillStatus target)
    {
        if (Status.IsTerminal || !Status.CanTransitionTo(target))
            throw BillErrors.DecisionNotAllowedInStatus(decision.Name, Status.Name);
    }

    // Invariante 3: acrescentar um check novo invalida aprovações pendentes até a revalidação,
    // e é o comportamento desejado — é uma pergunta que ninguém respondeu para aquele boleto.
    private void EnsureChecksAreComplete()
    {
        if (Enumeration.GetAll<CheckType>().Any(type => !_checks.Exists(c => c.Type == type)))
            throw BillErrors.ChecksNotEvaluated();
    }

    private void EnsureNoBlockingFailure()
    {
        var blocking = _checks.FindAll(c => c.IsBlockingFailure);
        if (blocking.Count > 0)
            throw BillErrors.BlockedByFailedChecks(
                string.Join(", ", blocking.Select(c => c.ReasonCode ?? c.Type.Name)));
    }

    private void EnsureSnapshotIsFresh(ApprovalPolicy policy, DateTime occurredAt)
    {
        if (LastConsultedAt is not { } consultedAt)
            return;

        var age = new DateTimeOffset(occurredAt, TimeSpan.Zero) - consultedAt;
        if (age > policy.MaxSnapshotAge)
            throw BillErrors.StaleLookupSnapshot((int)age.TotalHours);
    }

    private void EnsureScheduleDateIsAllowed(DateOnly scheduleFor, DateOnly today)
    {
        if (scheduleFor < today)
            throw BillErrors.ScheduleDateInThePast(scheduleFor, today);

        if (Lookup?.MinimumScheduleDate is { } minimum && scheduleFor < minimum)
            throw BillErrors.ScheduleDateBeforeProviderMinimum(scheduleFor, minimum);
    }

    private void EnsureAcceptsValidation()
    {
        if (Status.IsTerminal)
            throw BillErrors.TerminalStatus(Status.Name);
        if (!Status.AcceptsValidation)
            throw BillErrors.ValidationNotAllowedInStatus(Status.Name);
    }

    private void TransitionTo(BillStatus target)
    {
        if (Status == target)
            return;
        if (!Status.CanTransitionTo(target))
            throw BillErrors.ValidationNotAllowedInStatus(Status.Name);

        Status = target;
    }

    /// <summary>
    /// A natureza vem do código de barras. Documento que só traz QR Pix é tratado como
    /// cobrança: não há campo de convênio para dizer o contrário, e é a leitura que mantém os
    /// checks mais exigentes ligados em vez de afrouxá-los por omissão.
    /// </summary>
    private static BillKind DeriveKind(List<PaymentInstrument> instruments)
    {
        var kinds = instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Select(i => i.DigitableLine.Kind)
            .Distinct()
            .ToList();

        if (kinds.Count > 1)
            throw BillErrors.MixedBillKinds();

        return kinds.Count == 1 ? kinds[0] : BillKind.BankSlip;
    }

    /// <summary>Havendo QR Pix, paga-se por Pix (ADR-010). A precedência mora no Smart Enum.</summary>
    private static PaymentRail ChooseRail(List<PaymentInstrument> instruments)
        => instruments
            .Select(i => i.Kind.Rail)
            .OrderBy(r => r.Precedence)
            .First();

    /// <summary>
    /// Só instrumento de uso único vira chave de deduplicação. QR Pix estático é reutilizável
    /// e usá-lo bloquearia a conta do mês seguinte por causa da do mês anterior.
    /// Código de barras vence o QR dinâmico por ser a chave mais estável entre emissores.
    /// </summary>
    private static string? ChooseDedupKey(List<PaymentInstrument> instruments)
        => instruments
            .Where(i => i.IsSingleUse)
            .OrderBy(i => i.Kind == PaymentInstrumentKind.Barcode ? 0 : 1)
            .Select(i => i.NaturalKey)
            .FirstOrDefault();
}
