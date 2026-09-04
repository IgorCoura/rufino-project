namespace BillPayment.Domain.Expectations;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: EXP (BillExpectation)
// Public porque a Application lança NotFound e as pré-condições de cadastro.
public static class BillExpectationErrors
{
    private const string AGGREGATE_PREFIX = "BLP.EXP";

    public static DomainException NotFound(
        Guid expectationId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}00",
            messageTemplate: "Expectativa de boleto {0} não encontrada.",
            parameters: new object[] { expectationId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// Unicidade por <c>(TenantId, PayeeId, AccountReference)</c>. A referência de conta faz
    /// parte da chave porque um mesmo tenant tem várias contas do mesmo beneficiário — medido:
    /// quatro instalações da EDP e três do DAE no arquivo real.
    /// </summary>
    public static DomainException AlreadyExists(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Já existe uma expectativa para este beneficiário e esta referência de conta.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Dois ciclos na mesma competência descreveriam a mesma conta esperada duas vezes, e o
    /// segundo alertaria sozinho depois de o primeiro ter sido cumprido.
    /// </summary>
    public static DomainException CycleAlreadyOpen(
        string competence,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "Já existe um ciclo aberto para a competência {0}.",
            parameters: new object[] { competence },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException CycleNotOpen(
        string status,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "O ciclo está em '{0}' e não aceita mais essa operação.",
            parameters: new object[] { status },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Marcar como não cumprido antes da data de alerta transformaria a expectativa em fonte de
    /// falso positivo — que é justamente o que destrói a utilidade do mecanismo.
    /// </summary>
    public static DomainException TooEarlyToMiss(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "O ciclo ainda não chegou à data de alerta e não pode ser dado como não cumprido.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Antecedência maior que o próprio intervalo faria o alerta de um ciclo nascer antes de o
    /// anterior fechar.
    /// </summary>
    public static DomainException InvalidAlertLead(
        int maximum,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "A antecedência do alerta precisa estar entre 1 e {0} dias.",
            parameters: new object[] { maximum },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PayeeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "A expectativa precisa apontar para um beneficiário.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException LabelRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "A expectativa precisa de um rótulo — é o que a pessoa lê no alerta.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TextTooLong(
        string field,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O campo '{0}' da expectativa excede o limite de {1} caracteres.",
            parameters: new object[] { field, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidDueDay(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O dia de vencimento esperado precisa estar entre 1 e 31.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CycleNotFound(
        Guid cycleId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "Ciclo {0} não encontrado nesta expectativa.",
            parameters: new object[] { cycleId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException RecurrenceRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "A recorrência da expectativa é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AlertLevelRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "O nível do alerta é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Expectativa desativada não abre ciclo nem alerta. Reativar é decisão de quem opera.
    /// </summary>
    public static DomainException Inactive(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "Esta expectativa está desativada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
