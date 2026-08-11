namespace BillPayment.Domain.CaptureSources;

using BillPayment.Domain.SharedKernel;

/// <summary>
/// Porta de acesso ao Aggregate. Toda busca filtra por <see cref="TenantId"/> — com
/// <strong>uma</strong> exceção, <see cref="IsAddressMonitoredByAnyTenantAsync"/>, que é a
/// primeira das três travessias autorizadas do BC.
/// </summary>
public interface ICaptureSourceRepository
{
    Task AddAsync(CaptureSource source, CancellationToken cancellationToken = default);

    /// <summary>Busca <em>tracked</em>, para mutação. Devolve <c>null</c> se não existir no tenant.</summary>
    Task<CaptureSource?> GetAsync(TenantId tenantId, CaptureSourceId id, CancellationToken cancellationToken = default);

    /// <summary>Pré-condição de unicidade dentro do tenant. O endereço precisa vir normalizado.</summary>
    Task<bool> ExistsAsync(TenantId tenantId, string normalizedAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fontes habilitadas do tenant que o job de sincronização deve varrer, <em>tracked</em> —
    /// o job avança o cursor de cada uma.
    /// </summary>
    Task<IReadOnlyList<CaptureSource>> ListEnabledAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Todas as fontes habilitadas da instalação, <em>tracked</em>, para o worker de
    /// sincronização — que roda fora de qualquer requisição e portanto fora de um tenant.
    /// </summary>
    /// <remarks>
    /// <strong>Não é uma travessia de tenant no sentido do ADR-008.</strong> Nada aqui é devolvido
    /// a um usuário: é o agendador varrendo o próprio trabalho, do mesmo modo que o outbox varre
    /// mensagens pendentes de todos os tenants. O resultado nunca alcança uma resposta de API.
    /// </remarks>
    Task<IReadOnlyList<CaptureSource>> ListEnabledForWorkerAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// <strong>Travessia de tenant autorizada nº 1 (ADR-008).</strong> Responde se algum
    /// <em>outro</em> tenant já monitora este endereço.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Devolve <c>bool</c> e nada mais: sem id, sem nome, sem contagem. Só pode ser chamada
    /// <strong>depois</strong> de o acesso à caixa ter sido provado — perguntar antes
    /// transformaria o endpoint num oráculo para descobrir que endereços estão cadastrados na
    /// plataforma.
    /// </para>
    /// <para>
    /// <paramref name="excludingTenantId"/> existe para que o aviso seja sobre <em>outra</em>
    /// conta: a própria fonte do tenant que está conectando não deve fazer o aviso disparar.
    /// </para>
    /// </remarks>
    Task<bool> IsAddressMonitoredByAnyTenantAsync(
        string normalizedAddress,
        TenantId excludingTenantId,
        CancellationToken cancellationToken = default);

    void Remove(CaptureSource source);
}
