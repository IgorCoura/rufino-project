namespace BillPayment.Infra.Asaas;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Ports;

/// <summary>
/// Substitutos usados quando não há credencial do provedor configurada.
/// </summary>
/// <remarks>
/// <para>
/// Devolvem <c>Unavailable</c>, e isso é a parte que importa: a ausência de credencial fica
/// registrada como <strong>"não foi possível verificar"</strong>, jamais como verificação
/// concluída. Um adapter que devolvesse resultado vazio "com sucesso" faria a tela de aprovação
/// mostrar checks pulados como se o documento tivesse passado.
/// </para>
/// <para>
/// Existem para que a aplicação suba e a suíte de integração rode sem segredo — os testes não
/// têm nem devem ter chave capaz de pagar contas.
/// </para>
/// </remarks>
internal sealed class UnconfiguredBillLookupService(TimeProvider clock) : IBillLookupService
{
    public Task<BillLookupResult> SimulateAsync(DigitableLine digitableLine, CancellationToken cancellationToken)
        => Task.FromResult(BillLookupResult.Unavailable(
            UnconfiguredLookup.REASON_CODE, UnconfiguredLookup.MESSAGE, clock.GetUtcNow()));
}

internal sealed class UnconfiguredPixLookupService(TimeProvider clock) : IPixLookupService
{
    public Task<PixLookupResult> DecodeAsync(
        PixPayload payload,
        DateOnly? expectedPaymentDate,
        CancellationToken cancellationToken)
        => Task.FromResult(PixLookupResult.Unavailable(
            UnconfiguredLookup.REASON_CODE, UnconfiguredLookup.MESSAGE, clock.GetUtcNow()));
}

internal static class UnconfiguredLookup
{
    public const string REASON_CODE = "provider_not_configured";
    public const string MESSAGE = "Consulta oficial indisponível: nenhuma credencial do provedor configurada.";
}
