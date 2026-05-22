namespace AccountsPayable.Domain.Errors;

using System.IO;
using System.Runtime.CompilerServices;
using AccountsPayable.Domain.SeedWork;

// Aggregate: PAY (Payable) — Event-Sourced (D-405)
internal static class PayableErrors
{
    private const string PREFIX = "AP.PAY";

    public static DomainException InvalidStatusTransition(
        string currentStatus,
        string targetStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}01",
            messageTemplate: "Transição inválida de status da conta a pagar: {0} → {1}.",
            parameters: new object[] { currentStatus, targetStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException DueDateInPast(
        DateOnly attemptedDueDate,
        DateOnly today,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}02",
            messageTemplate: "Vencimento {0} é anterior à data de criação {1}.",
            parameters: new object[] { attemptedDueDate, today },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}03",
            messageTemplate: "Motivo é obrigatório para cancelar a conta a pagar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotPayCancelled(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}04",
            messageTemplate: "Conta a pagar já cancelada não pode ser marcada como paga.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotScheduleWithoutClassification(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}05",
            messageTemplate: "Conta a pagar deve ser classificada antes de ser agendada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CannotClassifyTerminalPayable(
        string currentStatus,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}06",
            messageTemplate: "Conta a pagar em status {0} não pode ser (re)classificada.",
            parameters: new object[] { currentStatus },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    // AP.PAY07 reservado (era RequiresApproval). Removido: o threshold deixou de ser parâmetro do
    // Aggregate — a decisão de exigir aprovação vive na Application (ApprovalRequirementCalculator)
    // e materializa via transição para AwaitingApproval, bloqueada pela máquina de estados (AP.PAY01).
    // Não reutilizar este slot sem confirmar que nenhum consumidor externo depende do Id antigo.

    public static DomainException RequiresClassificationBeforeApproval(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}08",
            messageTemplate: "Conta a pagar deve ser classificada antes de solicitar aprovação.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException RejectionReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}09",
            messageTemplate: "Motivo é obrigatório para rejeitar a conta a pagar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PaymentFailureReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}10",
            messageTemplate: "Motivo é obrigatório para registrar falha de pagamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PaymentOrderIdMismatch(
        Guid expected,
        Guid received,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}11",
            messageTemplate: "Confirmação de pagamento já recebida para PaymentOrder {0}; outra {1} não pode sobrescrever.",
            parameters: new object[] { expected, received },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException InstallmentNumberMustBePositive(
        int installmentNumber,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}12",
            messageTemplate: "Número da parcela deve ser ≥ 1; recebido: {0}.",
            parameters: new object[] { installmentNumber },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MultiApprovalRequiredCountTooLow(
        int count,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}13",
            messageTemplate: "Quantidade requerida de aprovações deve ser ≥ 1; recebido: {0}.",
            parameters: new object[] { count },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException MultiApprovalEligibleRolesRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}14",
            messageTemplate: "Lista de roles elegíveis não pode estar vazia para multi-aprovação.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ApproverRoleNotEligible(
        string role,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}15",
            messageTemplate: "Role '{0}' não está na lista de elegíveis para esta multi-aprovação.",
            parameters: new object[] { role },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ApproverAlreadyRecorded(
        Guid approverId,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}16",
            messageTemplate: "Usuário {0} já registrou sua aprovação nesta conta.",
            parameters: new object[] { approverId },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException SingleApprovalNotAllowedInMultiMode(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}17",
            messageTemplate: "Conta em modo multi-aprovação não aceita Approve(usuário) — use RecordApproval(usuário, role).",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException PaymentInstrumentRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}18",
            messageTemplate: "Instrumento de pagamento é obrigatório na criação da conta a pagar.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    // AP.PAY19 reservado (era PaymentMethodInstrumentMismatch — descartado na Sprint 12.G).
    // O parâmetro PaymentMethod foi removido das factories do Payable; o método de pagamento é
    // sempre derivado de PaymentInstrument.Method, então não há mismatch possível por construção.
    // Não reutilizar este slot — Ids estáveis para consumidores que possam ter logado AP.PAY19.

    // AP.PAY20 reservado (era SupplierMissingPixKey — descartado: PaymentMethod.SupplierTransfer
    // agora é genérico e aceita PIX sem chave; PSP decide canal).

    public static DomainException BankAccountSupplierMismatch(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}21",
            messageTemplate: "Snapshot da conta de pagamento não corresponde a uma conta ativa do fornecedor atual.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException OutdatedReasonRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{PREFIX}22",
            messageTemplate: "Motivo é obrigatório para sinalizar instrumento desatualizado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
