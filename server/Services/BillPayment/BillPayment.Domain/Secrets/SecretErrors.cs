namespace BillPayment.Domain.Secrets;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — módulo: SEC (Secrets)
// Public porque o cofre vive na Infra e é ele quem compõe e resolve CredentialRef.
public static class SecretErrors
{
    private const string MODULE_PREFIX = "BLP.SEC";

    public static DomainException CredentialRefRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}01",
            messageTemplate: "A referência de credencial é obrigatória.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// A mensagem não repete o valor recebido: uma referência malformada pode ser um segredo
    /// colado no campo errado, e ecoá-la o gravaria no log.
    /// </summary>
    public static DomainException CredentialRefMalformed(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}02",
            messageTemplate: "A referência de credencial não está no formato esperado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SecretNotFound(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}03",
            messageTemplate: "A credencial referenciada não existe mais no cofre.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    /// <summary>
    /// Decifrar falhou. Pode ser master key trocada, linha adulterada ou dado autenticado
    /// divergente — e a mensagem não distingue os três de propósito, porque a diferença só
    /// interessa a quem opera o cofre, não a quem chamou.
    /// </summary>
    public static DomainException SecretUnreadable(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}04",
            messageTemplate: "A credencial não pôde ser lida do cofre.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException SecretValueRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}05",
            messageTemplate: "Não é possível guardar uma credencial vazia.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SecretKindRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}06",
            messageTemplate: "É preciso informar a natureza da credencial guardada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Não há master key configurada. Falhar aqui é deliberado: um cofre que aceitasse guardar
    /// segredo sem chave o gravaria em claro, e a falha só apareceria no vazamento.
    /// </summary>
    public static DomainException VaultNotConfigured(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}07",
            messageTemplate: "O cofre de credenciais não está configurado neste ambiente.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
