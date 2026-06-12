namespace EconomicCore.Domain.Prospective.EconomicContracts;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

public static class EconomicContractErrors
{
    private const string PREFIX = "ECC.CTR";

    public static DomainException MissingReciprocal(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Todo Commitment de outflow exige ReciprocalLink para um Commitment de inflow.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DuplicateCommitmentForPeriod(
        int year,
        int month,
        string directionName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Já existe Commitment para o período {0}/{1} na direção {2}.",
            parameters: new object[] { month, year, directionName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException CannotFulfillInStatus(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Commitment em status {0} não pode ser cumprido (apenas Promised/Reserved).",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    // ECC.CTR04 — slot reservado: divergência de Amount fora da tolerância sinaliza (não bloqueia em Phase 1).
    public static DomainException AmountOutsideTolerance(
        decimal expected,
        decimal actual,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Amount {0} fora da tolerância em relação ao esperado {1}.",
            parameters: new object[] { actual, expected },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ContractNotActive(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Operação inválida: EconomicContract está em status {0} (esperado Active).",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    // Erros adicionais (CTR06+) para validação de VOs internos.
    public static DomainException InvalidRecurrencePeriodicity(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "RecurrencePattern requer Periodicity não nula (Monthly, Weekly ou Yearly).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidRecurrenceAnchorDay(
        int anchorDay,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "AnchorDay {0} inválido (esperado entre 1 e 31).",
            parameters: new object[] { anchorDay },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCommitmentTermsAmount(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}08",
            messageTemplate: "CommitmentTerms requer ExpectedAmount maior que zero.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCommitmentTermsTolerance(
        decimal tolerance,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}09",
            messageTemplate: "TolerancePercent {0} inválido (esperado entre 0 e 1).",
            parameters: new object[] { tolerance },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCommitmentTermsWindow(
        int windowDays,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}10",
            messageTemplate: "WindowDays {0} inválido (esperado >= 0).",
            parameters: new object[] { windowDays },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidReciprocalLink(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}11",
            messageTemplate: "ReciprocalLink requer um CommitmentId não vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CommitmentNotFound(
        Guid commitmentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}12",
            messageTemplate: "Commitment {0} não encontrado neste EconomicContract.",
            parameters: new object[] { commitmentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException InvalidContractStatusTransition(
        string from,
        string to,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}13",
            messageTemplate: "Transição inválida de ContractStatus: {0} -> {1}.",
            parameters: new object[] { from, to },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ContractNotFound(
        Guid contractId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}14",
            messageTemplate: "EconomicContract {0} não encontrado para o tenant informado.",
            parameters: new object[] { contractId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException NoCoveringCommitmentForPeriod(
        int year,
        int month,
        string directionName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}15",
            messageTemplate: "Nenhum Commitment {2} em status Promised encontrado para o período {0}/{1}.",
            parameters: new object[] { year, month, directionName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ContractNotDraft(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}16",
            messageTemplate: "Operação inválida: EconomicContract está em status {0} (esperado Draft).",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException InvalidTermMonths(
        int value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}17",
            messageTemplate: "TermMonths {0} inválido (esperado entre 1 e 120).",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidStartDate(
        DateOnly value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}18",
            messageTemplate: "StartDate {0} inválida: não pode ser anterior a 1 ano da data atual.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PaymentAmountMismatch(
        decimal expected,
        decimal received,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}19",
            messageTemplate: "Valor pago {1} difere do valor esperado {0} (pagamento parcial não suportado).",
            parameters: new object[] { expected, received },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidTerminationDate(
        DateOnly terminationDate,
        string lastOccupiedPeriod,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}20",
            messageTemplate: "TerminationDate {0} é anterior ao último período ocupado ({1}).",
            parameters: new object[] { terminationDate, lastOccupiedPeriod },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OverlappingActiveContract(
        Guid resourceId,
        DateOnly startDate,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}21",
            messageTemplate: "Já existe EconomicContract Draft/Active no recurso {0} com termo sobreposto a {1}.",
            parameters: new object[] { resourceId, startDate },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ChargesOnlyInDraft(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}22",
            messageTemplate: "Encargos só podem ser adicionados com o contrato em Draft (status atual: {0}).",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException DuplicateChargePurpose(
        string purposeName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}23",
            messageTemplate: "Já existe um encargo do tipo {0} neste contrato.",
            parameters: new object[] { purposeName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException RentChargeImplicit(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}24",
            messageTemplate: "A trilha-núcleo do contrato (PrimaryPurpose) não pode ser adicionada como encargo extra.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidChargeAmount(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}25",
            messageTemplate: "ContractCharge requer ExpectedAmount maior que zero.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidChargeResource(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}26",
            messageTemplate: "ContractCharge requer um EconomicResourceId não vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidChargeRecipient(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}27",
            messageTemplate: "ContractCharge requer um EconomicAgentId de destinatário não vazio.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidChargePurpose(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}28",
            messageTemplate: "ContractCharge requer um CommitmentPurpose não nulo.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidPenaltyTerms(
        decimal percent,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}30",
            messageTemplate: "Percentual de penalidade {0} inválido (esperado entre 0 e 1).",
            parameters: new object[] { percent },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AdjustmentOverLockedPeriod(
        int year,
        int month,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}40",
            messageTemplate: "Reajuste inválido: o período {0}/{1} ou anterior já foi cumprido e está travado.",
            parameters: new object[] { month, year },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException NoCommitmentsToAdjust(
        string purposeName,
        int year,
        int month,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}41",
            messageTemplate: "Nenhum commitment {0} em aberto a partir de {2}/{1} para reajustar.",
            parameters: new object[] { purposeName, year, month },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException AmbiguousAdjustment(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}42",
            messageTemplate: "Reajuste exige exatamente um entre novo valor absoluto OU índice (indexRate).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NoChargeTrack(
        string purposeName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}43",
            messageTemplate: "Não há trilha {0} neste contrato para ler o valor corrente.",
            parameters: new object[] { purposeName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException CommitmentAlreadyCovered(
        Guid commitmentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}44",
            messageTemplate: "Commitment {0} já está coberto por outro evento econômico.",
            parameters: new object[] { commitmentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
