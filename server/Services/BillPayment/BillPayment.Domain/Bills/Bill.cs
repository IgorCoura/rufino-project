namespace BillPayment.Domain.Bills;

using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Payees;
using BillPayment.Domain.PaymentOrders;
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
    private readonly List<BillHistoryEntry> _history = [];

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

    /// <summary>
    /// A decisão <strong>vigente</strong> — quem decidiu, quando e por quê. Nulo enquanto ninguém
    /// decidiu.
    /// </summary>
    /// <remarks>
    /// É o que as guardas consultam, e por isso guarda só a última: a pergunta que elas fazem é
    /// "esta aprovação vale?", não "o que já houve aqui". A narrativa completa vive em
    /// <see cref="History"/>, e as duas não competem — uma decide, a outra explica.
    /// </remarks>
    public ApprovalRecord? Approval { get; private set; }

    /// <summary>
    /// A trilha do boleto, da captura ao desfecho: o que foi feito, quando e por quem.
    /// </summary>
    /// <remarks>
    /// <strong>Só cresce, e só por dentro.</strong> Cada método rico que muda o estado acrescenta
    /// a sua linha na mesma transação — nenhum handler escreve aqui. É isso que impede a trilha
    /// de divergir da máquina de estados: não existe caminho que mude o status sem registrar.
    /// </remarks>
    public IReadOnlyList<BillHistoryEntry> History => _history.AsReadOnly();

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

        // A primeira linha da trilha não tem "antes" — é a única com FromStatus nulo. Autor é o
        // sistema mesmo quando a origem é importação manual: quem importa não decide nada sobre
        // o pagamento, e confundir importador com aprovador seria mentira de auditoria.
        bill._history.Add(BillHistoryEntry.Record(
            BillAction.Captured, BillActionOrigin.System, null, BillStatus.Captured,
            null, null, occurredAt, origin.SourceKind.Name));

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
        // encontrada, e cada verificação diz sozinha quanto pesa — a régua vive em
        // RiskLevel.Of, não aqui. ADR-020 (2026-09-08) endureceu essa régua: falha advisory,
        // aviso e inconclusivo passaram de Atenção para Perigo, e o teto de Atenção ficou
        // reservado a quem foi marcado Notice (expectativa, prazo e nome do beneficiário).
        var blocking = accepted.Where(r => r.IsBlockingFailure).ToList();
        var attention = accepted.Count(r => r.Outcome.RequiresAttention);

        var risk = accepted.Aggregate(RiskLevel.Safe, (worst, r) => worst.Worst(r.RiskContribution));
        Risk = risk;

        var from = Status;

        TransitionTo(BillStatus.AwaitingApproval);
        UpdatedAt = occurredAt;

        // A trilha registra TODA validação, inclusive a que não muda o status: revalidar é um
        // ato que alguém pediu e cujo resultado explica por que a aprovação anterior caiu.
        _history.Add(BillHistoryEntry.Record(
            BillAction.Validated, BillActionOrigin.System, from, Status, null, null, occurredAt,
            $"Risco {Risk?.Name ?? "não classificado"}; {blocking.Count} bloqueio(s), {attention} atenção"));

        AddDomainEvent(new BillValidatedDomainEvent(Id, TenantId, attention, occurredAt));

        return ValidationOutcome.Of(BillStatus.AwaitingApproval, risk, blocking.Count, attention);
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
        string? note,
        ApprovalPolicy policy,
        RiskLevel clearance,
        DateTime occurredAt,
        bool acknowledgeRisk = false,
        string? approverName = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        EnsureDecidable(ApprovalDecision.Approved, BillStatus.Approved);

        EnsureChecksAreComplete();
        // A alçada vem ANTES do aceite: dizer "marque o assumo o risco" a quem nem pode aprovar
        // este nível seria a mensagem errada.
        EnsureRiskWithinClearance(clearance);
        EnsureRiskIsAcknowledged(acknowledgeRisk);
        EnsureSnapshotIsFresh(policy, occurredAt);
        EnsureWithinApprovalLimit(policy);

        var from = Status;

        Approval = ApprovalRecord.Approve(approvedBy, occurredAt, note, Risk);
        Status = BillStatus.Approved;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Approved, BillActionOrigin.User, from, Status, approvedBy, approverName,
            occurredAt, note));

        AddDomainEvent(new BillApprovedDomainEvent(Id, TenantId, approvedBy, occurredAt));
    }

    /// <summary>
    /// Um humano escolhe a data e manda executar. <strong>É o ato que move dinheiro</strong>
    /// (ADR-018) — a aprovação autoriza, o agendamento executa.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>O frescor do retrato é reconferido aqui</strong>, e não é redundância com a
    /// aprovação: separar os dois atos abriu uma janela entre eles, e é neste método que o
    /// dinheiro anda. Aprovar ontem contra um retrato de ontem é legítimo; agendar hoje contra
    /// aquele mesmo retrato não é.
    /// </para>
    /// <para>
    /// As guardas de data vivem só aqui, porque só aqui existe data: <c>Approve</c> não conhece
    /// mais o calendário.
    /// </para>
    /// </remarks>
    /// <param name="sameDay">
    /// O veredito do <c>PaymentSchedulingService</c> sobre hoje ainda servir como data de
    /// pagamento neste instante (ADR-021). Só é consultado quando <paramref name="scheduleFor"/>
    /// é hoje — nenhuma data futura depende da hora.
    /// </param>
    public void Schedule(
        UserId requestedBy,
        DateOnly scheduleFor,
        ApprovalPolicy policy,
        DateOnly today,
        SameDayScheduling sameDay,
        DateTime occurredAt,
        bool acknowledgeImmediateExecution = false,
        string? requesterName = null)
    {
        ArgumentNullException.ThrowIfNull(sameDay);

        ArgumentNullException.ThrowIfNull(policy);

        if (Status != BillStatus.Approved)
            throw BillErrors.SchedulingRequiresApproval(Status.Name);

        // Duas provas de "já está agendado", porque elas cobrem instantes diferentes: a data
        // aparece assim que alguém agenda, e o vínculo com a ordem só depois que o provedor
        // aceita. Conferir só o vínculo deixaria passar o duplo clique.
        if (ScheduledFor is not null || PaymentOrderId is not null)
            throw BillErrors.AlreadyScheduled();

        EnsureSnapshotIsFresh(policy, occurredAt);
        EnsureScheduleDateIsAllowed(scheduleFor, today, sameDay);
        EnsureImmediateExecutionIsAcknowledged(today, acknowledgeImmediateExecution);

        ScheduledFor = scheduleFor;
        UpdatedAt = occurredAt;

        // O evento carrega o aceite COMO FOI DADO: exigido (boleto vencido na tela) e marcado.
        // Um "true" solto num boleto não vencido não é consentimento — a caixa nem apareceu.
        var immediateExecutionAcknowledged =
            acknowledgeImmediateExecution && DueDate is { } dueDate && dueDate < today;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Scheduled, BillActionOrigin.User, Status, Status, requestedBy, requesterName,
            occurredAt,
            $"Pagamento pedido para {scheduleFor:dd/MM/yyyy}"));

        AddDomainEvent(new BillSchedulingRequestedDomainEvent(
            Id, TenantId, requestedBy, scheduleFor, immediateExecutionAcknowledged, occurredAt));
    }

    /// <summary>
    /// O humano recusa o boleto. O motivo é obrigatório — é o desvio, e é dele que alguém vai
    /// querer entender a razão depois.
    /// </summary>
    public void Deny(UserId deniedBy, string reason, DateTime occurredAt, string? denierName = null)
    {
        EnsureDecidable(ApprovalDecision.Denied, BillStatus.Denied);

        var from = Status;

        Approval = ApprovalRecord.Deny(deniedBy, occurredAt, reason);
        Status = BillStatus.Denied;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Denied, BillActionOrigin.User, from, Status, deniedBy, denierName,
            occurredAt, reason));

        AddDomainEvent(new BillDeniedDomainEvent(Id, TenantId, deniedBy, occurredAt));
    }

    /// <summary>
    /// Tira o boleto do fluxo. Diferente de recusar: alcança documento que nem chegou a ser
    /// verificado, e libera a chave natural para o documento poder ser reimportado.
    /// </summary>
    public void Cancel(UserId cancelledBy, string reason, DateTime occurredAt, string? cancellerName = null)
    {
        EnsureDecidable(ApprovalDecision.Cancelled, BillStatus.Cancelled);

        var from = Status;

        Approval = ApprovalRecord.Cancel(cancelledBy, occurredAt, reason);
        Status = BillStatus.Cancelled;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Cancelled, BillActionOrigin.User, from, Status, cancelledBy, cancellerName,
            occurredAt, reason));

        AddDomainEvent(new BillCancelledDomainEvent(Id, TenantId, cancelledBy, occurredAt));
    }

    /// <summary>
    /// Uma pessoa desfaz a própria recusa ou o próprio cancelamento: o boleto volta à fila de
    /// decisão e as verificações rodam de novo (ADR-018).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>É a única saída de um estado terminal, e ela não passa pela matriz de
    /// transições</strong> — passar tornaria <c>Denied</c> e <c>Cancelled</c> não-terminais para
    /// todo mundo, e a terminalidade é o que impede um reflexo de pagamento atrasado de
    /// ressuscitar um boleto morto. Aqui a exceção é nomeada, tem alçada própria na borda, e
    /// vale só para decisão de gente: <c>Paid</c> não reverte.
    /// </para>
    /// <para>
    /// <strong>A trilha de aprovação anterior fica</strong>, como em <c>UnschedulePayment</c>:
    /// é história, e a próxima decisão grava a sua por cima. O que se limpa é o que descreve
    /// execução — data e vínculo com ordem — porque nenhum dos dois sobrevive à volta.
    /// </para>
    /// <para>
    /// Duas pré-condições que <strong>não</strong> cabem aqui ficam no caso de uso, por
    /// dependerem de consulta: a chave natural pode ter sido reocupada por uma reimportação, e
    /// pode haver ordem viva no provedor. Ambas exigem repositório, e o agregado não consulta.
    /// </para>
    /// </remarks>
    public void UndoDecision(UserId undoneBy, string reason, DateTime occurredAt, string? undoerName = null)
    {
        if (!Status.CanBeUndone)
            throw BillErrors.DecisionCannotBeUndone(Status.Name);

        var from = Status;

        PaymentOrderId = null;
        ScheduledFor = null;
        Status = BillStatus.AwaitingApproval;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Reverted, BillActionOrigin.User, from, Status, undoneBy, undoerName,
            occurredAt, reason));

        AddDomainEvent(new BillDecisionUndoneDomainEvent(Id, TenantId, undoneBy, occurredAt));
    }

    /// <summary>Este boleto está num estado do qual a decisão pode ser desfeita?</summary>
    /// <remarks>
    /// Existe para o caso de uso <strong>não consultar o banco à toa</strong> num pedido que o
    /// agregado vai recusar de qualquer forma — e, sobretudo, para ele não recusar com o erro
    /// errado. As duas pré-condições da reversão exigem consulta, e a primeira delas pergunta se
    /// a chave natural foi reocupada: num boleto que ainda espera decisão, quem ocupa a chave é
    /// ELE MESMO, então a resposta era <c>BLP.BIL02</c> ("boleto já capturado") mandando procurar
    /// uma duplicata que não existe, no lugar do <c>BLP.BIL38</c> que explica a recusa.
    /// </remarks>
    public bool AcceptsDecisionUndo => Status.CanBeUndone;

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

        var from = Status;

        PaymentOrderId = paymentOrderId;
        ScheduledFor = effectiveScheduleDate;
        Status = BillStatus.Scheduled;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.HandedToProvider, BillActionOrigin.Provider, from, Status, null, null, occurredAt,
            $"Pagamento aceito pelo provedor para {effectiveScheduleDate:dd/MM/yyyy}"));
    }

    /// <summary>Reflexo de <c>PaymentOrderPaid</c>: <c>Scheduled → Paid</c>. Terminal.</summary>
    public void MarkPaid(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Paid);

        var from = Status;

        Status = BillStatus.Paid;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Paid, BillActionOrigin.Provider, from, Status, null, null, occurredAt));
    }

    /// <summary>
    /// Reflexo de <c>PaymentOrderFailed</c>: <c>Scheduled → Failed</c>. O que fazer com a falha
    /// — reabrir, pagar à mão — é decisão de gente, e os motivos vivem na ordem.
    /// </summary>
    public void MarkFailed(DateTime occurredAt)
    {
        EnsurePaymentTransition(BillStatus.Failed);

        var from = Status;

        Status = BillStatus.Failed;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.PaymentFailed, BillActionOrigin.Provider, from, Status, null, null, occurredAt));
    }

    /// <summary>
    /// O agendamento morreu e a aprovação sobreviveu: o boleto volta a <c>Approved</c>, sem data
    /// e sem vínculo com ordem, pronto para ser agendado de novo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Substituiu <c>MarkScheduleCancelled</c> em 2026-09-08 (ADR-018).</strong> Até
    /// então, ordem cancelada levava o boleto a <c>Cancelled</c> — terminal. Quem cancelava um
    /// agendamento só para trocar a data matava o boleto e a aprovação junto, e precisava
    /// reimportar o documento. O que o cancelamento desfaz é a execução; a autorização humana
    /// continua valendo, e é dela que o ADR-007 cuida.
    /// </para>
    /// <para>
    /// Atende os dois instantes em que a ordem pode morrer: em <c>Draft</c>, antes de o boleto
    /// virar <c>Scheduled</c> (e aí o estado já é <c>Approved</c> — a chamada é no-op de estado,
    /// mas registra na trilha e limpa a data), e depois de agendado, vindo do provedor. Um só
    /// método porque o significado é um só.
    /// </para>
    /// <para>
    /// <strong>A trilha de aprovação fica.</strong> É história, e a próxima decisão grava a sua.
    /// </para>
    /// </remarks>
    /// <param name="origin">
    /// De onde partiu o cancelamento. <strong>É o parâmetro que impede a trilha de mentir</strong>:
    /// o mesmo caminho de código atende ao cancelamento pedido no nosso app e ao que alguém fez
    /// no painel do provedor, e sem distingui-los a auditoria não responde "quem cancelou isto?".
    /// </param>
    /// <param name="note">
    /// O detalhe que só quem chamou conhece — o motivo do provedor, por exemplo. Ausente, cada
    /// origem escreve a sua frase padrão.
    /// </param>
    public void UnschedulePayment(
        BillActionOrigin origin,
        DateTime occurredAt,
        UserId? requestedBy = null,
        string? requesterName = null,
        string? note = null)
    {
        ArgumentNullException.ThrowIfNull(origin);

        if (Status != BillStatus.Approved && Status != BillStatus.Scheduled)
            throw BillErrors.PaymentTransitionNotAllowed(Status.Name, BillStatus.Approved.Name);

        var from = Status;

        PaymentOrderId = null;
        ScheduledFor = null;
        Status = BillStatus.Approved;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Unscheduled, origin, from, Status, requestedBy, requesterName, occurredAt,
            note ?? DefaultUnscheduleNote(origin)));
    }

    private static string DefaultUnscheduleNote(BillActionOrigin origin)
        => origin == BillActionOrigin.Provider
            ? "Agendamento cancelado NO PROVEDOR; a aprovação continua válida"
            : "Agendamento cancelado; a aprovação continua válida";

    /// <summary>
    /// Devolve um boleto de pagamento falhado à fila de decisão. A nova tentativa é uma nova
    /// aprovação e uma nova ordem (ADR-002) — por isso o vínculo com a ordem anterior é limpo.
    /// </summary>
    public void ReopenForApproval(DateTime occurredAt, UserId? reopenedBy = null, string? reopenerName = null)
    {
        // Só Failed reabre. A matriz também admite Approved → AwaitingApproval (é a aresta da
        // revalidação), mas reabrir um Approved por aqui descartaria uma aprovação vigente sem
        // motivo de pagamento — quem quer desfazer uma aprovação revalida ou cancela.
        if (Status != BillStatus.Failed)
            throw BillErrors.PaymentTransitionNotAllowed(Status.Name, BillStatus.AwaitingApproval.Name);

        var from = Status;

        PaymentOrderId = null;
        ScheduledFor = null;
        Status = BillStatus.AwaitingApproval;
        UpdatedAt = occurredAt;

        _history.Add(BillHistoryEntry.Record(
            BillAction.Reopened, BillActionOrigin.User, from, Status, reopenedBy, reopenerName,
            occurredAt,
            "Reaberto para nova tentativa de pagamento"));
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

    private void EnsureScheduleDateIsAllowed(DateOnly scheduleFor, DateOnly today, SameDayScheduling sameDay)
    {
        if (scheduleFor < today)
            throw BillErrors.ScheduleDateInThePast(scheduleFor, today);

        if (Lookup?.MinimumScheduleDate is { } minimum && scheduleFor < minimum)
            throw BillErrors.ScheduleDateBeforeProviderMinimum(scheduleFor, minimum);

        // Só HOJE depende da hora (ADR-021), e por isso o veredito entra por parâmetro em vez de
        // ser calculado aqui: o agregado não conhece relógio nem calendário bancário. Datas
        // futuras não passam por esta porta.
        if (scheduleFor == today && !sameDay.Allowed)
            throw BillErrors.SameDaySchedulingUnavailable(sameDay.ReasonCode);
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
