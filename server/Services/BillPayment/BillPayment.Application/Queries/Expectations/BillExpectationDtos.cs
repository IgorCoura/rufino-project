namespace BillPayment.Application.Queries.Expectations;

/// <param name="Status">O nome do <c>CycleStatus</c>.</param>
/// <param name="MissReason">Nulo quando o ciclo não falhou.</param>
/// <param name="Arrived">
/// Se o documento chegou. É o que separa os dois avisos — "não chegou, vá buscar" de "chegou e
/// não consegui ler, resolva o item" —, e por isso vem projetado em vez de deduzido na tela.
/// </param>
public sealed record ExpectationCycleDto(
    Guid Id,
    string Competence,
    DateOnly ExpectedDueDate,
    DateOnly AlertAt,
    string Status,
    string? MissReason,
    bool? Arrived,
    Guid? FulfilledByBillId,
    Guid? BlockedByCaptureItemId,
    string? LastAlertLevel);

public sealed record BillExpectationDto(
    Guid Id,
    Guid PayeeId,
    string AccountReference,
    string Label,
    string Recurrence,
    int ExpectedDueDay,
    int ObservedLeadDays,
    int AlertLeadDays,
    string Origin,
    int ObservationCount,
    bool IsActive,
    DateOnly? PausedUntil,
    IReadOnlyCollection<ExpectationCycleDto> Cycles);

public sealed record BillExpectationPage(IReadOnlyCollection<BillExpectationDto> Items, string? NextCursor);

/// <summary>
/// O painel de pendências: o que está atrasado, o que falhou na captura e o que vence em breve.
/// </summary>
/// <remarks>
/// <strong>É a tela que torna a rede de segurança utilizável mesmo sem canal de aviso externo.</strong>
/// Enquanto o adapter de e-mail não existir, é aqui que o alerta aparece — e ele existe porque o
/// registro do alerta vive no agregado, não no canal.
/// </remarks>
public sealed record PendingExpectationDto(
    Guid ExpectationId,
    Guid CycleId,
    string Label,
    string Competence,
    DateOnly ExpectedDueDate,
    string Status,
    string? MissReason,
    bool? Arrived,
    Guid? BlockedByCaptureItemId,
    string? LastAlertLevel);

public sealed record PendingExpectationsView(
    IReadOnlyCollection<PendingExpectationDto> Missing,
    IReadOnlyCollection<PendingExpectationDto> CaptureFailed,
    IReadOnlyCollection<PendingExpectationDto> DueSoon);
