namespace BillPayment.Domain.Retention;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: CRP (CaptureRetentionPolicy)
public static class CaptureRetentionPolicyErrors
{
    private const string AGGREGATE_PREFIX = "BLP.CRP";

    public static DomainException WindowRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Informe por quantos dias o histórico deve ser guardado.",
            parameters: [],
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    /// <summary>
    /// Prazo fora da faixa oferecida. A faixa é fechada de propósito: a janela chega pela API, e
    /// um número livre viraria retenção arbitrária — zero apagaria o histórico no instante em que
    /// ele nasce.
    /// </summary>
    public static DomainException UnsupportedWindow(
        int days,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "O prazo de {0} dias não é um dos oferecidos: 7, 30, 90 ou 180.",
            parameters: new object[] { days },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Validation);

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
