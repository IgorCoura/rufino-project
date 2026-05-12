namespace AccountsPayable.UnitTests.Payables.ValueObjects;

using AccountsPayable.Domain.Payables.ValueObjects;

public class DueDateTests
{
    // DueDate é um wrapper trivial de DateOnly — preserva o valor recebido.
    [Fact]
    public void Constructor_PreservesValue()
    {
        var date = new DateOnly(2024, 3, 15);

        var dueDate = new DueDate(date);

        Assert.Equal(date, dueDate.Value);
    }

    // Dois DueDates com a mesma data são iguais (igualdade por componente).
    [Fact]
    public void Equals_WithSameDate_ShouldReturnTrue()
    {
        var a = new DueDate(new DateOnly(2024, 3, 15));
        var b = new DueDate(new DateOnly(2024, 3, 15));

        Assert.Equal(a, b);
    }

    // DueDate aceita data no passado — validação contextual ("não pode estar no passado na criação") é responsabilidade do Aggregate Payable, não do VO.
    [Fact]
    public void Constructor_AcceptsPastDate_BecauseContextualRuleLivesInAggregate()
    {
        var farPast = new DateOnly(1999, 1, 1);

        var dueDate = new DueDate(farPast);

        Assert.Equal(farPast, dueDate.Value);
    }
}
