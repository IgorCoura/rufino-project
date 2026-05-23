namespace EconomicCore.UnitTests.Operational.EconomicEvents;

using EconomicCore.Domain.Operational.EconomicEvents;

public class EconomicEventErrorsTests
{
    // Cada factory mapeia para um sufixo ECC.EVT## consistente com a tabela §5.1 + extensões para VOs.
    [Theory]
    [InlineData("ECC.EVT01")]
    [InlineData("ECC.EVT02")]
    [InlineData("ECC.EVT03")]
    [InlineData("ECC.EVT04")]
    [InlineData("ECC.EVT05")]
    [InlineData("ECC.EVT07")]
    [InlineData("ECC.EVT08")]
    [InlineData("ECC.EVT09")]
    [InlineData("ECC.EVT11")]
    [InlineData("ECC.EVT12")]
    [InlineData("ECC.EVT13")]
    public void Factories_NoParam_ShouldReturnExceptionWithExpectedId(string expectedId)
    {
        var ex = expectedId switch
        {
            "ECC.EVT01" => EconomicEventErrors.MissingParticipants(),
            "ECC.EVT02" => EconomicEventErrors.InvalidAmount(),
            "ECC.EVT03" => EconomicEventErrors.MissingResource(),
            "ECC.EVT04" => EconomicEventErrors.OrphanEvent(),
            "ECC.EVT05" => EconomicEventErrors.DualityAlreadyClosed(),
            "ECC.EVT07" => EconomicEventErrors.ImmutableFactViolation("Amount"),
            "ECC.EVT08" => EconomicEventErrors.InvalidParticipationAgent(),
            "ECC.EVT09" => EconomicEventErrors.InvalidParticipationRole(),
            "ECC.EVT11" => EconomicEventErrors.InvalidDualityCounterpart(),
            "ECC.EVT12" => EconomicEventErrors.InvalidDualityMatchedAmount(),
            "ECC.EVT13" => EconomicEventErrors.InvalidCommitmentRef(),
            _ => throw new InvalidOperationException("Unknown error id."),
        };

        Assert.Equal(expectedId, ex.Id);
        Assert.False(string.IsNullOrWhiteSpace(ex.MessageTemplate));
        Assert.False(string.IsNullOrWhiteSpace(ex.SourcePath));
    }

    // MatchExceedsBalance carrega 2 parâmetros (attempted, remaining).
    [Fact]
    public void MatchExceedsBalance_ShouldReturnECC_EVT06_WithTwoParameters()
    {
        var ex = EconomicEventErrors.MatchExceedsBalance(attempted: 1500m, remaining: 1000m);

        Assert.Equal("ECC.EVT06", ex.Id);
        Assert.Equal(2, ex.Parameters.Count);
    }

    // InvalidEventTimestamp carrega 1 parâmetro (kind).
    [Fact]
    public void InvalidEventTimestamp_ShouldReturnECC_EVT10_WithOneParameter()
    {
        var ex = EconomicEventErrors.InvalidEventTimestamp("Local");

        Assert.Equal("ECC.EVT10", ex.Id);
        Assert.Single(ex.Parameters);
    }
}
