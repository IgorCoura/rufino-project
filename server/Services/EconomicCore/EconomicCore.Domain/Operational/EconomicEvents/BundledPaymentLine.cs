namespace EconomicCore.Domain.Operational.EconomicEvents;

/// <summary>
/// One primitive line of a bundled payment: which covering commitment (root contract + commitment) and how much.
/// The <see cref="EconomicEvent.RegisterBundledPayment"/> factory composes the domain Value Objects from these,
/// keeping callers (Application) free of domain-type assembly.
/// </summary>
public sealed record BundledPaymentLine(Guid ContractId, Guid CommitmentId, decimal Amount);
