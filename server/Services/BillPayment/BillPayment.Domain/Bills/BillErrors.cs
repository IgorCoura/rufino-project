namespace BillPayment.Domain.Bills;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: BIL (Bill)
// Public porque a Application lança NotFound e AlreadyCaptured nas pré-condições.
public static class BillErrors
{
    private const string AGGREGATE_PREFIX = "BLP.BIL";

    /// <summary>
    /// A mensagem não repete o instrumento: linha digitável e BR Code são instrumentos de
    /// pagamento e não podem vazar para tela nem log.
    /// </summary>
    public static DomainException UnreadableInstrument(
        string reason,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Não foi possível ler o instrumento de pagamento: {0}.",
            parameters: new object[] { reason },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Duplicata global — deliberadamente <strong>genérica</strong>. A colisão pode ser com
    /// um boleto de outro tenant, e dizer de quem seria vazamento entre contas (ADR-008).
    /// </summary>
    public static DomainException AlreadyCaptured(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "Este documento de cobrança já foi importado e está aguardando tratamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException TerminalStatus(
        string status,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "Boleto em situação {0} não aceita alteração.",
            parameters: new object[] { status },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException InstrumentRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O boleto precisa de pelo menos uma forma de pagamento: código de barras ou QR Code Pix.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DuplicateInstrument(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O mesmo instrumento de pagamento foi informado duas vezes.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OriginSourceKindRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "A origem do boleto precisa informar por onde ele entrou.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OriginSourceIdRequired(
        string sourceKind,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "Origem do tipo {0} exige a fonte de captura que trouxe o documento.",
            parameters: new object[] { sourceKind },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OriginWithoutAnyIdentifier(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "A origem precisa de ao menos um identificador — fonte, remetente, mensagem, arquivo ou hash.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OriginFieldTooLong(
        string field,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "O campo {0} da origem excede o limite de {1} caracteres.",
            parameters: new object[] { field, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NotFound(
        Guid billId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}14",
            messageTemplate: "Boleto {0} não encontrado.",
            parameters: new object[] { billId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// Dois códigos de barras de naturezas diferentes no mesmo documento. Um boleto é de
    /// cobrança ou de arrecadação; misturar significa que a extração juntou dois documentos.
    /// </summary>
    public static DomainException MixedBillKinds(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}15",
            messageTemplate: "O documento traz códigos de barras de naturezas diferentes — provavelmente são dois documentos.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CheckTypeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}16",
            messageTemplate: "Toda verificação precisa dizer a que tipo pertence.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Só um <c>Passed</c> limpo pode não ter motivo — é essa string que a tela mostra ao
    /// aprovador e que o relatório de exceção agrupa.
    /// </summary>
    public static DomainException CheckReasonRequired(
        string checkType,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}17",
            messageTemplate: "A verificação {0} não passou e precisa registrar o motivo.",
            parameters: new object[] { checkType },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DuplicateCheckType(
        string checkType,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}18",
            messageTemplate: "A verificação {0} foi apurada duas vezes na mesma validação.",
            parameters: new object[] { checkType },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O conjunto de checks é substituído inteiro a cada validação (ADR-003). Gravar um
    /// conjunto parcial deixaria perguntas sem resposta parecendo respondidas.
    /// </summary>
    public static DomainException IncompleteCheckCoverage(
        string missingChecks,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}19",
            messageTemplate: "A validação não apurou todas as verificações — faltaram: {0}.",
            parameters: new object[] { missingChecks },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ValidationNotAllowedInStatus(
        string status,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}20",
            messageTemplate: "Boleto em situação {0} não aceita nova verificação.",
            parameters: new object[] { status },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ExtractedPartyWithoutAnyIdentifier(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}21",
            messageTemplate: "O pagador lido do documento precisa ter nome ou documento fiscal.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Invariante 3: aprovar exige que <strong>todas</strong> as verificações tenham sido
    /// apuradas. Um check novo invalida aprovações pendentes até a revalidação, e é o
    /// comportamento desejado — é uma pergunta que ninguém ainda respondeu para aquele boleto.
    /// </summary>
    public static DomainException ChecksNotEvaluated(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "Este boleto ainda não foi verificado por completo — revalide antes de aprovar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Invariante 4. O aprovador aprova <em>apesar</em> de um check advisory; de um bloqueante,
    /// não — o motivo precisa ser resolvido primeiro.
    /// </summary>
    public static DomainException BlockedByFailedChecks(
        string reasons,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "Este boleto tem verificação bloqueante reprovada e não pode ser aprovado: {0}.",
            parameters: new object[] { reasons },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ScheduleDateInThePast(
        DateOnly scheduleFor,
        DateOnly today,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "A data de pagamento {0} é anterior a hoje ({1}).",
            parameters: new object[] { scheduleFor, today },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ScheduleDateBeforeProviderMinimum(
        DateOnly scheduleFor,
        DateOnly minimum,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        // BIL31, e não BIL05: partilhava o id com ScheduleDateInThePast até 2026-08-28, e a UI
        // que traduz por id não distinguia "ontem" de "antes do que o provedor aceita".
        => new(
            id: $"{AGGREGATE_PREFIX}31",
            messageTemplate: "A data de pagamento {0} é anterior à primeira data que o provedor aceita ({1}).",
            parameters: new object[] { scheduleFor, minimum },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Invariante 6. O retrato envelhece — valor de boleto vencido muda todo dia —, e aprovar
    /// contra um retrato velho é consentir com um número que já não é o que será debitado.
    /// </summary>
    public static DomainException StaleLookupSnapshot(
        int ageInHours,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "A consulta oficial tem {0}h e precisa ser refeita antes da aprovação.",
            parameters: new object[] { ageInHours },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ApproverRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}22",
            messageTemplate: "Toda decisão sobre um boleto precisa identificar quem a tomou.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DecisionReasonRequired(
        string decision,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}23",
            messageTemplate: "A decisão de {0} precisa registrar o motivo.",
            parameters: new object[] { decision },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// A alçada é do aprovador, não do boleto: o mesmo documento pode ser aprovável por uma
    /// pessoa e não por outra. A mensagem não repete o teto — quem aprova sabe o seu.
    /// </summary>
    public static DomainException AboveApprovalLimit(
        decimal amount,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}24",
            messageTemplate: "O valor de {0} está acima da sua alçada de aprovação.",
            parameters: new object[] { amount },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ApprovalPolicyWithoutSnapshotWindow(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}26",
            messageTemplate: "A política de aprovação precisa de um prazo de validade positivo para a consulta.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DecisionNotAllowedInStatus(
        string decision,
        string status,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}25",
            messageTemplate: "Não é possível {0} um boleto em situação {1}.",
            parameters: new object[] { decision, status },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// ADR-015: boleto em Perigo ou Extremo Perigo só é aprovável com o risco explicitamente
    /// assumido pelo aprovador — sem o aceite, a recusa lista o nível e os motivos.
    /// </summary>
    public static DomainException DangerRequiresAcknowledgment(
        string riskLevel,
        string reasons,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}27",
            messageTemplate: "Este boleto está classificado como {0} ({1}). Para aprová-lo é preciso "
                + "assumir o risco explicitamente.",
            parameters: new object[] { riskLevel, reasons },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// A alçada de risco de quem aprova não cobre o nível do boleto — 403, decidido pelo
    /// DOMÍNIO contra o risco atual (a borda só resolve quais escopos a pessoa tem).
    /// </summary>
    public static DomainException ApprovalAboveRiskClearance(
        string riskLevel,
        string clearance,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}32",
            messageTemplate: "Este boleto está classificado como {0}, acima da sua alçada de aprovação "
                + "({1}). Peça a alguém com a alçada adequada.",
            parameters: new object[] { riskLevel, clearance },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Forbidden);

    /// <summary>Aprovação sem alçada resolvida é defeito da borda, não escolha do usuário.</summary>
    public static DomainException ApprovalClearanceRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}33",
            messageTemplate: "A alçada de aprovação de quem decide é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O extrator de IA não respondeu ao reler o documento do boleto.
    /// </summary>
    /// <remarks>
    /// <strong>É lançada para o boleto VOLTAR À FILA</strong>, e não porque algo esteja errado
    /// com ele. A análise por IA nunca bloqueia o boleto — o que ela bloqueia é a si mesma, até o
    /// provedor responder. Tratar indisponibilidade como "nada a ler" deixaria o boleto sem
    /// retrato por causa de um 503, que é o defeito medido em 2026-08-27.
    /// </remarks>
    public static DomainException ReadingUnavailable(
        string? reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}28",
            messageTemplate: "A leitura por IA não pôde ser feita agora ({0}). O boleto volta para a fila.",
            parameters: new object[] { reasonCode ?? "provider_unavailable" },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// O arquivo anexado na importação manual é de um tipo que a leitura não sabe abrir.
    /// </summary>
    /// <remarks>
    /// Recusar <strong>antes</strong> de gravar é o que impede o balde de acumular arquivo que
    /// nunca poderá ser lido — mesma regra do anexo manual da quarentena.
    /// </remarks>
    public static DomainException UnsupportedDocument(
        string contentType,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}29",
            messageTemplate: "Arquivo do tipo {0} não é aceito. Envie PDF, PNG, JPEG ou WebP.",
            parameters: new object[] { contentType },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Há teto de alçada e nenhum valor para compará-lo — nem oficial, nem impresso no
    /// instrumento (QR estático sem valor). Aprovar aqui seria aprovar sem alçada.
    /// </summary>
    public static DomainException ApprovalLimitRequiresAmount(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}30",
            messageTemplate: "Não dá para aplicar a alçada de aprovação: o boleto não tem valor conhecido. Refaça a consulta oficial antes de aprovar.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Reflexo de pagamento fora da máquina de estados — um evento da <c>PaymentOrder</c>
    /// chegando a um boleto que não está no estado esperado (ADR-002).
    /// </summary>
    public static DomainException PaymentTransitionNotAllowed(
        string current,
        string target,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}34",
            messageTemplate: "Boleto em situação {0} não aceita refletir o pagamento como {1}.",
            parameters: new object[] { current, target },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// ADR-017: o boleto está vencido, o provedor o processaria imediatamente — sem janela de
    /// reação — e ninguém confirmou que quer isso.
    /// </summary>
    public static DomainException OverdueRequiresImmediateAcknowledgment(
        DateOnly dueDate,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}35",
            messageTemplate: "Este boleto venceu em {0:dd/MM/yyyy} e será pago imediatamente, sem agendamento. Confirme que deseja pagar agora.",
            parameters: new object[] { dueDate },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
