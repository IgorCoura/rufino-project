namespace EconomicCore.Domain.Operational.EconomicEvents;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

public static class EconomicEventErrors
{
    private const string PREFIX = "ECC.EVT";

    public static DomainException MissingParticipants(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "EconomicEvent requer ao menos um Provider e um Recipient (Axiom 3).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidAmount(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Amount de EconomicEvent deve ser maior que zero.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MissingResource(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "ResourceId é obrigatório (Axiom 1: todo evento afeta um recurso identificável).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OrphanEvent(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "EconomicEvent órfão: registro exige exatamente uma cobertura (DualityLink XOR CoveringCommitment).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException DualityAlreadyClosed(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Não é possível fechar a dualidade: o evento já está totalmente pareado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException MatchExceedsBalance(
        decimal attempted,
        decimal remaining,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "MatchedAmount ({0}) excede o saldo não pareado do evento ({1}).",
            parameters: new object[] { attempted, remaining },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    // ECC.EVT07 — slot reservado: imutabilidade do fato é estrutural (sem setters públicos após Create).
    // A factory existe como documentação da invariante; nunca é chamada no fluxo normal.
    public static DomainException ImmutableFactViolation(
        string fieldName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "EconomicEvent é imutável: campo '{0}' não pode ser alterado após registro.",
            parameters: new object[] { fieldName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    // Erros adicionais (EVT08+) para validação de VOs internos do aggregate.
    public static DomainException InvalidParticipationAgent(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}08",
            messageTemplate: "Participation requer um EconomicAgentId não vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidParticipationRole(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}09",
            messageTemplate: "Participation requer um ParticipationRole válido (Provider ou Recipient).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidEventTimestamp(
        string kind,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}10",
            messageTemplate: "EventTimestamp deve ser UTC (recebido DateTimeKind={0}).",
            parameters: new object[] { kind },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidDualityCounterpart(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}11",
            messageTemplate: "DualityLink requer um CounterpartEventId não vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidDualityMatchedAmount(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}12",
            messageTemplate: "DualityLink requer MatchedAmount maior que zero.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCommitmentRef(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}13",
            messageTemplate: "CommitmentRef requer ContractId e CommitmentId não vazios.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OccupancyInFuture(
        int periodYear,
        int periodMonth,
        DateOnly today,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}14",
            messageTemplate: "Não é possível registrar ocupação para o período {0}/{1}: o mês ainda não iniciou (hoje: {2}).",
            parameters: new object[] { periodMonth, periodYear, today },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException FuturePaidDate(
        DateTime paidDate,
        DateTime now,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}15",
            messageTemplate: "PaidDate {0} é futura em relação à data atual ({1}).",
            parameters: new object[] { paidDate, now },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidAllocation(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}16",
            messageTemplate: "PaymentAllocation requer um CommitmentRef válido e um Amount maior que zero.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException EmptyAllocations(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}17",
            messageTemplate: "Pagamento bundled requer ao menos uma alocação (rateio por commitment).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AllocationNotFound(
        Guid commitmentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}18",
            messageTemplate: "Nenhuma alocação para o commitment {0} neste EconomicEvent.",
            parameters: new object[] { commitmentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
