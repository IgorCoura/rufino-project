namespace BillPayment.Domain.Instruments;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — VO: INS (PaymentInstrument)
public static class PaymentInstrumentErrors
{
    private const string PREFIX = "BLP.INS";

    public static DomainException DigitableLineRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "O instrumento de código de barras exige uma linha digitável válida.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PixPayloadRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "O instrumento Pix exige um payload de QR Code válido.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Pedir a linha digitável de um instrumento Pix (ou o contrário) é erro de programação,
    /// não de dado. Existe para a chamada indevida falhar alto em vez de devolver nulo.
    /// </summary>
    public static DomainException WrongKindAccess(
        string requested,
        string actual,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Instrumento do tipo {1} não expõe {0}. Consulte Kind antes.",
            parameters: new object[] { requested, actual },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
