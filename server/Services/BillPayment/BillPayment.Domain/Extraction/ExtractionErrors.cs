namespace BillPayment.Domain.Extraction;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — VOs da cascata de extração: EXT
// Estes erros indicam parser mal escrito, não resultado de negócio: "não achei boleto" é
// ExtractionResult.NotFound, nunca exceção.
public static class ExtractionErrors
{
    private const string PREFIX = "BLP.EXT";

    public static DomainException InstrumentRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Uma extração bem-sucedida precisa carregar ao menos um instrumento de pagamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ReasonCodeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Uma extração sem resultado precisa registrar o motivo.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Senha candidata sem rótulo seria senha sem evidência: o <c>UnlockedBy</c> ficaria vazio e
    /// não haveria como auditar qual campo do cadastro abriu o documento.
    /// </summary>
    public static DomainException PasswordLabelRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Uma senha candidata precisa registrar de qual campo foi derivada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PayloadRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Não há documento para extrair.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PayloadTooLarge(
        int maxBytes,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "O documento excede o limite de {0} bytes para extração.",
            parameters: new object[] { maxBytes },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Chamar o extrator com mídia que ele não lê é erro de orquestração: quem decide se vale
    /// gastar já deveria ter conferido com <c>DocumentPayload.IsSupported</c>.
    /// </summary>
    public static DomainException UnsupportedMediaType(
        string mediaType,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "O tipo de mídia '{0}' não pode ser lido pelo extrator de documentos.",
            parameters: new object[] { mediaType },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Documento trazido da rede sem o endereço que o produziu seria documento sem procedência:
    /// um boleto que chegou por link não tem anexo para reapresentar, e a URL é a única evidência.
    /// </summary>
    public static DomainException SourceUrlRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}07",
            messageTemplate: "Um documento resolvido por link precisa registrar o endereço de origem.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
