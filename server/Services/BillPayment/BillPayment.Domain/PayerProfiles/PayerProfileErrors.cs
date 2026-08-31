namespace BillPayment.Domain.PayerProfiles;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: PRF (PayerProfile)
// Public porque a Application lança NotFound e AlreadyRegistered nas pré-condições.
public static class PayerProfileErrors
{
    private const string AGGREGATE_PREFIX = "BLP.PRF";

    public static DomainException PrimaryTaxIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "O documento fiscal principal do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PrimaryTaxIdKindMismatch(
        string payerKind,
        string expectedTaxIdKind,
        string actualTaxIdKind,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "Tenant do tipo {0} exige documento principal {1}, mas recebeu {2}.",
            parameters: new object[] { payerKind, expectedTaxIdKind, actualTaxIdKind },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O cadastro fiscal é um por tenant. A checagem é intencionalmente escopada ao tenant:
    /// perguntar se um documento já existe em <em>outra</em> conta seria uma quarta travessia
    /// entre tenants, e só três são autorizadas neste BC.
    /// </summary>
    public static DomainException TenantAlreadyHasProfile(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "Este tenant já possui cadastro fiscal. Altere o existente em vez de criar outro.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException NotFound(
        Guid tenantId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "Cadastro fiscal do tenant {0} não encontrado.",
            parameters: new object[] { tenantId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException LegalNameRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "O nome do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException LegalNameTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "O nome do tenant excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CnpjRootMatchingRequiresCompany(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "Casar por raiz de CNPJ só faz sentido para pessoa jurídica.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AdditionalTaxIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O documento adicional é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AdditionalTaxIdCannotBePrimary(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O documento principal não pode ser cadastrado também como adicional.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    // BLP.PRF10 (AsaasAccountRefTooLong) foi APOSENTADO em 2026-08-31, quando o ponteiro virou
    // CredentialRef e o tamanho passou a ser invariante do VO. Não reutilize o número.

    public static DomainException AsaasKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "A chave de API da conta Asaas é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AsaasKeyRejected(
        string reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "O provedor recusou a chave informada ({0}). Confira a chave da subconta e tente de novo.",
            parameters: new object[] { reasonCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException AsaasProviderUnreachable(
        string reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "Não foi possível provar a chave junto ao provedor ({0}). Tente novamente em instantes.",
            parameters: new object[] { reasonCode },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
