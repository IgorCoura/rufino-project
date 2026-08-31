namespace BillPayment.Domain.Ports;

/// <summary>
/// Prova que uma chave de API do provedor de pagamento funciona, ANTES de ela entrar no cofre —
/// mesma doutrina da prova de acesso à caixa: chave errada é recusada na hora, não meses depois
/// numa consulta oficial. A falha é modelada, nunca lançada (como <c>IMailboxReader</c> e
/// <c>IBillLookupService</c>): colapsar recusa e indisponibilidade faria uma queda de rede
/// parecer chave errada.
/// </summary>
public interface IPaymentAccountVerifier
{
    /// <summary>Chama um endpoint barato e read-only do provedor com a chave crua.</summary>
    Task<PaymentAccountProbe> ProbeAsync(string apiKey, CancellationToken cancellationToken);
}

/// <summary>
/// O desfecho da prova. <see cref="IsRetryable"/> separa "o provedor recusou a chave"
/// (configuração errada, insistir não resolve) de "o provedor não respondeu" (tentar de novo).
/// </summary>
public sealed record PaymentAccountProbe(bool IsOk, string? ReasonCode, bool IsRetryable)
{
    public static PaymentAccountProbe Ok() => new(true, null, false);

    public static PaymentAccountProbe Rejected(string reasonCode) => new(false, reasonCode, false);

    public static PaymentAccountProbe Unavailable(string reasonCode) => new(false, reasonCode, true);
}
