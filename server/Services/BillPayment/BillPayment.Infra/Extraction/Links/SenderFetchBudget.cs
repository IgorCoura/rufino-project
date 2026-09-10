namespace BillPayment.Infra.Extraction.Links;

using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

/// <summary>
/// O teto diário de buscas por remetente, no regime aberto.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O orçamento por mensagem limita um e-mail; este limita mil.</strong> Sem ele, quem
/// consegue mandar e-mail para a caixa consegue gastar a rede, a fila e a cota de IA de todos os
/// tenants — que é o achado A9 (filas sem justiça) visto pelo lado da entrada. E o custo do
/// atacante é zero: mandar mil e-mails é barato, buscar doze endereços por mensagem não é.
/// </para>
/// <para>
/// <strong>Singleton e em memória, no mesmo molde do <c>ExtractionBudget</c></strong> — e com a
/// mesma limitação registrada lá: o contador zera ao reiniciar a API e não é compartilhado entre
/// instâncias. Aceitável enquanto há um deployment só; ao escalar horizontalmente, cada instância
/// terá seu próprio teto e o efetivo será o múltiplo. É defesa contra abuso acidental e contra o
/// atacante comum, não contra quem sabe exatamente quantas instâncias existem.
/// </para>
/// </remarks>
internal sealed class SenderFetchBudget(IOptions<LinkResolutionOptions> options, TimeProvider clock)
{
    private readonly ConcurrentDictionary<string, Counter> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkResolutionOptions _options = options.Value;

    /// <summary>
    /// Reserva uma busca para este remetente, ou recusa.
    /// </summary>
    /// <remarks>
    /// Remetente nulo dispensa o teto: é o artefato que não veio de caixa de e-mail (anexo manual,
    /// reprocessamento), onde há uma pessoa por trás de cada chamada.
    /// </remarks>
    public bool TryReserve(string? sender)
    {
        if (_options.MaxFetchesPerSenderPerDay <= 0 || string.IsNullOrWhiteSpace(sender))
            return true;

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);

        var counter = _counters.AddOrUpdate(
            sender.Trim(),
            _ => new Counter(today, 1),
            (_, current) => current.Day == today
                ? current with { Used = current.Used + 1 }
                : new Counter(today, 1));

        // Podar o que é de ontem evita o dicionário crescer com um remetente por mensagem recebida.
        if (_counters.Count > MAX_TRACKED_SENDERS)
            PruneStale(today);

        return counter.Used <= _options.MaxFetchesPerSenderPerDay;
    }

    private const int MAX_TRACKED_SENDERS = 10_000;

    private void PruneStale(DateOnly today)
    {
        foreach (var (sender, counter) in _counters)
        {
            if (counter.Day != today)
                _counters.TryRemove(sender, out _);
        }
    }

    private readonly record struct Counter(DateOnly Day, int Used);
}
