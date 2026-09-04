namespace BillPayment.IntegrationTests.Payments;

using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// A classificação de falha da submissão — a regra que vale dinheiro: só o domínio dizendo
/// "não" é permanente; QUALQUER outra exceção é passageira, porque a retentativa começa pela
/// consulta de <c>externalReference</c> e ADOTA a ordem que já existe no provedor, nunca
/// reenvia. Sem containers.
/// </summary>
public class PaymentSubmissionFailureHandlingTests
{
    // Recusa traduzida pelo domínio: os mesmos dados dão a mesma resposta — permanente.
    [Fact]
    public void IsPermanent_WithADomainRefusal_ShouldBeTrue()
        => Assert.True(PaymentSubmissionFailureHandling.IsPermanent(
            PaymentOrderErrors.ProviderRefusedCancellation("not_cancellable")));

    // BLP.PMO18 é o sinal de "volte para a fila" — passageiro por definição.
    [Fact]
    public void IsPermanent_WithTheSubmissionUnavailableSignal_ShouldBeFalse()
        => Assert.False(PaymentSubmissionFailureHandling.IsPermanent(
            PaymentOrderErrors.SubmissionUnavailable("timeout")));

    // Regressão do pagamento em dobro: timeout de banco no save DEPOIS de o gateway aceitar não
    // pode virar Failed — a reaprovação pagaria duas vezes; a retentativa adota pela referência.
    [Fact]
    public void IsPermanent_WithADbUpdateFailure_ShouldBeFalse()
        => Assert.False(PaymentSubmissionFailureHandling.IsPermanent(
            new DbUpdateException("save falhou", new TimeoutException())));

    // O mesmo vale para qualquer exceção de infraestrutura fora do vocabulário do domínio.
    [Fact]
    public void IsPermanent_WithAnUnknownInfrastructureFailure_ShouldBeFalse()
        => Assert.False(PaymentSubmissionFailureHandling.IsPermanent(
            new InvalidOperationException("pool de conexões esgotado")));

    // Conflito de concorrência é corrida, não recusa — a releitura decide o que fazer.
    [Fact]
    public void IsPermanent_WithAConcurrencyConflict_ShouldBeFalse()
        => Assert.False(PaymentSubmissionFailureHandling.IsPermanent(
            new ConcurrencyConflictException("xmin mudou")));
}
