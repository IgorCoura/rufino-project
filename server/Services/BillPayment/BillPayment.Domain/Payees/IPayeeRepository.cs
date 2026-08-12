namespace BillPayment.Domain.Payees;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/>, com a única
/// exceção de <see cref="IsRegisteredByAnotherTenantAsync"/> — a terceira travessia autorizada
/// do BC (ADR-008).
/// </summary>
public interface IPayeeRepository
{
    Task AddAsync(Payee payee, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<Payee?> GetAsync(TenantId tenantId, PayeeId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade por (tenant, documento). O documento é a identidade estável do beneficiário.</summary>
    Task<bool> ExistsAsync(TenantId tenantId, TaxId taxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Todos os beneficiários do tenant, para a resolução do beneficiário consultado.
    /// </summary>
    /// <remarks>
    /// Traz <strong>inclusive os inativos</strong>: beneficiário desativado que volta a emitir
    /// boleto precisa reprovar por <c>payee_inactive</c>, e não passar por "não cadastrado".
    /// A carga completa é o que a detecção de sósia exige — ela compara o nome consultado
    /// contra o conjunto inteiro, não contra um candidato já escolhido.
    /// </remarks>
    Task<IReadOnlyCollection<Payee>> ListByTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// <strong>Travessia de tenant autorizada nº 3 (ADR-008).</strong> Diz se algum tenant
    /// <em>diferente</em> de <paramref name="excludingTenantId"/> cadastrou este beneficiário.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sustenta o degrau 3 da escada de roteamento: um beneficiário que só este tenant cadastrou
    /// atribui o boleto a ele; um que dois tenants cadastraram não atribui nada, porque a
    /// evidência é ambígua e escolher seria adivinhar de quem é a conta.
    /// </para>
    /// <para>
    /// <strong>Devolve <c>bool</c> e nada mais</strong> — não existe projeção de onde tirar id,
    /// nome ou contagem do outro tenant. Trocar por <c>CountAsync</c> ou acrescentar um
    /// <c>Select</c> transforma a travessia em vazamento e viola o ADR-008.
    /// </para>
    /// <para>
    /// <strong>Ocupou o lugar da travessia que o doc 07 previa</strong>
    /// (<c>IRoutingRuleRepository.ExistsForPairInAnyTenantAsync</c>): a medição de 2026-08-12
    /// mostrou que a referência de conta que aquela regra usaria não distingue pagadores, e o
    /// Aggregate <c>RoutingRule</c> deixou de ser criado. Ver <c>BillRoutingService</c>.
    /// </para>
    /// </remarks>
    Task<bool> IsRegisteredByAnotherTenantAsync(
        TenantId excludingTenantId,
        TaxId taxId,
        CancellationToken cancellationToken = default);

    void Remove(Payee payee);
}
