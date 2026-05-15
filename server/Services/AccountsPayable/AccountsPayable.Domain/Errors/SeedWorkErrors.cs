namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// SeedWork (SWK) — transversal errors thrown by base classes.
//
// Error ID convention for this Bounded Context (AccountsPayable):
//   AP.<AGG>##  — aggregate-specific errors  (e.g., AP.PAY01, AP.SUP01)
//   AP##        — cross-aggregate BC errors  (e.g., AP01 - TenantMismatch)
//   SWK##       — SeedWork base-class errors (e.g., SWK01 - EmptyId) — sigla reservada
//
// Aggregate / Entity / VO / Service sigla table (extend as code is introduced):
//   Aggregates:
//     PAY → Payable                       (Sprint 2, A+ES)
//     SUP → Supplier                      (Sprint 1)
//     COA → ChartOfAccounts               (Sprint 3)
//     CCT → CostCenter                    (Sprint 3)
//     AAP → AutoApprovalPolicy            (Sprint 10, traditional)
//   Internal Entities (Sprint 10):
//     ARU → ApprovalRule                  (inside AutoApprovalPolicy — errors reuse AAP prefix)
//     ECR → ExpenseClassificationRule     (Sprint 9, traditional)
//     CTR → Contract                      (Sprint 11, traditional)
//     ERB → ExpectedRecurringBill         (Sprint 11, traditional)
//     IPL → InstallmentPlan               (Sprint 8, traditional grouper)
//   Internal Entities:
//     SBA → SupplierBankAccount           (Sprint 1, inside Supplier)
//   Value Objects (per-VO factory, shared across aggregates):
//     TXI → TaxId
//     LGN → LegalName
//     TRN → TradeName
//     EML → EmailAddress
//     PHN → PhoneNumber
//     ADR → Address
//     PIX → PixKey
//     CTI → ContactInfo
//     MON → Money
//     DSC → Description
//     PPF → PaymentProof
//     DUE → DueDate          (sem errors próprios — validação contextual fica no Aggregate)
//     CON → ChartOfAccountsName
//     ACO → AccountCode
//     ANM → AccountName
//     CCC → CostCenterCode
//     CCN → CostCenterName
//     CMR → ClassificationMatcher        (Sprint 9)
//     CAT → ClassificationAction         (Sprint 9)
//     AMC → ApprovalMatchCriteria        (Sprint 10)
//     APR → ApproverRoles                (Sprint 10)
//   Domain Services:
//     SUC → SupplierUniquenessChecker     (Sprint 1)
//     PCL → PayableClassification         (Sprint 4)
//     RBG → RecurringBillGenerator        (Sprint 11, sem errors próprios)
//     RBM → RecurringBillMatcher          (Sprint 11, sem errors próprios)
public static class SeedWorkErrors
{
    private const string PREFIX = "SWK";

    public static DomainException EmptyId(
        string entityTypeName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Id de {0} não pode ser Guid.Empty.",
            parameters: new object[] { entityTypeName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MissingWhenHandler(
        string aggregateTypeName,
        string eventTypeName,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Aggregate {0} não possui handler When({1}) para o evento aplicado.",
            parameters: new object[] { aggregateTypeName, eventTypeName },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
