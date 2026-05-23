namespace EconomicCore.UnitTests.Prospective.EconomicContracts;

using EconomicCore.Domain.Prospective.EconomicContracts;

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
}
