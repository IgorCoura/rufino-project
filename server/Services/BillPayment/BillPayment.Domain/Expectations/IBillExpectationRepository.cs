namespace BillPayment.Domain.Expectations;

using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.CaptureSources;
using BillPayment.Domain.Payees;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — este repositório
/// não tem nenhuma das três travessias de tenant autorizadas do BC.
/// </summary>
public interface IBillExpectationRepository
{
    Task AddAsync(BillExpectation expectation, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<BillExpectation?> GetAsync(
        TenantId tenantId, BillExpectationId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade por (tenant, beneficiário, referência de conta).</summary>
    /// <param name="excluding">
    /// A expectativa que está sendo editada, ignorada na conferência. Sem ela, uma edição que não
    /// mexe na referência colidiria com a própria linha — e o índice único do banco devolveria
    /// erro cru no lugar do <c>BLP.EXP01</c>.
    /// </param>
    Task<bool> ExistsAsync(
        TenantId tenantId,
        PayeeId payeeId,
        string accountReference,
        BillExpectationId? excluding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// As expectativas que o beneficiário responde, para o casamento de um boleto que chegou.
    /// </summary>
    Task<IReadOnlyCollection<BillExpectation>> ListByPayeeAsync(
        TenantId tenantId, PayeeId payeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// As expectativas que costumam receber conta por aquela fonte.
    /// </summary>
    /// <remarks>
    /// Serve ao alerta de "chegou e não consegui ler": um artefato travado não tem beneficiário
    /// nem vencimento, e a fonte por onde ele entrou é a única coisa que o liga a uma conta
    /// esperada. Ver <c>ExpectationCaptureMatchingService</c>.
    /// </remarks>
    Task<IReadOnlyCollection<BillExpectation>> ListByHintSourceAsync(
        TenantId tenantId, CaptureSourceId sourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// As expectativas cujo ciclo aberto aponta para aquele artefato travado.
    /// </summary>
    /// <remarks>
    /// É o caminho de volta: o item foi reprovado, reaberto ou virou boleto, e o ciclo precisa
    /// deixar de apontar para ele. Sem isto o painel continuaria mandando resolver o que já foi.
    /// </remarks>
    Task<IReadOnlyCollection<BillExpectation>> ListByBlockedCaptureItemAsync(
        TenantId tenantId, CaptureItemId itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// A fila do job: expectativas ativas de todos os tenants ainda não varridas neste ciclo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Não é travessia de tenant.</strong> Quem chama é o worker, que não age em nome de
    /// nenhum usuário e não projeta nada para tela — mesma natureza da varredura de caixas. O
    /// <c>TenantId</c> continua em cada agregado e acompanha todo efeito que sair daqui.
    /// </para>
    /// <para>
    /// <strong>O corte é por <c>LastSweptAt</c>, não por <c>UpdatedAt</c>.</strong> Ordenar pela
    /// atualização de negócio invertia a prioridade da fila: expectativa parada mantinha o carimbo
    /// antigo e ocupava as vagas do lote para sempre, enquanto a que estava sendo cumprida ia para
    /// o fim. Com o carimbo de varredura — gravado em toda passagem — o lote deixa de ser teto de
    /// cobertura e volta a ser só tamanho de transação: o worker pede lotes até a fila secar.
    /// </para>
    /// </remarks>
    /// <param name="notSweptSince">Instante em que o ciclo de varredura começou.</param>
    Task<IReadOnlyCollection<BillExpectation>> ListActiveForSweepAsync(
        int batchSize, DateTime notSweptSince, CancellationToken cancellationToken = default);

    void Remove(BillExpectation expectation);
}
