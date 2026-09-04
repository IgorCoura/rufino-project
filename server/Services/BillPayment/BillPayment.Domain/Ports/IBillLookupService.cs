namespace BillPayment.Domain.Ports;

using BillPayment.Domain.Instruments;
using BillPayment.Domain.Lookups;
using BillPayment.Domain.Secrets;

/// <summary>
/// Consulta oficial de um documento de código de barras na fonte que o emitiu, através do
/// provedor. É o que transforma "esta linha digitável tem DV válido" em "este título existe,
/// e é deste beneficiário, por este valor".
/// </summary>
/// <remarks>
/// <para>
/// <strong>Nunca lança por documento não encontrado.</strong> Ausência de registro é resposta,
/// não falha — ver <see cref="LookupStatus"/>. Exceção aqui significa defeito de programação,
/// não resultado de negócio.
/// </para>
/// <para>
/// <strong>Custa uma credencial perigosa.</strong> O provedor exige permissão de saque na chave
/// para atender esta consulta, mesmo ela sendo read-only (achado da sprint 1.0). Toda chamada
/// desta porta roda com uma credencial capaz de pagar contas — daí a whitelist de IP ser
/// obrigatória, e não recomendada (<c>ADR-001</c>, <c>ADR-009</c>).
/// </para>
/// <para>
/// <strong>A credencial é DO TENANT</strong> (doc 07 — subconta por tenant, desde 2026-08-31):
/// o ponteiro do cofre viaja na chamada, como no <c>IMailboxReader</c>, e é a Infra que o
/// resolve. Nulo — tenant sem chave configurada — degrada para <c>Unavailable</c>, nunca usa
/// chave de outro tenant nem da instalação.
/// </para>
/// </remarks>
public interface IBillLookupService
{
    Task<BillLookupResult> SimulateAsync(
        CredentialRef? credential,
        DigitableLine digitableLine,
        CancellationToken cancellationToken);
}
