namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Ports;

/// <summary>
/// Prova de chave determinística: cada teste arma o desfecho e confere qual chave chegou.
/// A tradução da resposta real do provedor é exercitada nos testes do adapter, com transporte
/// falso — aqui o que está sob teste é o fluxo prova → cofre → perfil.
/// </summary>
internal sealed class FakePaymentAccountVerifier : IPaymentAccountVerifier
{
    /// <summary>O desfecho da próxima prova.</summary>
    public PaymentAccountProbe NextProbe { get; set; } = PaymentAccountProbe.Ok();

    /// <summary>A última chave apresentada — prova que a chave crua chegou à prova.</summary>
    public string? LastApiKey { get; private set; }

    public void Reset()
    {
        NextProbe = PaymentAccountProbe.Ok();
        LastApiKey = null;
    }

    public Task<PaymentAccountProbe> ProbeAsync(string apiKey, CancellationToken cancellationToken)
    {
        LastApiKey = apiKey;
        return Task.FromResult(NextProbe);
    }
}
