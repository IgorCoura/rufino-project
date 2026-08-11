namespace BillPayment.Domain.Lookups;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate/módulo: LKP (Lookup)
// Public porque a Infra (adapters de consulta) compõe estes VOs e precisa lançar daqui.
public static class LookupErrors
{
    private const string MODULE_PREFIX = "BLP.LKP";

    /// <summary>
    /// Uma parte sem nome, sem nome fantasia e sem documento não identifica ninguém — e o
    /// check de beneficiário passaria a comparar vazio com vazio.
    /// </summary>
    public static DomainException PartyWithoutAnyIdentifier(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}01",
            messageTemplate: "A consulta oficial não trouxe nome nem documento do beneficiário.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException BeneficiaryRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}02",
            messageTemplate: "O retrato da consulta oficial precisa identificar o beneficiário.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// Um resultado que não resolveu precisa dizer por quê — é essa string que vira evidência
    /// na tela de aprovação e no relatório de falha de consulta.
    /// </summary>
    public static DomainException ReasonCodeRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}03",
            messageTemplate: "Consulta sem resultado precisa registrar o motivo.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SnapshotRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}04",
            messageTemplate: "Consulta resolvida precisa carregar o retrato do documento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AmountBoundsInverted(
        decimal minAmount,
        decimal maxAmount,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}05",
            messageTemplate: "A consulta devolveu valor mínimo {0} maior que o máximo {1}.",
            parameters: new object[] { minAmount, maxAmount },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O documento mascarado do pagador vem do provedor com caracteres ocultos. Sem nenhum
    /// dígito visível ele não contradiz nada e não deveria ter sido guardado.
    /// </summary>
    public static DomainException MaskedTaxIdWithoutVisibleDigits(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}06",
            messageTemplate: "O documento mascarado do pagador não tem nenhum dígito visível.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ConsultedAtRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{MODULE_PREFIX}07",
            messageTemplate: "O retrato da consulta precisa registrar o instante em que foi tirado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
