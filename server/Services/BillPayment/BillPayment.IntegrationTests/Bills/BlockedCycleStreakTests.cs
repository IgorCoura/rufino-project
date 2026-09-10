namespace BillPayment.IntegrationTests.Bills;

using BillPayment.API.BackgroundServices;

/// <summary>
/// A contagem de ciclos travados da varredura de revalidação.
/// </summary>
/// <remarks>
/// <strong>Sem containers</strong>, como os testes de autorização que não precisam de banco: o
/// assunto aqui é uma decisão de contagem, e montar Postgres para ela só tornaria a suíte mais
/// lenta. Mora nesta suíte, e não na unitária, porque o tipo é da camada API.
/// </remarks>
public sealed class BlockedCycleStreakTests
{
    // TESTE DE REGRESSÃO (2026-09-10): a contagem vivia no worker e era ZERADA em todo ciclo que
    // não achava nada elegível. Ciclo vazio é o desfecho normal assim que o backoff de um boleto
    // passa do intervalo da varredura — o que acontece já na terceira tentativa —, então o
    // contador nunca passava de 2, o teto de alerta era inalcançável, e uma indisponibilidade
    // longa corria sem que ninguém fosse avisado.
    [Fact]
    public void Record_WithIdleCyclesInBetween_ShouldKeepCountingTheBlockedOnes()
    {
        var streak = new BlockedCycleStreak(alertThreshold: 3);

        Assert.False(streak.Record(claimedCount: 20, resolvedAny: false));
        Assert.False(streak.Record(claimedCount: 0, resolvedAny: false));
        Assert.False(streak.Record(claimedCount: 0, resolvedAny: false));
        Assert.False(streak.Record(claimedCount: 5, resolvedAny: false));

        Assert.True(streak.Record(claimedCount: 5, resolvedAny: false));
        Assert.Equal(3, streak.Cycles);
    }

    // Fila vazia não é ciclo travado: ela não conta nem zera, e sozinha nunca alerta.
    [Fact]
    public void Record_WithOnlyIdleCycles_ShouldNeverAlert()
    {
        var streak = new BlockedCycleStreak(alertThreshold: 1);

        Assert.False(streak.Record(claimedCount: 0, resolvedAny: false));
        Assert.Equal(0, streak.Cycles);
    }

    // A consulta voltando zera a contagem — é o desfecho que o alerta existe para esperar.
    [Fact]
    public void Record_WhenSomethingResolves_ShouldResetTheStreak()
    {
        var streak = new BlockedCycleStreak(alertThreshold: 2);

        streak.Record(claimedCount: 10, resolvedAny: false);
        Assert.False(streak.Record(claimedCount: 10, resolvedAny: true));

        Assert.Equal(0, streak.Cycles);
        Assert.False(streak.Record(claimedCount: 10, resolvedAny: false));
    }

    // Passado o teto, o alerta continua saindo a cada ciclo travado — a queda não melhorou.
    [Fact]
    public void Record_PastTheThreshold_ShouldKeepAlerting()
    {
        var streak = new BlockedCycleStreak(alertThreshold: 2);

        streak.Record(claimedCount: 1, resolvedAny: false);

        Assert.True(streak.Record(claimedCount: 1, resolvedAny: false));
        Assert.True(streak.Record(claimedCount: 1, resolvedAny: false));
    }
}
