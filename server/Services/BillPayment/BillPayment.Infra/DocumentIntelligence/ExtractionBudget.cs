namespace BillPayment.Infra.DocumentIntelligence;

using System.Collections.Concurrent;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Segura o ritmo e o volume das chamadas ao extrator — antes de gastá-las.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Existe porque o custo do erro é assimétrico.</strong> Uma caixa antiga recém-conectada
/// traz centenas de anexos de uma vez — medido: 404 na primeira varredura, dos quais 95 chegariam
/// ao extrator. Sem teto, isso é conta surpresa; na conta gratuita, é a cota do dia queimada em
/// minutos e a captura parada até amanhã.
/// </para>
/// <para>
/// <strong>Segurar aqui é mais barato do que descobrir do outro lado.</strong> Estourar o limite
/// de taxa devolve <c>429</c>, e retentar em cima piora — o provedor passa a recusar por mais
/// tempo. O intervalo mínimo é respeitado <em>antes</em> de a requisição sair.
/// </para>
/// <para>
/// <strong>O teto é por tenant.</strong> Um cliente com caixa grande não pode consumir a cota que
/// impediria os outros de capturar — é o mesmo princípio do isolamento que vale no resto do BC.
/// </para>
/// <para>
/// Singleton: o contador só significa alguma coisa se for o mesmo entre requisições.
/// </para>
/// </remarks>
internal sealed class ExtractionBudget(TimeProvider clock, ILogger<ExtractionBudget> logger) : IDisposable
{
    private readonly ConcurrentDictionary<(Guid Tenant, DateOnly Day), int> _spent = new();
    private readonly SemaphoreSlim _gate = new(1, 1);

    private DateTimeOffset _lastCall = DateTimeOffset.MinValue;

    /// <summary>
    /// Reserva uma chamada: espera o intervalo mínimo e devolve <c>false</c> se o teto do dia
    /// acabou.
    /// </summary>
    /// <remarks>
    /// A reserva é feita <strong>antes</strong> da chamada e não é devolvida se ela falhar. É
    /// deliberado: uma requisição que estourou no provedor consumiu cota lá também, e devolver o
    /// crédito aqui transformaria falha em laço.
    /// </remarks>
    public async Task<bool> TryReserveAsync(
        TenantId tenantId,
        int maxPerDay,
        int minIntervalMs,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var key = (tenantId.Value, today);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var spent = _spent.GetValueOrDefault(key);
            if (spent >= maxPerDay)
            {
                // Aviso, não erro: a captura segue, o artefato vai para a quarentena, e amanhã
                // ele volta a ser candidato. Falhar alto aqui pararia a ingestão inteira.
                logger.LogWarning(
                    "Teto diário de extração por IA atingido ({Spent}/{Max}). Os artefatos de hoje vão para a quarentena.",
                    spent,
                    maxPerDay);

                return false;
            }

            var since = clock.GetUtcNow() - _lastCall;
            var wait = TimeSpan.FromMilliseconds(minIntervalMs) - since;

            if (wait > TimeSpan.Zero)
                await Task.Delay(wait, clock, cancellationToken);

            _lastCall = clock.GetUtcNow();
            _spent[key] = spent + 1;

            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Quantas chamadas este tenant já gastou hoje. Para métrica e diagnóstico.</summary>
    public int SpentToday(TenantId tenantId)
        => _spent.GetValueOrDefault((tenantId.Value, DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)));

    public void Dispose() => _gate.Dispose();
}
