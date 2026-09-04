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
/// <para>
/// <strong>Reivindica, e por isso escreve — apesar do nome.</strong> Listar e marcar têm de ser
/// o mesmo passo: em dois passos, dois workers leem a mesma linha antes de qualquer um marcar,
/// e o mesmo artefato é processado duas vezes. Foi o que produziu os <c>BLP.CPI03</c> de
/// <c>Promoted → Parsed</c> observados em 2026-08-26. A escrita é um <c>UPDATE</c> só, sem
/// agregado carregado e sem regra de negócio — é reserva de trabalho, não mutação de domínio.
/// </para>
/// </remarks>
public interface ICaptureItemWorkQueries
{
    /// <summary>
    /// Reivindica os próximos artefatos à espera de processamento, dos mais antigos aos mais novos.
    /// </summary>
    /// <param name="leaseUntil">Até quando os itens reivindicados ficam fora do alcance de outro worker.</param>
    /// <remarks>
    /// Ordem por chegada porque boleto tem vencimento: o que entrou primeiro é o que está mais
    /// perto de vencer, e deixá-lo atrás numa fila cheia é como se perde prazo.
    /// </remarks>
    Task<IReadOnlyList<PendingCaptureItem>> ClaimPendingAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reivindica da fila do extrator de IA: artefatos que a cascata determinística não resolveu.
    /// </summary>
    /// <remarks>
    /// <strong>Fila separada porque o ritmo é outro.</strong> O item comum leva 150 ms; o de visão
    /// leva de 3 a 5 segundos e disputa uma cota limitada por minuto. Na mesma fila, os poucos
    /// lentos seguravam todos os rápidos — medido em 2026-08-26: 27% dos itens, 86% do tempo.
    /// O aluguel aqui é mais longo pelo mesmo motivo.
    /// </remarks>
    Task<IReadOnlyList<PendingCaptureItem>> ClaimPendingVisionAsync(
        int limit,
        DateTimeOffset leaseUntil,
        CancellationToken cancellationToken = default);
}

/// <param name="TenantId">Reconstitui o escopo do comando — o worker não tem requisição de onde tirá-lo.</param>
public sealed record PendingCaptureItem(Guid TenantId, Guid CaptureItemId);
