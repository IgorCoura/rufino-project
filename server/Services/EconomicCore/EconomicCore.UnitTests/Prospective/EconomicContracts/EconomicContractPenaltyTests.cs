namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

public class EconomicContractPenaltyTests
{
    private static Func<CommitmentId> SequentialFactory()
    {
        var counter = 0;
        return () =>
        {
            counter++;
            var bytes = new byte[16];
            BitConverter.GetBytes(counter).CopyTo(bytes, 0);
            return CommitmentId.From(new Guid(bytes));
        };
    }

    private static (EconomicContract Contract, CommitmentId RentOutflow) ActivatedOneMonth(PenaltyTerms? penaltyTerms = null)
    {
        // DefaultTerms: anchor 5, window 15 dias → janela [2025-10-05, 2025-10-20].
        var mother = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithStartDate(new DateOnly(2025, 10, 1));
        if (penaltyTerms is not null)
            mother.WithPenaltyTerms(penaltyTerms);
        var contract = mother.Build();
        contract.Activate(EconomicContractMother.FixedOccurredAt, SequentialFactory());
        var rentOutflow = contract.FindPromisedCommitment(
            EconomicContractMother.October2025(), CommitmentDirection.OutflowPromise, CommitmentPurpose.Rent);
        return (contract, rentOutflow.Id);
    }

    // Fábrica de ids para a trilha Penalty, com offset alto para não colidir com os ids da ativação.
    private static Func<CommitmentId> PenaltyFactory()
    {
        var counter = 1000;
        return () =>
        {
            counter++;
            var bytes = new byte[16];
            BitConverter.GetBytes(counter).CopyTo(bytes, 0);
            return CommitmentId.From(new Guid(bytes));
        };
    }

    // Pagamento dentro da janela de cumprimento não gera penalidade (no-op, retorna false).
    [Fact]
    public void TryRegisterLatePenalty_WhenPaidWithinWindow_ShouldBeNoOp()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var created = contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 18), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.False(created);
        Assert.DoesNotContain(contract.Commitments, c => c.Purpose == CommitmentPurpose.Penalty);
    }

    // Pagamento após a janela gera o par Penalty; com 0 meses cheios incide só a multa (1000 × 2% = 20).
    [Fact]
    public void TryRegisterLatePenalty_WhenPaidAfterWindow_ShouldGeneratePenaltyPair()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        var created = contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.True(created);
        var penaltyCommitments = contract.Commitments.Where(c => c.Purpose == CommitmentPurpose.Penalty).ToList();
        Assert.Equal(2, penaltyCommitments.Count);
        Assert.All(penaltyCommitments, c => Assert.Equal(20m, c.ExpectedAmount.Amount));
    }

    // Atraso de meses cheios soma juros de mora: pago em dez/2025 (2 meses após out) → 1000 × (2% + 1%×2) = 40.
    [Fact]
    public void TryRegisterLatePenalty_WithFullMonthsLate_ShouldAddInterest()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();

        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 12, 1), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(40m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Idempotência: chamar de novo para o mesmo período não materializa uma segunda penalidade.
    [Fact]
    public void TryRegisterLatePenalty_CalledTwice_ShouldMaterializeOnlyOnce()
    {
        var (contract, rentOutflow) = ActivatedOneMonth();
        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var secondCall = contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        Assert.False(secondCall);
        Assert.Equal(2, contract.Commitments.Count(c => c.Purpose == CommitmentPurpose.Penalty));
    }

    // Política com juros diário: 5 dias após a janela → 1000 × (2% + 0,1%×5) = 25.
    [Fact]
    public void TryRegisterLatePenalty_WithDailyInterest_ShouldAccruePerDay()
    {
        var (contract, rentOutflow) = ActivatedOneMonth(new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, PenaltyValueKind.Percent, 0.001m, InterestAccrualPeriod.Daily));

        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 25), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(25m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Política com juros anual: pagamento em jan/2026 (1 ano-calendário após out/2025) → 1000 × (2% + 10%×1) = 120.
    [Fact]
    public void TryRegisterLatePenalty_WithYearlyInterest_ShouldAccruePerYear()
    {
        var (contract, rentOutflow) = ActivatedOneMonth(new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, PenaltyValueKind.Percent, 0.10m, InterestAccrualPeriod.Yearly));

        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2026, 1, 5), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(120m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Multa fixa é cobrada uma vez, somada ao juros percentual mensal: 50 + 1000×1%×2 = 70.
    [Fact]
    public void TryRegisterLatePenalty_WithFixedFine_ShouldChargeFlatAmount()
    {
        var (contract, rentOutflow) = ActivatedOneMonth(new PenaltyTerms(
            PenaltyValueKind.FixedAmount, 50m, PenaltyValueKind.Percent, 0.01m, InterestAccrualPeriod.Monthly));

        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 12, 1), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(70m, penaltyOutflow.ExpectedAmount.Amount);
    }

    // Juros de valor fixo multiplica pelas unidades decorridas: multa 2%×1000 + R$10×3 dias = 50.
    [Fact]
    public void TryRegisterLatePenalty_WithFixedInterest_ShouldMultiplyByUnitsLate()
    {
        var (contract, rentOutflow) = ActivatedOneMonth(new PenaltyTerms(
            PenaltyValueKind.Percent, 0.02m, PenaltyValueKind.FixedAmount, 10m, InterestAccrualPeriod.Daily));

        contract.TryRegisterLatePenalty(
            rentOutflow, new DateOnly(2025, 10, 23), PenaltyFactory(), EconomicContractMother.FixedOccurredAt);

        var penaltyOutflow = contract.Commitments.Single(c =>
            c.Purpose == CommitmentPurpose.Penalty && c.Direction == CommitmentDirection.OutflowPromise);
        Assert.Equal(50m, penaltyOutflow.ExpectedAmount.Amount);
    }
}
