namespace BillPayment.API.BackgroundServices;

/// <summary>
/// Conta os ciclos seguidos em que a varredura <strong>tentou e nada resolveu</strong>.
/// </summary>
/// <remarks>
/// <para>
/// Existe como tipo próprio por causa do defeito que ele corrige. A contagem vivia no worker e
/// era <strong>zerada em todo ciclo que não achava nada elegível</strong> — e ciclo vazio é o
/// desfecho NORMAL assim que o backoff dos boletos passa do intervalo da varredura, o que
/// acontece já na terceira tentativa. O contador nunca passava de 2, o teto de alerta era
/// inalcançável, e uma indisponibilidade longa corria sem que ninguém fosse avisado: exatamente
/// a falha silenciosa que o resto do BC existe para evitar.
/// </para>
/// <para>
/// <strong>Fila vazia não é ciclo travado.</strong> Só zera quem resolve; só conta quem tentou.
/// </para>
/// </remarks>
internal sealed class BlockedCycleStreak(int alertThreshold)
{
    private int _cycles;

    /// <summary>Quantos ciclos seguidos tentaram sem sucesso.</summary>
    public int Cycles => _cycles;

    /// <summary>
    /// Registra o desfecho de um ciclo e diz se é hora de gritar.
    /// </summary>
    /// <param name="claimedCount">Quantos boletos o ciclo reivindicou. Zero é fila vazia.</param>
    /// <param name="resolvedAny">Alguma consulta respondeu neste ciclo.</param>
    public bool Record(int claimedCount, bool resolvedAny)
    {
        if (claimedCount == 0)
            return false;

        if (resolvedAny)
        {
            _cycles = 0;
            return false;
        }

        return ++_cycles >= alertThreshold;
    }
}
