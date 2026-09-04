namespace BillPayment.UnitTests.CaptureItems;

using BillPayment.Domain.Bills;
using BillPayment.Domain.CaptureItems;
using BillPayment.Domain.SeedWork;
using BillPayment.UnitTests.CaptureItems.Mothers;

/// <summary>
/// Teste de regressão do laço eterno da fila de captura (2026-08-26).
/// </summary>
/// <remarks>
/// O worker tratava toda falha como passageira e devolvia o item à fila indefinidamente. Medido
/// na caixa real: quatro artefatos somaram 1.709 tentativas do mesmo <c>BLP.BIL15</c> — um PDF
/// com dois boletos de naturezas diferentes, que nenhuma repetição transformaria em boleto
/// único. Cada um ocupava em caráter permanente uma das dez vagas do lote, e dez deles parariam
/// a captura inteira sem erro em tela nenhuma.
/// </remarks>
public class CaptureItemRetryTests
{
    private static readonly DateTime Now = CaptureItemMother.DefaultOccurredAt;
    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    private const int MAX_ATTEMPTS = 3;
    private const string DOMAIN_REFUSAL = "[BLP.BIL15] O documento traz códigos de barras de naturezas diferentes.";

    // TESTE ÂNCORA: a recusa determinística do domínio para na primeira vez, sem gastar tentativas.
    // É o caso exato que loopou 1.709 vezes — repetir devolveria a mesma resposta para sempre.
    [Fact]
    public void RecordProcessingFailure_WhenPermanent_ShouldGiveUpImmediately()
    {
        var item = CaptureItemMother.Ingest();

        var gaveUp = item.RecordProcessingFailure(DOMAIN_REFUSAL, permanent: true, MAX_ATTEMPTS, OneMinute, Now);

        Assert.True(gaveUp);
        Assert.Equal(CaptureItemStatus.Failed, item.Status);
        Assert.Equal(CaptureItem.REASON_PROCESSING_REJECTED, item.Reason);
        Assert.Equal(DOMAIN_REFUSAL, item.LastError);
    }

    // Falha passageira mantém o item na fila — retentar é justamente o que resolve rede instável.
    [Fact]
    public void RecordProcessingFailure_WhenTransient_ShouldKeepTheItemInTheQueue()
    {
        var item = CaptureItemMother.Ingest();

        var gaveUp = item.RecordProcessingFailure("TimeoutException: o provedor demorou", false, MAX_ATTEMPTS, OneMinute, Now);

        Assert.False(gaveUp);
        Assert.Equal(CaptureItemStatus.Received, item.Status);
    }

    // A espera da próxima tentativa é gravada no futuro, e é ela que espaça as retentativas:
    // a consulta da fila pula quem tem aluguel vivo, então não existe segundo agendador.
    [Fact]
    public void RecordProcessingFailure_WhenTransient_ShouldPushTheNextAttemptIntoTheFuture()
    {
        var item = CaptureItemMother.Ingest();
        item.Lease(Now.AddMinutes(5), Now);

        item.RecordProcessingFailure("falhou", permanent: false, MAX_ATTEMPTS, OneMinute, Now);

        Assert.NotNull(item.LeaseExpiresAt);
        Assert.True(item.LeaseExpiresAt > Now);
    }

    // A espera dobra a cada tentativa: a 1ª espera 1 min, a 2ª espera 2 min, a 3ª espera 4 min.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    public void RecordProcessingFailure_ShouldDoubleTheWaitOnEachAttempt(int attempts, int expectedMinutes)
    {
        var item = CaptureItemMother.Ingest();
        for (var i = 0; i < attempts; i++)
            item.Lease(Now.AddMinutes(30), Now);

        item.RecordProcessingFailure("falhou", permanent: false, maxAttempts: 99, OneMinute, Now);

        Assert.Equal(Now.AddMinutes(expectedMinutes), item.LeaseExpiresAt);
    }

    // A espera para de dobrar no teto de 30 minutos — senão a oitava tentativa cairia daqui a
    // duas horas, mais do que a folga que um boleto costuma ter até vencer.
    [Fact]
    public void RecordProcessingFailure_ShouldCapTheWait()
    {
        var item = CaptureItemMother.Ingest();
        for (var i = 0; i < 20; i++)
            item.Lease(Now.AddMinutes(30), Now);

        item.RecordProcessingFailure("falhou", permanent: false, maxAttempts: 99, OneMinute, Now);

        Assert.Equal(Now.AddMinutes(30), item.LeaseExpiresAt);
    }

    // Esgotado o teto de tentativas, o item desiste mesmo sendo falha passageira — é o que
    // impede a fila de ficar ocupada para sempre por um item que nunca fecha.
    [Fact]
    public void RecordProcessingFailure_WhenAttemptsAreExhausted_ShouldGiveUp()
    {
        var item = CaptureItemMother.Ingest();
        for (var i = 0; i < MAX_ATTEMPTS; i++)
            item.Lease(Now.AddMinutes(5), Now);

        var gaveUp = item.RecordProcessingFailure("falhou de novo", false, MAX_ATTEMPTS, OneMinute, Now);

        Assert.True(gaveUp);
        Assert.Equal(CaptureItemStatus.Failed, item.Status);
        Assert.Equal(CaptureItem.REASON_ATTEMPTS_EXHAUSTED, item.Reason);
    }

    // Item que já concluiu não volta a falhar: a exceção que chega é o eco de uma execução que
    // perdeu a corrida, e arrastar um Promoted para Failed destruiria o desfecho bom.
    [Fact]
    public void RecordProcessingFailure_OnATerminalItem_ShouldBeANoOp()
    {
        var item = CaptureItemMother.Ingest();
        item.StoreArtifact(CaptureItemMother.DefaultContentHash, CaptureItemMother.DefaultStorageKey, Now);
        item.MarkParsed(ExtractionMethod.EmbeddedText, null, Now);
        item.Promote(CaptureItemMother.DefaultBill, RoutingConfidence.Strong, Now);

        var gaveUp = item.RecordProcessingFailure("eco atrasado", permanent: true, MAX_ATTEMPTS, OneMinute, Now);

        Assert.False(gaveUp);
        Assert.Equal(CaptureItemStatus.Promoted, item.Status);
        Assert.Null(item.LastError);
    }

    // Reabrir dá orçamento novo — senão o item voltaria a Failed na primeira falha, inclusive
    // quando a reabertura foi motivada pela correção que faz o processamento funcionar.
    [Fact]
    public void Reopen_ShouldResetTheAttemptBudget()
    {
        var item = CaptureItemMother.Ingest();
        item.Lease(Now.AddMinutes(5), Now);
        item.RecordProcessingFailure("recusado", permanent: true, MAX_ATTEMPTS, OneMinute, Now);

        item.Reopen(Now.AddHours(1));

        Assert.Equal(CaptureItemStatus.Received, item.Status);
        Assert.Equal(0, item.ProcessingAttempts);
        Assert.Null(item.LastError);
        Assert.Null(item.LeaseExpiresAt);
    }

    // Ceder a vez para a fila da IA é ter avançado: o contador zera porque o orçamento de lá é
    // outro, e gastar chamadas ao extrator com tentativas da faixa rápida seria trocar o recurso
    // escasso pelo barato.
    [Fact]
    public void MarkVisionPending_ShouldResetTheAttemptBudget()
    {
        var item = CaptureItemMother.Ingest();
        item.Lease(Now.AddMinutes(5), Now);
        item.Lease(Now.AddMinutes(5), Now);

        item.MarkVisionPending("awaiting_vision", Now);

        Assert.Equal(CaptureItemStatus.VisionPending, item.Status);
        Assert.Equal(0, item.ProcessingAttempts);
    }

    // Toda transição limpa o aluguel: ele era da fila de onde o item saiu, e carregá-lo faria o
    // item nascer bloqueado na fila seguinte esperando um prazo que não diz respeito a ela.
    [Fact]
    public void Transition_ShouldClearTheLease()
    {
        var item = CaptureItemMother.Ingest();
        item.Lease(Now.AddMinutes(5), Now);

        item.MarkVisionPending("awaiting_vision", Now);

        Assert.Null(item.LeaseExpiresAt);
    }

    // O aluguel conta a tentativa na SAÍDA da fila, não no fim: a falha que derruba o worker
    // antes de escrever qualquer coisa também precisa consumir orçamento, senão um item que
    // mata o processo volta à fila para sempre.
    [Fact]
    public void Lease_ShouldCountTheAttempt()
    {
        var item = CaptureItemMother.Ingest();

        item.Lease(Now.AddMinutes(5), Now);
        item.Lease(Now.AddMinutes(5), Now);

        Assert.Equal(2, item.ProcessingAttempts);
    }

    // Falha de processamento alcança qualquer estado de passagem — ela descreve o worker, não o
    // documento, e estourar pode acontecer em qualquer degrau da cascata.
    [Theory]
    [InlineData(1)]  // Received
    [InlineData(2)]  // Parsed
    [InlineData(3)]  // Locked
    [InlineData(4)]  // LinkPending
    [InlineData(5)]  // LinkFailed
    [InlineData(8)]  // Unrouted
    [InlineData(9)]  // Unrecognized
    [InlineData(11)] // VisionPending
    public void CanTransitionTo_Failed_ShouldBeAllowedFromEveryPipelineStatus(int statusId)
        => Assert.True(Enumeration.FromValue<CaptureItemStatus>(statusId).CanTransitionTo(CaptureItemStatus.Failed));

    // Estado terminal não vira Failed: o item já concluiu, e o desfecho registrado é o que vale.
    [Theory]
    [InlineData(6)]  // Promoted
    [InlineData(7)]  // ForeignPayer
    [InlineData(10)] // Discarded
    public void CanTransitionTo_Failed_ShouldBeRefusedFromTerminalStatuses(int statusId)
        => Assert.False(Enumeration.FromValue<CaptureItemStatus>(statusId).CanTransitionTo(CaptureItemStatus.Failed));

    // Failed não é terminal: sai para Received pelo mesmo Reopen da quarentena, porque falha de
    // processamento é justamente o que uma correção de código costuma resolver.
    [Fact]
    public void Failed_ShouldReopenAndBeDiscardable()
    {
        Assert.False(CaptureItemStatus.Failed.IsTerminal);
        Assert.True(CaptureItemStatus.Failed.CanTransitionTo(CaptureItemStatus.Received));
        Assert.True(CaptureItemStatus.Failed.CanTransitionTo(CaptureItemStatus.Discarded));
    }

    // Failed é estado do funil e não expõe detalhe financeiro — antes do roteamento ninguém sabe
    // de quem é o documento (ADR-008).
    [Fact]
    public void Failed_ShouldNotExposeFinancialDetail()
        => Assert.False(CaptureItemStatus.Failed.ExposesFinancialDetail);

    // Regressão: o download pode falhar também na fila da IA, e a transição faltava — o que
    // transformava um anexo não entregue num item preso em VisionPending para sempre.
    [Fact]
    public void CanTransitionTo_LinkFailed_ShouldBeAllowedFromVisionPending()
        => Assert.True(CaptureItemStatus.VisionPending.CanTransitionTo(CaptureItemStatus.LinkFailed));
}
