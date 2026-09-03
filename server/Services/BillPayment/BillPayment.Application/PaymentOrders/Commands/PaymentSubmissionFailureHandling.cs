namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Domain.SeedWork;

/// <summary>
/// Classifica a falha de uma submissão de pagamento: recusa que se repetirá igual, ou tropeço
/// que a próxima tentativa pode não repetir? Molde do <c>CaptureFailureHandling</c> da captura.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Só o domínio dizendo "não" é permanente.</strong> Uma <c>DomainException</c> é regra
/// avaliando os mesmos dados e chegando à mesma conclusão em toda tentativa — exceto o
/// <c>BLP.PMO18</c>, que descreve a REDE (provedor indisponível) e existe justamente para
/// devolver a ordem à fila.
/// </para>
/// <para>
/// <strong>Todo o resto é passageiro, e aqui isso vale DINHEIRO.</strong> Um timeout de banco no
/// <c>SaveEntitiesAsync</c> DEPOIS de o gateway aceitar, classificado como permanente, viraria
/// <c>Failed</c> num pagamento que o provedor vai executar — o usuário reabriria, aprovaria de
/// novo e pagaria em dobro. Passageiro é o lado seguro por construção: toda retentativa começa
/// pela consulta de <c>externalReference</c> e ADOTA a ordem que já existe lá, nunca reenvia.
/// </para>
/// </remarks>
public static class PaymentSubmissionFailureHandling
{
    /// <summary><c>PaymentOrderErrors.SubmissionUnavailable</c> — "volte para a fila".</summary>
    private const string SUBMISSION_UNAVAILABLE = "BLP.PMO18";

    public static bool IsPermanent(Exception failure)
        => failure is DomainException domain
            && !string.Equals(domain.Id, SUBMISSION_UNAVAILABLE, StringComparison.Ordinal);
}
