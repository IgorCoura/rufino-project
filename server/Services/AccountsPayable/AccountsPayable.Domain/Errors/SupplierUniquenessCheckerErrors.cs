namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Domain Service: SUC (SupplierUniquenessChecker)
internal static class SupplierUniquenessCheckerErrors
{
    private const string PREFIX = "AP.SUC";

    public static DomainException TaxIdAlreadyRegistered(
        string taxIdType,
        string maskedTaxId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Já existe fornecedor cadastrado com {0} {1} neste tenant.",
            parameters: new object[] { taxIdType, maskedTaxId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
