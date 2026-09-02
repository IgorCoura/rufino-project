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
    /// <summary>
    /// Teto da espera entre tentativas de leitura por IA. A espera dobra a cada falha e para aqui.
    /// </summary>
    /// <remarks>
    /// Meia hora é o mesmo teto da fila de captura: além disso a espera passa a ser maior que a
    /// folga que um boleto costuma ter, e o retrato chegaria tarde demais para servir à decisão.
    /// </remarks>
    private static readonly TimeSpan MAX_READING_RETRY_DELAY = TimeSpan.FromMinutes(30);

    /// <summary>Quantas duplicações de espera antes de o expoente parar de crescer.</summary>
    private const int MAX_READING_BACKOFF_SHIFT = 10;

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
    /// O retrato da leitura por IA do documento e do corpo do e-mail. Nulo até a extração rodar.
    /// Enriquecimento e contradição — nunca decisão de pagamento (ADR-011).
    /// </summary>
    public DocumentReading? Reading { get; private set; }

    /// <summary>
    /// Em que pé está a leitura por IA. <strong>Nunca bloqueia o boleto</strong> — ver
    /// <see cref="ReadingStatus"/>.
    /// </summary>
    public ReadingStatus ReadingState { get; private set; } = ReadingStatus.NotApplicable;

    /// <summary>Quantas vezes a fila já tentou ler este documento.</summary>
    public int ReadingAttempts { get; private set; }

    /// <summary>
    /// Até quando a análise é de um worker — e, depois de uma falha passageira, a partir de
    /// quando vale tentar de novo.
    /// </summary>
    /// <remarks>
    /// Uma coluna só, porque é a mesma pergunta: já posso mexer nisto? Dois campos divergiriam.
    /// É a mesma escolha do aluguel da fila de captura.
    /// </remarks>
    public DateTime? ReadingLeaseExpiresAt { get; private set; }

    /// <summary>
    /// O retrato chegou DEPOIS de alguém já ter decidido sobre o boleto.
    /// </summary>
    /// <remarks>
    /// Marca que a verificação não foi refeita: revalidar um boleto aprovado derruba a aprovação
    /// incondicionalmente, e desfazer em silêncio uma decisão humana por causa de um
    /// enriquecimento de fundo seria a pior troca possível.
    /// </remarks>
    public bool ReadingArrivedAfterDecision { get; private set; }

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

    /// <summary>
    /// A classificação de risco derivada da última validação — Seguro, Atenção ou Perigo.
    /// Nula até a primeira rodada de verificações (ADR-015).
    /// </summary>
    public RiskLevel? Risk { get; private set; }

    /// <summary>Toda tentativa de consulta, em ordem. Só cresce — ver <see cref="BillLookupRecord"/>.</summary>
    public IReadOnlyList<BillLookupRecord> LookupHistory => _lookupHistory.AsReadOnly();

    /// <summary>Quem decidiu, quando e por quê. Nulo enquanto ninguém decidiu.</summary>
    public ApprovalRecord? Approval { get; private set; }

    /// <summary>
    /// Vencimento consolidado do boleto, <strong>materializado</strong> para a listagem ordenar
    /// e filtrar em SQL — jsonb não serve a esse propósito.
    /// </summary>
    /// <remarks>
    /// A precedência é a mesma de <see cref="PayableAmount"/> e <see cref="Beneficiary"/>:
    /// consulta oficial do trilho que paga primeiro, o outro trilho como reserva, e a data
    /// embutida na linha digitável por último. Recomputado em cada ponto que muda uma das
    /// fontes — nunca atribuído de fora.
    /// </remarks>
    public DateOnly? DueDate { get; private set; }

    /// <summary>Data pedida na aprovação. A data efetiva é do agendamento, na fase 3.</summary>
    public DateOnly? ScheduledFor { get; private set; }

    /// <summary>
    /// A ordem de pagamento desta aprovação. Referência por id, nunca navegação (ADR-002) —
    /// e nula até o provedor aceitar a submissão.
    /// </summary>
    public PaymentOrders.PaymentOrderId? PaymentOrderId { get; private set; }

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

    /// <summary>
    /// O valor com que a ordem de pagamento é submetida: o oficial do trilho que paga, e na
    /// falta dele o impresso no instrumento (protegido por DV/CRC) — a mesma reserva que a
    /// alçada de aprovação usa. Nulo só quando nenhuma fonte tem valor (QR estático sem campo 54).
    /// </summary>
    public Money? AmountForPayment => PayableAmount ?? DeclaredAmount;

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
        RoutingConfidence? routing = null,
        DocumentReading? reading = null)
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
            Reading = reading,

            ReadingState = InitialReadingState(reading, origin),
        };

        bill._instruments.AddRange(accepted);
        bill.RecomputeDueDate();
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

        RecomputeDueDate();
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Anexa (ou substitui) o retrato da leitura por IA. Sem guarda de status de propósito:
    /// é metadado do documento, e enriquecer um boleto já decidido só melhora o histórico.
    /// </summary>
    public void AttachReading(DocumentReading reading, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(reading);

        Reading = reading;
        ReadingState = ReadingStatus.Done;
        ReadingLeaseExpiresAt = null;

        // O retrato que chega depois da decisão não refaz a verificação — quem marca isso é o
        // chamador, que é quem sabe em que situação o boleto estava.
        RecomputeDueDate();
        UpdatedAt = occurredAt;
    }

    /// <summary>Põe (ou repõe) o boleto na fila de análise por IA.</summary>
    /// <remarks>
    /// Zera as tentativas porque reenfileirar é decisão de quem opera, e decisão de gente ganha
    /// orçamento novo — mesma regra do <c>Reopen</c> da quarentena. Boleto sem documento guardado
    /// não entra: não há o que ler, e a fila giraria nele para sempre.
    /// </remarks>
    public bool QueueReading(DateTime occurredAt)
    {
        if (string.IsNullOrEmpty(Origin.StorageKey))
            return false;

        ReadingState = ReadingStatus.Queued;
        ReadingAttempts = 0;
        ReadingLeaseExpiresAt = null;
        UpdatedAt = occurredAt;
        return true;
    }

    /// <summary>Marca que um worker assumiu a análise até o instante informado.</summary>
    /// <remarks>
    /// A tentativa conta na SAÍDA da fila, não no fim do processamento: um worker que morre antes
    /// de escrever qualquer coisa deixaria o boleto voltando para sempre. Mesma lição da captura.
    /// </remarks>
    public void LeaseReading(DateTime expiresAt, DateTime occurredAt)
    {
        ReadingAttempts++;
        ReadingLeaseExpiresAt = expiresAt;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// A análise falhou. Devolve <c>true</c> quando o boleto desistiu de vez.
    /// </summary>
    /// <remarks>
    /// <strong>Permanente desiste na hora; passageira ganha as tentativas.</strong> É a mesma
    /// classificação da fila de captura, e errar para o lado de "passageira" é o lado seguro: no
    /// máximo se gasta o teto, ao passo que tratar indisponibilidade como definitiva deixaria o
    /// boleto sem retrato por causa de um 503.
    /// </remarks>
    public bool RecordReadingFailure(
        bool permanent,
        int maxAttempts,
        TimeSpan baseRetryDelay,
        DateTime occurredAt)
    {
        if (!permanent && ReadingAttempts < maxAttempts)
        {
            ReadingLeaseExpiresAt = NextReadingAttemptAt(baseRetryDelay, occurredAt);
            UpdatedAt = occurredAt;
            return false;
        }

        ReadingState = ReadingStatus.Unavailable;
        ReadingLeaseExpiresAt = null;
        UpdatedAt = occurredAt;
        return true;
    }

    /// <summary>Registra que o retrato chegou depois de o boleto já ter sido decidido.</summary>
    public void MarkReadingArrivedAfterDecision(DateTime occurredAt)
    {
        ReadingArrivedAfterDecision = true;
        UpdatedAt = occurredAt;
    }

    /// <summary>A verificação ainda pode ser refeita sem desfazer decisão de ninguém.</summary>
    /// <remarks>
    /// <c>AcceptsValidation</c> inclui <c>Approved</c>, e revalidar ali derruba a aprovação. Para
    /// um enriquecimento de fundo isso é inaceitável — quem decide é gente, e um retrato que
    /// chegou atrasado não pode desfazer a decisão em silêncio.
    /// </remarks>
    public bool AcceptsSilentRevalidation
        => Status == BillStatus.Captured || Status == BillStatus.AwaitingApproval;

    /// <summary>
    /// Em que pé a análise nasce.
    /// </summary>
    /// <remarks>
    /// A leitura que a captura já obteve vem <strong>de graça</strong>: a chamada ao extrator
    /// aconteceu para resolver o instrumento e o retrato veio junto. Enfileirar é só para o
    /// boleto que ficou SEM retrato — o provedor falhou, ou o degrau de visão nem foi acionado.
    /// </remarks>
    private static ReadingStatus InitialReadingState(DocumentReading? reading, BillOrigin origin)
    {
        if (reading is not null)
            return ReadingStatus.Done;

        return string.IsNullOrEmpty(origin.StorageKey)
            ? ReadingStatus.NotApplicable
            : ReadingStatus.Queued;
    }

    /// <summary>A espera dobra a cada falha, com o mesmo teto da fila de captura.</summary>
    private DateTime NextReadingAttemptAt(TimeSpan baseRetryDelay, DateTime occurredAt)
    {
        var shift = Math.Min(Math.Max(ReadingAttempts - 1, 0), MAX_READING_BACKOFF_SHIFT);
        var scaled = baseRetryDelay.Ticks * (1L << shift);

        return occurredAt.AddTicks(Math.Min(scaled, MAX_READING_RETRY_DELAY.Ticks));
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

        // ADR-015: a validação CLASSIFICA, nunca rejeita. A flag mede a PIOR evidência
        // encontrada: declaração explícita do tenant (blacklist, origem bloqueada) é Extremo
        // Perigo; contradição entre fontes ou conferência central falhando é Perigo;
        // inconclusivo ou aviso é Atenção; e todo boleto validado aguarda a decisão humana.
        var blocking = accepted.Where(r => r.IsBlockingFailure).ToList();
        var attention = accepted.Count(r => r.Outcome.RequiresAttention);

        if (blocking.Exists(r => r.IsCriticalFailure))
            Risk = RiskLevel.ExtremeDanger;
        else if (blocking.Count > 0)
            Risk = RiskLevel.Danger;
        else if (attention > 0)
            Risk = RiskLevel.Attention;
        else
            Risk = RiskLevel.Safe;

        TransitionTo(BillStatus.AwaitingApproval);
        UpdatedAt = occurredAt;

        AddDomainEvent(new BillValidatedDomainEvent(Id, TenantId, attention, occurredAt));

        return ValidationOutcome.Of(BillStatus.AwaitingApproval, blocking.Count, attention);
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
        RiskLevel clearance,
        DateOnly today,
        DateTime occurredAt,
        bool acknowledgeRisk = false,
        bool acknowledgeImmediateExecution = false)
    {
        ArgumentNullException.ThrowIfNull(policy);
        EnsureDecidable(ApprovalDecision.Approved, BillStatus.Approved);

        EnsureChecksAreComplete();
        // A alçada vem ANTES do aceite: dizer "marque o assumo o risco" a quem nem pode aprovar
        // este nível seria a mensagem errada.
        EnsureRiskWithinClearance(clearance);
        EnsureRiskIsAcknowledged(acknowledgeRisk);
        EnsureSnapshotIsFresh(policy, occurredAt);
        EnsureScheduleDateIsAllowed(scheduleFor, today);
        EnsureImmediateExecutionIsAcknowledged(today, acknowledgeImmediateExecution);
        EnsureWithinApprovalLimit(policy);

        Approval = ApprovalRecord.Approve(approvedBy, occurredAt, note, Risk);
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

    /// <summary>
    /// O provedor aceitou a ordem: <c>Approved → Scheduled</c>. Daqui em diante o boleto é
    /// <strong>espelho</strong> da <c>PaymentOrder</c> (ADR-002) — estes métodos de reflexo só
    /// são chamados por handler de evento dela, nunca por escrita direta de um caso de uso.
    /// </summary>
    /// <remarks>
    /// <c>ScheduledFor</c> passa a dizer a data <em>efetiva</em>: a pedida vive na trilha de
    /// aprovação e na ordem, e a tela mostra as duas quando diferem (ADR-017 desliza datas).
    /// </remarks>
    public void LinkPaymentOrder(
        PaymentOrders.PaymentOrderId paymentOrderId,
        DateOnly effectiveScheduleDate,
        DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Scheduled);

        PaymentOrderId = paymentOrderId;
        ScheduledFor = effectiveScheduleDate;
        Status = BillStatus.Scheduled;
        UpdatedAt = occurredAt;
    }

    /// <summary>Reflexo de <c>PaymentOrderPaid</c>: <c>Scheduled → Paid</c>. Terminal.</summary>
    public void MarkPaid(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Paid);

        Status = BillStatus.Paid;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Reflexo de <c>PaymentOrderFailed</c>: <c>Scheduled → Failed</c>. O que fazer com a falha
    /// — reabrir, pagar à mão — é decisão de gente, e os motivos vivem na ordem.
    /// </summary>
    public void MarkFailed(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Failed);

        Status = BillStatus.Failed;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Reflexo de <c>PaymentOrderCancelled</c> depois de agendado: <c>Scheduled → Cancelled</c>.
    /// Não toca a trilha de aprovação — quem cancelou e por quê vive na ordem.
    /// </summary>
    public void MarkScheduleCancelled(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Cancelled);

        Status = BillStatus.Cancelled;
        UpdatedAt = occurredAt;
    }

    /// <summary>
    /// Devolve um boleto de pagamento falhado à fila de decisão. A nova tentativa é uma nova
    /// aprovação e uma nova ordem (ADR-002) — por isso o vínculo com a ordem anterior é limpo.
    /// </summary>
    public void ReopenForApproval(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.AwaitingApproval);

        PaymentOrderId = null;
        ScheduledFor = null;
        Status = BillStatus.AwaitingApproval;
        UpdatedAt = occurredAt;
    }

    private void EnsurePaymentTransition(BillStatus target)
    {
        if (Status.IsTerminal || !Status.CanTransitionTo(target))
            throw BillErrors.PaymentTransitionNotAllowed(Status.Name, target.Name);
    }

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

    // A alçada de risco é hierárquica e comparada contra o risco ATUAL do boleto — é o que
    // fecha a corrida "uma revalidação concorrente subiu o risco depois de a borda resolver a
    // alçada". Quem resolve QUAL alçada a pessoa tem é a borda (escopos UMA); a regra vive aqui.
    private void EnsureRiskWithinClearance(RiskLevel clearance)
    {
        if (clearance is null)
            throw BillErrors.ApprovalClearanceRequired();
        if (Risk is null || Risk.IsCoveredBy(clearance))
            return;

        throw BillErrors.ApprovalAboveRiskClearance(Risk.Name, clearance.Name);
    }

    // ADR-015: Perigo e Extremo Perigo não bloqueiam — exigem que o aprovador assuma o risco
    // explicitamente, e a trilha grava o nível assumido. Sem o aceite, a recusa lista os motivos.
    private void EnsureRiskIsAcknowledged(bool acknowledgeRisk)
    {
        if ((Risk != RiskLevel.Danger && Risk != RiskLevel.ExtremeDanger) || acknowledgeRisk)
            return;

        var reasons = _checks.FindAll(c => c.IsBlockingFailure);
        throw BillErrors.DangerRequiresAcknowledgment(
            Risk!.Name,
            string.Join(", ", reasons.Select(c => c.ReasonCode ?? c.Type.Name)));
    }

    private void EnsureSnapshotIsFresh(ApprovalPolicy policy, DateTime occurredAt)
    {
        if (LastConsultedAt is not { } consultedAt)
            return;

        var age = new DateTimeOffset(occurredAt, TimeSpan.Zero) - consultedAt;
        if (age > policy.MaxSnapshotAge)
            throw BillErrors.StaleLookupSnapshot((int)age.TotalHours);
    }

    // O valor impresso no próprio instrumento — o da linha digitável é protegido por DV, o do
    // BR Code pelo CRC. Reserva da alçada para quando a consulta oficial não resolveu.
    private Money? DeclaredAmount
        => _instruments
            .Where(i => i.DeclaredAmount is not null)
            .Select(i => i.DeclaredAmount!)
            .MaxBy(m => m.Amount);

    // A alçada vale com o valor oficial e, na falta dele, com o valor do instrumento. Até
    // 2026-08-28 o teto só era conferido quando havia consulta oficial: com o provedor fora,
    // o boleto ia a Perigo e bastava assumir o risco para aprovar qualquer valor — a alçada
    // sumia exatamente no caso menos verificado. Sem valor nenhum e com teto, não há como
    // aplicar a alçada, e a resposta certa é recusar, não presumir.
    private void EnsureWithinApprovalLimit(ApprovalPolicy policy)
    {
        var amount = PayableAmount ?? DeclaredAmount;

        if (amount is null)
        {
            if (policy.Limit is not null)
                throw BillErrors.ApprovalLimitRequiresAmount();

            return;
        }

        if (!policy.Allows(amount))
            throw BillErrors.AboveApprovalLimit(amount.Amount);
    }

    // ADR-017: boleto vencido não é pago em silêncio. O provedor processa conta vencida
    // IMEDIATAMENTE, sem agendamento — ou seja, sem a janela de reação que a política das 24h
    // existe para garantir. Aprovar um vencido exige o aceite explícito, gravado na trilha
    // como o aceite de risco. A fila reconfere: se o vencimento passar DEPOIS da aprovação,
    // a ordem para em "aguardando confirmação" em vez de executar.
    private void EnsureImmediateExecutionIsAcknowledged(DateOnly today, bool acknowledged)
    {
        if (DueDate is not { } due || due >= today || acknowledged)
            return;

        throw BillErrors.OverdueRequiresImmediateAcknowledgment(due);
    }

    private void EnsureScheduleDateIsAllowed(DateOnly scheduleFor, DateOnly today)
    {
        if (scheduleFor < today)
            throw BillErrors.ScheduleDateInThePast(scheduleFor, today);

        if (Lookup?.MinimumScheduleDate is { } minimum && scheduleFor < minimum)
            throw BillErrors.ScheduleDateBeforeProviderMinimum(scheduleFor, minimum);
    }

    private void RecomputeDueDate()
    {
        var official = Rail == PaymentRail.Pix
            ? PixLookup?.DueDate ?? Lookup?.DueDate
            : Lookup?.DueDate ?? PixLookup?.DueDate;

        // A leitura por IA é a ÚLTIMA reserva, atrás da linha digitável: a data embutida é
        // protegida por DV, a lida é transcrição de modelo. Só QR estático sem consulta chega nela.
        DueDate = official ?? EmbeddedDueDate() ?? Reading?.DueDate;
    }

    // Consultar o Kind antes é obrigatório: acessar a linha digitável de um instrumento Pix
    // lança BLP.INS03, por desenho.
    private DateOnly? EmbeddedDueDate()
    {
        var embedded = _instruments
            .Where(i => i.Kind == PaymentInstrumentKind.Barcode)
            .Select(i => i.DigitableLine.DueDate)
            .FirstOrDefault(d => d is not null);

        return embedded is { } date ? DateOnly.FromDateTime(date) : null;
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
