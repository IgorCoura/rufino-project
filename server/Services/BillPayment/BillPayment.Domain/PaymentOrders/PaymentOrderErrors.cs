namespace BillPayment.Domain.PaymentOrders;

using System.IO;
using System.Runtime.CompilerServices;
using BillPayment.Domain.SeedWork;

// BC: BLP (BillPayment) — Aggregate: PMO (PaymentOrder)
// Public porque a Application lança NotFound nas pré-condições dos commands e do webhook.
public static class PaymentOrderErrors
{
    private const string AGGREGATE_PREFIX = "BLP.PMO";

    public static DomainException NotFound(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}01",
            messageTemplate: "Ordem de pagamento não encontrada.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.NotFound);

    public static DomainException RailRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}02",
            messageTemplate: "A ordem de pagamento precisa de um trilho de pagamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O provedor mandou um retrato incoerente — pago sem data de pagamento. Fora de ordem é
    /// ignorado; incoerente é defeito e lança, para não gravar mentira na trilha.
    /// </summary>
    public static DomainException IncoherentProviderPayload(
        string detail,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}03",
            messageTemplate: "O retrato do provedor é incoerente: {0}.",
            parameters: new object[] { detail },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException TransitionNotAllowed(
        string current,
        string target,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}04",
            messageTemplate: "Ordem de pagamento em situação {0} não aceita ir para {1}.",
            parameters: new object[] { current, target },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException SubmissionRequiresDraft(
        string current,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}05",
            messageTemplate: "Só uma ordem em rascunho pode ser submetida ao provedor — esta está em {0}.",
            parameters: new object[] { current },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>Confirmar execução imediata só faz sentido quando é isso que a ordem espera.</summary>
    public static DomainException ConfirmationNotPending(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}06",
            messageTemplate: "Esta ordem de pagamento não está aguardando confirmação de execução imediata.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>ADR-007: pagar vencido na hora é decisão de gente, e gente tem identidade.</summary>
    public static DomainException ConfirmationRequiresUser(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}07",
            messageTemplate: "Confirmar a execução imediata exige a identidade de quem confirma.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException ProviderOrderIdRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}08",
            messageTemplate: "A submissão aceita exige o identificador da ordem no provedor.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException CancellationNotAllowed(
        string current,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}09",
            messageTemplate: "Ordem de pagamento em situação {0} não pode ser cancelada.",
            parameters: new object[] { current },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException AmountRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}10",
            messageTemplate: "A submissão exige o valor a pagar, e esta ordem não tem nenhum.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException AccountHoldNotPending(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}11",
            messageTemplate: "Esta ordem de pagamento não está retida por falta de conta de pagamento.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException SubmissionErrorRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}12",
            messageTemplate: "Registrar uma falha de submissão exige o erro que a causou.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    public static DomainException HoldRequiresDraft(
        string current,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}13",
            messageTemplate: "Só uma ordem em rascunho pode ser retida — esta está em {0}.",
            parameters: new object[] { current },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    public static DomainException ReceiptStorageKeyRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}14",
            messageTemplate: "Anexar o comprovante exige a chave do arquivo guardado.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>Comprovante só existe de pagamento que aconteceu.</summary>
    public static DomainException ReceiptRequiresPayment(
        string current,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}15",
            messageTemplate: "Ordem em situação {0} não tem comprovante a anexar.",
            parameters: new object[] { current },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>Política de agendamento incoerente é defeito de configuração, não de uso.</summary>
    public static DomainException SchedulingPolicyInvalid(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}17",
            messageTemplate: "A política de agendamento é inválida: antecedência negativa ou janela vazia.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    /// <summary>
    /// O provedor não respondeu à submissão. <strong>Lançada para a ordem VOLTAR À FILA</strong>,
    /// como o <c>BLP.BIL28</c> da leitura por IA — o worker a classifica como passageira e a
    /// retentativa recomeça pela consulta por <c>externalReference</c>.
    /// </summary>
    public static DomainException SubmissionUnavailable(
        string? reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}18",
            messageTemplate: "A submissão do pagamento não pôde ser feita agora ({0}). A ordem volta para a fila.",
            parameters: new object[] { reasonCode ?? "provider_unavailable" },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>O provedor não respondeu ao pedido de cancelamento — tentar de novo é seguro.</summary>
    public static DomainException CancellationUnavailable(
        string? reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}19",
            messageTemplate: "O provedor não respondeu ao cancelamento ({0}). Tente de novo em instantes.",
            parameters: new object[] { reasonCode ?? "provider_unavailable" },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>O provedor respondeu que esta ordem não é mais cancelável.</summary>
    public static DomainException ProviderRefusedCancellation(
        string? reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}20",
            messageTemplate: "O provedor recusou o cancelamento ({0}) — a ordem já está adiantada demais.",
            parameters: new object[] { reasonCode ?? "not_cancellable" },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// O comprovante não pôde ser baixado agora. <strong>Lançada para o outbox retentar</strong>
    /// — mesma família do PMO18: o sinal descreve a rede, não o pagamento.
    /// </summary>
    public static DomainException ReceiptUnavailable(
        string? reasonCode,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}21",
            messageTemplate: "O comprovante não pôde ser baixado agora ({0}). Nova tentativa em seguida.",
            parameters: new object[] { reasonCode ?? "provider_unavailable" },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Cancelar um rascunho enquanto o aluguel de submissão está vigente abriria a corrida em
    /// que o worker paga no provedor uma ordem que aqui já morreu — o espelho diria "cancelado"
    /// com dinheiro vivo lá. A janela é curta: o aluguel vence sozinho.
    /// </summary>
    public static DomainException CancellationDuringSubmission(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}22",
            messageTemplate: "Há uma submissão em andamento para esta ordem de pagamento. Tente novamente em instantes.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber),
            category: DomainErrorCategory.Conflict);

    /// <summary>
    /// Um id de ordem do provedor maior que a coluna não é truncado em silêncio — id cortado
    /// consultaria para sempre uma ordem que não existe (conciliação, cancelamento, comprovante).
    /// Recusar é o único desfecho que não corrompe a referência.
    /// </summary>
    public static DomainException ProviderOrderIdTooLong(
        int maxLength,
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}23",
            messageTemplate: "O identificador da ordem no provedor excede o tamanho máximo ({0} caracteres).",
            parameters: new object[] { maxLength },
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    internal static DomainException SnapshotRequired(
        [CallerFilePath] string filePath = "",
        [CallerMemberName] string memberName = "",
        [CallerLineNumber] int lineNumber = 0)
        => new(
            id: $"{AGGREGATE_PREFIX}16",
            messageTemplate: "Resultado aceito exige o retrato do provedor.",
            parameters: Array.Empty<object>(),
            sourcePath: BuildSourcePath(filePath, memberName, lineNumber));

    private static string BuildSourcePath(string filePath, string memberName, int lineNumber)
        => $"{Path.GetFileName(filePath)}:{lineNumber} ({memberName})";
}
