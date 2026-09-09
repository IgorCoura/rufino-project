namespace BillPayment.Domain.PayerProfiles;

using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// O tenant vinculou (ou trocou) a chave da conta de pagamento dele.
/// </summary>
/// <remarks>
/// <para>
/// Dois consumidores, e os dois precisam acontecer FORA da transação do vínculo: destravar as
/// ordens que esperavam conta, e provisionar o webhook daquela conta no provedor. O segundo é
/// chamada externa — prendê-la à transação faria o tenant não conseguir cadastrar a chave sempre
/// que o provedor estivesse fora do ar, por um passo que não é o dele.
/// </para>
/// <para>
/// Emitido também na TROCA de chave, de propósito: chave nova é conta possivelmente nova, e o
/// webhook da anterior não vale para ela.
/// </para>
/// </remarks>
public sealed record AsaasAccountLinkedDomainEvent(
    PayerProfileId PayerProfileId,
    TenantId TenantId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
