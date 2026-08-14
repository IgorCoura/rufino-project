namespace TenantManagement.Domain.Tenants;

using System.IO;
using System.Runtime.CompilerServices;
using TenantManagement.Domain.SeedWork;

// BC: TNM (TenantManagement)
// Aggregate: TNT (Tenant)
// Public porque a Application lança NotFound e TaxIdAlreadyRegistered nas pré-condições.
public static class TenantErrors
{
    private const string AGGREGATE_PREFIX = "TNM.TNT";

    public static DomainException KindRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "O tipo do tenant (pessoa física ou jurídica) é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException LegalNameRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "O nome do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException LegalNameTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "O nome do tenant excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PrimaryTaxIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "O documento fiscal do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>A única diferença entre PF e PJ que o cadastro impõe.</summary>
    public static DomainException PrimaryTaxIdKindMismatch(
        string tenantKind,
        string expectedTaxIdKind,
        string actualTaxIdKind,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "Tenant do tipo {0} exige documento {1}, mas recebeu {2}.",
            parameters: new object[] { tenantKind, expectedTaxIdKind, actualTaxIdKind },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TradeNameRequiresCompany(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "Nome fantasia só se aplica a pessoa jurídica.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TradeNameTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "O nome fantasia excede o limite de {0} caracteres.",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ContactRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "O contato do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AddressRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "O endereço do tenant é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Um documento fiscal identifica um tenant e um só. Dois cadastros com o mesmo CPF ou
    /// CNPJ primário separariam o dinheiro da mesma pessoa em duas contas sem que ninguém
    /// tivesse decidido isso.
    /// </summary>
    public static DomainException TaxIdAlreadyRegistered(
        string taxId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "Já existe um tenant cadastrado com o documento {0}.",
            parameters: new object[] { taxId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException NotFound(
        Guid tenantId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "Tenant {0} não encontrado.",
            parameters: new object[] { tenantId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException SuspendedTenantIsReadOnly(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "Tenant suspenso não aceita alterações. Reative-o antes.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException AlreadySuspended(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "Este tenant já está suspenso.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException NotSuspended(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}14",
            messageTemplate: "Este tenant não está suspenso.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException SuspensionReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}15",
            messageTemplate: "Suspender um tenant exige registrar o motivo.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ProductRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}16",
            messageTemplate: "O produto é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ProductNotActive(
        string product,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}17",
            messageTemplate: "O produto {0} não está habilitado para este tenant.",
            parameters: new object[] { product },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException MembershipEmailRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}18",
            messageTemplate: "O e-mail de quem recebe o acesso é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidMembershipEmail(
        string email,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}19",
            messageTemplate: "E-mail inválido: {0}.",
            parameters: new object[] { email },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Um tenant sem dono é um cadastro que ninguém consegue mais administrar — e o socorro
    /// seria mexer no banco à mão.
    /// </summary>
    public static DomainException LastOwnerCannotBeRevoked(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}20",
            messageTemplate: "Este é o último responsável pelo tenant e não pode perder o acesso.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException MembershipNotFound(
        string email,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}21",
            messageTemplate: "{0} não tem acesso a este tenant.",
            parameters: new object[] { email },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException MembershipRoleRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}22",
            messageTemplate: "O papel do membro é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O tipo chegou como texto pela borda e não corresponde a nenhum valor conhecido. É erro
    /// de domínio, e não de plataforma, porque a lista de valores válidos é do negócio.
    /// </summary>
    public static DomainException UnknownKind(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}23",
            messageTemplate: "Tipo de tenant desconhecido: {0}. Use Individual ou Company.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException UnknownStatus(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}26",
            messageTemplate: "Situação desconhecida: {0}. Use Active ou Suspended.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException UnknownProduct(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}24",
            messageTemplate: "Produto desconhecido: {0}.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException UnknownMembershipRole(
        string value,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}25",
            messageTemplate: "Papel desconhecido: {0}. Use Owner ou Member.",
            parameters: new object[] { value },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
