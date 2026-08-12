namespace BillPayment.Application.Models.Expectations;

using BillPayment.Application.Expectations.Commands;

/// <summary>
/// Corpo do cadastro de expectativa.
/// </summary>
/// <remarks>
/// O <c>tenantId</c> não entra aqui: ele vem da rota, e aceitá-lo no corpo permitiria cadastrar
/// expectativa em nome de outra conta — o vetor de IDOR que todo Model do BC fecha.
/// </remarks>
public sealed record RegisterBillExpectationModel(
    Guid PayeeId,
    string? AccountReference,
    string Label,
    string Recurrence,
    int ExpectedDueDay,
    int ObservedLeadDays,
    int? AlertLeadDays)
{
    public RegisterBillExpectationCommand ToCommand(Guid tenantId)
        => new(tenantId, PayeeId, AccountReference, Label, Recurrence, ExpectedDueDay, ObservedLeadDays, AlertLeadDays);
}

/// <param name="PausedUntil">
/// Preenchido pausa até a data; nulo com <c>IsActive</c> verdadeiro retoma o monitoramento.
/// </param>
public sealed record AlterBillExpectationWatchModel(bool IsActive, DateOnly? PausedUntil, string? Reason)
{
    public AlterBillExpectationWatchCommand ToCommand(Guid tenantId, Guid expectationId)
        => new(tenantId, expectationId, IsActive, PausedUntil, Reason);
}

public sealed record WaiveExpectationCycleModel(string? Reason)
{
    public WaiveExpectationCycleCommand ToCommand(Guid tenantId, Guid expectationId, Guid cycleId, Guid userId)
        => new(tenantId, expectationId, cycleId, userId, Reason);
}
