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
/// <param name="IsOverdue">
/// O vencimento previsto já passou. Vem projetado porque a tela precisa dele nas três listas —
/// um artefato travado também pode estar vencido —, e recalcular "hoje" no cliente faria a tela
/// discordar do servidor na virada do dia.
/// </param>
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
    string? LastAlertLevel,
    bool IsOverdue);

/// <summary>
/// O painel de pendências, separado pela ação que cada caso pede.
/// </summary>
/// <remarks>
/// <strong><c>Missing</c> e <c>Overdue</c> são a mesma falha em dois momentos, e separá-las é
/// regra de produto.</strong> Antes do vencimento a ação é "busque no portal, ainda dá tempo";
/// depois dele há encargos correndo, e misturar as duas na mesma lista faz a segunda se perder
/// no meio da primeira — que é como uma rede de segurança deixa de ser lida.
/// </remarks>
public sealed record PendingExpectationsView(
    IReadOnlyCollection<PendingExpectationDto> Missing,
    IReadOnlyCollection<PendingExpectationDto> Overdue,
    IReadOnlyCollection<PendingExpectationDto> CaptureFailed,
    IReadOnlyCollection<PendingExpectationDto> DueSoon);
