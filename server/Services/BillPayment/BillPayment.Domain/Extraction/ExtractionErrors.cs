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

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
