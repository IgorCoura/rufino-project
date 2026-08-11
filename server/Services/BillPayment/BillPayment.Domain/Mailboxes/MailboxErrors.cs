namespace BillPayment.Domain.Mailboxes;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — VOs de leitura de caixa: MBX
// Estes erros indicam adapter mal escrito, não resultado de negócio: a falha da caixa é
// modelada em MailboxStatus, nunca lançada.
public static class MailboxErrors
{
    private const string PREFIX = "BLP.MBX";

    public static DomainException MessageIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "A mensagem lida da caixa precisa de um identificador do provedor.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ArtifactKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "O artefato lido da caixa precisa de uma chave que o distinga na mensagem.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ReasonCodeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Uma leitura de caixa que não teve êxito precisa registrar o motivo.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Chaves repetidas dentro da mesma mensagem quebrariam a idempotência da ingestão — dois
    /// anexos colidiriam no índice único e o segundo boleto seria perdido em silêncio.
    /// </summary>
    public static DomainException DuplicateArtifactKey(
        string artifactKey,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "O artefato '{0}' aparece mais de uma vez na mesma mensagem.",
            parameters: new object[] { artifactKey },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
