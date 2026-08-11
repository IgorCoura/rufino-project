namespace BillPayment.Domain.Instruments;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — VO: PIX (PixPayload)
// Public porque a Application recusa QR ilegível na borda de importação.
public static class PixPayloadErrors
{
    private const string PREFIX = "BLP.PIX";

    public static DomainException Required(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "O payload do QR Code Pix é obrigatório.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// A mensagem não repete o payload: ele vai para tela e log, e BR Code é instrumento de
    /// pagamento tanto quanto linha digitável.
    /// </summary>
    public static DomainException MalformedPayload(
        string reason,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "O QR Code Pix não segue o padrão EMV: {0}.",
            parameters: new object[] { reason },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InvalidCrc(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "O CRC do QR Code Pix não confere — o conteúdo foi alterado ou copiado pela metade.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException NotPix(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "O QR Code não é um BR Code de Pix.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
