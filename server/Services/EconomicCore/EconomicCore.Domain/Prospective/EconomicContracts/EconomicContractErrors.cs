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
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

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

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
