namespace BillPayment.Domain.Bills;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate.
/// </summary>
/// <remarks>
/// <strong>Contém uma das três travessias de tenant autorizadas do BC.</strong>
/// <see cref="ExistsActiveByDedupKeyAsync"/> pergunta pela chave de instrumento <em>sem</em>
/// filtrar por tenant, de propósito: um compromisso é pago uma vez, e uma caixa de e-mail
/// compartilhada torna a colisão entre tenants provável. Ela devolve <c>bool</c> — nunca o
/// boleto, nunca o tenant dono (ADR-008). Todo o resto filtra por <c>TenantId</c>.
/// </remarks>
public interface IBillRepository
{
    Task AddAsync(Bill bill, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<Bill?> GetAsync(TenantId tenantId, BillId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Travessia autorizada: existe algum boleto ativo com esta chave de instrumento, em
    /// qualquer tenant? Devolve só <c>bool</c>, para o chamador não conseguir identificar
    /// de quem é. "Ativo" exclui <c>Denied</c> e <c>Cancelled</c>, que liberam a chave.
    /// </summary>
    Task<bool> ExistsActiveByDedupKeyAsync(string dedupKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// A mesma travessia, com o detalhe que o check de duplicata precisa: <strong>o id só
    /// volta quando o boleto original é do próprio tenant</strong>. Sendo de outro, a resposta
    /// diz que existe e nada mais — dizer de quem seria vazamento entre contas (ADR-008).
    /// </summary>
    /// <param name="excluding">
    /// O boleto sendo verificado. Ele já está gravado e ocupa a própria chave, então sem
    /// excluí-lo toda validação encontraria a si mesma como duplicata.
    /// </param>
    /// <summary>
    /// O histórico de boletos de um beneficiário, do mais recente para o mais antigo.
    /// </summary>
    /// <remarks>
    /// Alimenta o aprendizado de expectativa, que precisa da cadência das ocorrências. O teto
    /// existe porque a dedução olha para o padrão recente: um beneficiário com anos de histórico
    /// não fica mais previsível por trazer tudo, e a carga cresceria sem limite.
    /// </remarks>
    Task<IReadOnlyCollection<Bill>> ListByPayeeAsync(
        TenantId tenantId,
        Payees.PayeeId payeeId,
        int limit,
        CancellationToken cancellationToken = default);

    Task<DuplicateProbe> ProbeActiveDuplicateAsync(
        string dedupKey,
        TenantId tenantId,
        BillId excluding,
        CancellationToken cancellationToken = default);
}
