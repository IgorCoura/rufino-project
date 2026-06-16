namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.SeedWork;

public class EconomicContractErrorsTests
{
    // Cada factory mapeia para o sufixo ECC.CTR## esperado. Slot CTR04 reservado (warning soft).
    [Theory]
    [InlineData("ECC.CTR01")]
    [InlineData("ECC.CTR06")]
    [InlineData("ECC.CTR08")]
    [InlineData("ECC.CTR11")]
    public void Factories_NoParam_ShouldReturnExceptionWithExpectedId(string expectedId)
    {
        var ex = expectedId switch
        {
            "ECC.CTR01" => EconomicContractErrors.MissingReciprocal(),
            "ECC.CTR06" => EconomicContractErrors.InvalidRecurrencePeriodicity(),
            "ECC.CTR08" => EconomicContractErrors.InvalidCommitmentTermsAmount(),
            "ECC.CTR11" => EconomicContractErrors.InvalidReciprocalLink(),
            _ => throw new InvalidOperationException("Unknown error id."),
        };

        Assert.Equal(expectedId, ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
    }

    // DuplicateCommitmentForPeriod (CTR02) carrega 3 parâmetros (month, year, directionName).
    [Fact]
    public void DuplicateCommitmentForPeriod_ShouldReturnECC_CTR02_WithThreeParameters()
    {
        var ex = EconomicContractErrors.DuplicateCommitmentForPeriod(2025, 10, "OUTFLOW_PROMISE");

        Assert.Equal("ECC.CTR02", ex.Id);
        Assert.Equal(3, ex.Parameters.Count);
    }

    // CannotFulfillInStatus (CTR03) carrega 1 parâmetro (currentStatus).
    [Fact]
    public void CannotFulfillInStatus_ShouldReturnECC_CTR03_WithOneParameter()
    {
        var ex = EconomicContractErrors.CannotFulfillInStatus("FULFILLED");

        Assert.Equal("ECC.CTR03", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // AmountOutsideTolerance (CTR04) é slot reservado para sinalização (não bloqueia em Phase 1).
    [Fact]
    public void AmountOutsideTolerance_ShouldReturnECC_CTR04_WithTwoParameters()
    {
        var ex = EconomicContractErrors.AmountOutsideTolerance(expected: 1000m, actual: 1500m);

        Assert.Equal("ECC.CTR04", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // ContractNotActive (CTR05) carrega o status atual.
    [Fact]
    public void ContractNotActive_ShouldReturnECC_CTR05_WithStatusName()
    {
        var ex = EconomicContractErrors.ContractNotActive("TERMINATED");

        Assert.Equal("ECC.CTR05", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // InvalidContractStatusTransition (CTR13) carrega from + to.
    [Fact]
    public void InvalidContractStatusTransition_ShouldReturnECC_CTR13_WithTwoParameters()
    {
        var ex = EconomicContractErrors.InvalidContractStatusTransition("TERMINATED", "ACTIVE");

        Assert.Equal("ECC.CTR13", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // ContractNotDraft (CTR16) carrega o status atual quando se tenta ativar contrato fora de Draft.
    [Fact]
    public void ContractNotDraft_ShouldReturnECC_CTR16_WithCurrentStatus()
    {
        var ex = EconomicContractErrors.ContractNotDraft("ACTIVE");

        Assert.Equal("ECC.CTR16", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // InvalidTermMonths (CTR17) carrega o valor recebido.
    [Fact]
    public void InvalidTermMonths_ShouldReturnECC_CTR17_WithValue()
    {
        var ex = EconomicContractErrors.InvalidTermMonths(0);

        Assert.Equal("ECC.CTR17", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // InvalidStartDate (CTR18) carrega a data recebida.
    [Fact]
    public void InvalidStartDate_ShouldReturnECC_CTR18_WithDate()
    {
        var ex = EconomicContractErrors.InvalidStartDate(new DateOnly(2000, 1, 1));

        Assert.Equal("ECC.CTR18", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // PaymentAmountMismatch (CTR19) carrega expected + received.
    [Fact]
    public void PaymentAmountMismatch_ShouldReturnECC_CTR19_WithTwoParameters()
    {
        var ex = EconomicContractErrors.PaymentAmountMismatch(expected: 1000m, received: 800m);

        Assert.Equal("ECC.CTR19", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // InvalidTerminationDate (CTR20) carrega data e último período ocupado.
    [Fact]
    public void InvalidTerminationDate_ShouldReturnECC_CTR20_WithTwoParameters()
    {
        var ex = EconomicContractErrors.InvalidTerminationDate(new DateOnly(2025, 10, 15), "2025-12");

        Assert.Equal("ECC.CTR20", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // OverlappingActiveContract (CTR21) carrega resourceId e startDate.
    [Fact]
    public void OverlappingActiveContract_ShouldReturnECC_CTR21_WithTwoParameters()
    {
        var ex = EconomicContractErrors.OverlappingActiveContract(
            resourceId: Guid.NewGuid(),
            startDate: new DateOnly(2025, 10, 1));

        Assert.Equal("ECC.CTR21", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // InvalidPenaltyFixedValue (CTR48) carrega o valor fixo rejeitado.
    [Fact]
    public void InvalidPenaltyFixedValue_ShouldReturnECC_CTR48_WithOneParameter()
    {
        var ex = EconomicContractErrors.InvalidPenaltyFixedValue(-10m);

        Assert.Equal("ECC.CTR48", ex.Id);
        Assert.Single(ex.Parameters);
    }

    // InvalidPenaltyTermsComposition (CTR49) é sem parâmetros (kind/period nulos).
    [Fact]
    public void InvalidPenaltyTermsComposition_ShouldReturnECC_CTR49()
    {
        var ex = EconomicContractErrors.InvalidPenaltyTermsComposition();

        Assert.Equal("ECC.CTR49", ex.Id);
        Assert.Empty(ex.Parameters);
    }

    // PenaltyTermsRequired (CTR50) é sem parâmetros (bloco penalty ausente na criação).
    [Fact]
    public void PenaltyTermsRequired_ShouldReturnECC_CTR50()
    {
        var ex = EconomicContractErrors.PenaltyTermsRequired();

        Assert.Equal("ECC.CTR50", ex.Id);
        Assert.Empty(ex.Parameters);
    }

    // PenaltyChangeNotAllowed (CTR51) carrega o status atual e mapeia para Conflict (409).
    [Fact]
    public void PenaltyChangeNotAllowed_ShouldReturnECC_CTR51_AsConflict()
    {
        var ex = EconomicContractErrors.PenaltyChangeNotAllowed("TERMINATED");

        Assert.Equal("ECC.CTR51", ex.Id);
        Assert.Single(ex.Parameters);
        Assert.Equal(DomainErrorCategory.Conflict, ex.Category);
    }
}
