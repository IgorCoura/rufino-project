namespace BillPayment.Application.Queries.CaptureItems;

/// <summary>
/// Fila de trabalho do processador de artefatos.
/// </summary>
/// <remarks>
/// <para>
/// Separada de <c>ICaptureItemQueries</c> de propósito: aquela serve a uma tela e projeta com o
/// filtro de visibilidade do ADR-008; esta serve a um worker e devolve <strong>só ids</strong>.
/// Reaproveitar a de leitura faria o agendador carregar projeção de tela a cada ciclo, e — pior
/// — abriria caminho para alguém expor um DTO de worker numa resposta de API.
/// </para>
/// <para>
/// <strong>Atravessa tenants</strong>, como a varredura de caixas: o worker roda fora de
/// requisição, e o par <c>(tenant, item)</c> que ele devolve é o que reconstitui o escopo no
/// comando seguinte. Nada aqui alcança uma resposta de API.
/// </para>
/// </remarks>
public interface ICaptureItemWorkQueries
{
    /// <summary>
    /// Itens à espera de processamento, dos mais antigos para os mais novos.
    /// </summary>
    /// <remarks>
    /// Ordem por chegada porque boleto tem vencimento: o que entrou primeiro é o que está mais
    /// perto de vencer, e deixá-lo atrás numa fila cheia é como se perde prazo.
    /// </remarks>
    Task<IReadOnlyList<PendingCaptureItem>> ListPendingAsync(int limit, CancellationToken cancellationToken = default);
}

/// <param name="TenantId">Reconstitui o escopo do comando — o worker não tem requisição de onde tirá-lo.</param>
public sealed record PendingCaptureItem(Guid TenantId, Guid CaptureItemId);
