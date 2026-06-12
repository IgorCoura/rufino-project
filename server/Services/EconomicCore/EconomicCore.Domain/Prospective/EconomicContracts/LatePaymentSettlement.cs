namespace EconomicCore.Domain.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts.Entities;

/// <summary>
/// Result of <see cref="EconomicContract.MaterializeLatePaymentPenalty"/>: the data a caller needs to compose the
/// bundled cash event that settles a late payment (base track + Penalty track) — primitives only, never domain VOs.
/// <see cref="PenaltyMaterialized"/> is false when an open Penalty pair already existed for the period and was reused.
/// </summary>
public sealed record LatePaymentSettlement(
    CommitmentId BaseCommitmentId,
    decimal BaseAmountValue,
    CommitmentId PenaltyCommitmentId,
    decimal PenaltyAmountValue,
    bool PenaltyMaterialized);
