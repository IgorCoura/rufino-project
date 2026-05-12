namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.UnitTests.SeedWork._TestDoubles;

public class ValueObjectTests
{
    // Dois VOs com mesmos componentes de igualdade (Amount + Currency) são iguais e têm mesmo hash code.
    [Fact]
    public void Equality_ByEqualityComponents()
    {
        var a = new TestMoney(10m, "BRL");
        var b = new TestMoney(10m, "BRL");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Diferença em um único componente (Amount) já quebra a igualdade entre os VOs.
    [Fact]
    public void Equality_DifferentAmount_NotEqual()
    {
        var a = new TestMoney(10m, "BRL");
        var b = new TestMoney(11m, "BRL");

        Assert.NotEqual(a, b);
    }

    // Diferença em outro componente (Currency) também quebra a igualdade.
    [Fact]
    public void Equality_DifferentCurrency_NotEqual()
    {
        var a = new TestMoney(10m, "BRL");
        var b = new TestMoney(10m, "USD");

        Assert.NotEqual(a, b);
    }

    // Comparar um VO com null retorna false (Equals não lança NullReferenceException).
    [Fact]
    public void Equality_AgainstNull_False()
    {
        var a = new TestMoney(10m, "BRL");
        Assert.False(a.Equals(null));
    }

    // GetCopy retorna nova instância igual em valor mas com referência distinta (shallow clone).
    [Fact]
    public void GetCopy_ProducesEqualButDistinctInstance()
    {
        var a = new TestMoney(10m, "BRL");
        var copy = a.GetCopy();

        Assert.Equal(a, copy);
        Assert.NotSame(a, copy);
    }
}
