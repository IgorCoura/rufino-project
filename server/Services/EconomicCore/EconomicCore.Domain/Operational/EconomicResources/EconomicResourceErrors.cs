namespace EconomicCore.Domain.Operational.EconomicResources;

using System.IO;
using System.Runtime.CompilerServices;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.SeedWork;

public static class EconomicResourceErrors
{
    private const string PREFIX = "ECC.RES";

    public static DomainException InvalidName(
        string name,
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Nome de EconomicResource inválido: '{0}' (não pode ser vazio nem exceder {1} caracteres).",
            parameters: new object[] { name, maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MissingKind(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "ResourceKind é obrigatório (esperado Cash, Service, LaborService ou FiscalObligation).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    // ECC.RES03 — slot reservado para validação cross-aggregate (CustodianId deve ser AgentScope.Inside).
    // Será chamada por um Domain Service futuro (envolve EconomicAgent + EconomicResource).
    // Phase 1 só cria recursos sem custodian, então esta factory existe apenas como contrato.
    public static DomainException CustodianMustBeInternal(
        EconomicAgentId custodianId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "CustodianId {0} deve referir um EconomicAgent com escopo Inside.",
            parameters: new object[] { custodianId.Value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ResourceNotFound(
        Guid resourceId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "EconomicResource {0} não encontrado para o tenant informado.",
            parameters: new object[] { resourceId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
