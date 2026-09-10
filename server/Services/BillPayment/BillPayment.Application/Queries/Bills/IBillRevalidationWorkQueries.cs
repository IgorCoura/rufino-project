namespace BillPayment.Application.Queries.Bills;

/// <param name="TenantId">Acompanha o id porque o worker não age em nome de nenhum usuário.</param>
/// <param name="Attempts">Quantas tentativas o boleto já acumulou, para o log dizer há quanto tempo insiste.</param>
public sealed record PendingBillRevalidation(Guid TenantId, Guid BillId, int Attempts);

/// <summary>
/// A fila dos boletos que ficaram sem consulta oficial e precisam ser reconsultados.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque a ausência de consulta passou a valer Extremo Perigo</strong>
/// (2026-09-10). Sem esta varredura, o aviso "revalide mais tarde" seria trabalho manual que
/// ninguém faz, e o boleto ficaria na alçada máxima por um incidente que já terminou.
/// </para>
/// <para>
/// <strong>Não filtra por tenant e não é travessia.</strong> Mesma natureza da fila de leitura e
/// da varredura de caixas: o worker não age em nome de ninguém e não projeta nada para tela. O
/// <c>TenantId</c> vem junto e acompanha todo efeito que sair daqui.
/// </para>
/// </remarks>
public interface IBillRevalidationWorkQueries
{
    /// <summary>
    /// Reivindica boletos parados sem consulta, contando a tentativa e adiando a próxima no mesmo
    /// passo.
    /// </summary>
    /// <remarks>
    /// <strong>Escolher e marcar têm de ser um passo só</strong> — em dois, dois workers leem a
    /// mesma linha antes de qualquer um marcar. E a tentativa é contada na SAÍDA, não no fim do
    /// processamento: contar só as falhas registradas deixaria de fora a pior delas, a que derruba
    /// o worker antes de escrever qualquer coisa, e o boleto voltaria sem espera nenhuma.
    /// </remarks>
    /// <param name="baseDelay">Espera-base, dobrada a cada tentativa já acumulada.</param>
    /// <param name="maxDelay">Teto da espera. Sem ele o backoff exponencial adia para nunca.</param>
    Task<IReadOnlyList<PendingBillRevalidation>> ClaimStaleWithoutLookupAsync(
        int limit,
        TimeSpan baseDelay,
        TimeSpan maxDelay,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Devolve à fila, agora, os boletos de UM tenant que ficaram sem consulta por falta de chave.
    /// </summary>
    /// <remarks>
    /// Chamado quando o tenant vincula a conta do provedor. Esses boletos não dependem do tempo —
    /// dependiam de um cadastro que acabou de acontecer —, então esperar o backoff correr seria
    /// deixá-los em Extremo Perigo por uma condição que já foi resolvida.
    /// </remarks>
    Task<int> ReleaseNotConfiguredAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
