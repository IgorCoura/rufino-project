namespace BillPayment.Application.Queries.Bills;

/// <param name="TenantId">Acompanha o id porque o worker não age em nome de nenhum usuário.</param>
public sealed record PendingBillReading(Guid TenantId, Guid BillId);

/// <summary>
/// A fila da análise por IA dos boletos. <strong>Separada da query de tela</strong>, como a fila
/// de captura: quem lê aqui é um worker, e o que ele precisa são ids, não projeção.
/// </summary>
/// <remarks>
/// <strong>Não filtra por tenant e não é travessia.</strong> Mesma natureza da varredura de
/// caixas e da fila de captura: o worker não age em nome de ninguém e não projeta nada para tela.
/// O <c>TenantId</c> vem junto e acompanha todo efeito que sair daqui.
/// </remarks>
public interface IBillReadingWorkQueries
{
    /// <summary>
    /// Reivindica boletos que esperam análise, marcando o aluguel no mesmo passo.
    /// </summary>
    /// <remarks>
    /// <strong>Escolher e marcar têm de ser um passo só.</strong> Em dois passos, dois workers
    /// leem a mesma linha antes de qualquer um marcar — foi a origem dos conflitos de estado
    /// observados na fila de captura, e a correção lá foi exatamente esta.
    /// </remarks>
    Task<IReadOnlyList<PendingBillReading>> ClaimPendingReadingsAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default);
}
