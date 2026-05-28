namespace EconomicCore.Domain.Operational.EconomicAgents;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.SeedWork;

public static class EconomicAgentErrors
{
    private const string PREFIX = "ECC.AGT";

    public static DomainException InvalidName(
        string name,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Nome de EconomicAgent inválido: '{0}' (não pode ser vazio nem exceder {1} caracteres).",
            parameters: new object[] { name, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MissingScope(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "AgentScope é obrigatório (esperado Inside ou Outside).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    // ECC.AGT03 — slot reservado para "TaxId, se presente, válido".
    // A validação real é estrutural: TaxId VO (SharedKernel) lança SHK.TAX01..03 no próprio construtor.
    // Esta factory existe como documentação da invariante §5.3; pode vir a ser usada
    // se a regra mudar (ex.: bloquear TaxId duplicado por tenant via cross-aggregate service).
    public static DomainException InvalidTaxId(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "TaxId de EconomicAgent inválido: '{0}'.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AgentNotFound(
        Guid agentId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "EconomicAgent {0} não encontrado para o tenant informado.",
            parameters: new object[] { agentId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
