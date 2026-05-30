namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
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

    private static (EconomicContract Contract, CommitmentId RentOutflow) ActivatedOneMonth()
    {
        // DefaultTerms: anchor 5, window 15 dias → janela [2025-10-05, 2025-10-20].
        var contract = EconomicContractMother.New()
            .WithTermMonths(1)
            .WithStartDate(new DateOnly(2025, 10, 1))
            .Build();
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
}
