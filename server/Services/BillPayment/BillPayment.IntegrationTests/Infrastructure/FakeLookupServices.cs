namespace BillPayment.IntegrationTests.Infrastructure;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;

/// <summary>
/// Consulta oficial determinística para os testes de fluxo.
/// </summary>
/// <remarks>
/// <para>
/// O que está sob teste aqui é a cadeia captura → outbox → validação → persistência, e a
/// resposta do provedor é <em>entrada</em> desse fluxo, não parte dele. A tradução da resposta
/// real já é exercitada pelos testes dos adapters, com um transporte de teste.
/// </para>
/// <para>
/// Registrado como singleton e com <see cref="Reset"/>: a coleção de integração roda em série,
/// então cada teste arma a resposta que precisa e limpa depois — do mesmo jeito que o Respawn
/// faz com o banco.
/// </para>
/// </remarks>
internal sealed class FakeLookupServices : IBillLookupService, IPixLookupService
{
    public BillLookupResult? BankSlipResult { get; set; }

    public PixLookupResult? PixResult { get; set; }

    /// <summary>Quantas vezes o boleto foi consultado — prova que a revalidação consultou de novo.</summary>
    public int BankSlipCallCount { get; private set; }

    public void Reset()
    {
        BankSlipResult = null;
        PixResult = null;
        BankSlipCallCount = 0;
    }

    public Task<BillLookupResult> SimulateAsync(DigitableLine digitableLine, CancellationToken cancellationToken)
    {
        BankSlipCallCount++;

        return Task.FromResult(BankSlipResult
            ?? BillLookupResult.Unavailable("not_configured_in_test", null, DateTimeOffset.UnixEpoch));
    }

    public Task<PixLookupResult> DecodeAsync(
        PixPayload payload,
        DateOnly? expectedPaymentDate,
        CancellationToken cancellationToken)
        => Task.FromResult(PixResult
            ?? PixLookupResult.Unavailable("not_configured_in_test", null, DateTimeOffset.UnixEpoch));
}
