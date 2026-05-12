namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.SeedWork._TestDoubles;

public class EnumerationTests
{
    // GetAll<T>() retorna todas as instâncias public static readonly declaradas no Smart Enum.
    [Fact]
    public void GetAll_ReturnsAllDeclaredInstances()
    {
        var all = Enumeration.GetAll<TestStatus>().ToList();
        Assert.Equal(3, all.Count);
        Assert.Contains(TestStatus.Draft, all);
        Assert.Contains(TestStatus.Active, all);
        Assert.Contains(TestStatus.Closed, all);
    }

    // FromValue<T>(id) resolve para a instância cujo Id casa exatamente.
    [Fact]
    public void FromValue_ResolvesById()
    {
        Assert.Same(TestStatus.Active, Enumeration.FromValue<TestStatus>(2));
    }

    // FromValue<T> com Id inexistente lança InvalidOperationException (sem fallback silencioso).
    [Fact]
    public void FromValue_UnknownIdThrows()
    {
        Assert.Throws<InvalidOperationException>(() => Enumeration.FromValue<TestStatus>(99));
    }

    // TryFromValue<T> com Id inexistente retorna null em vez de lançar — variante segura para parsing.
    [Fact]
    public void TryFromValue_UnknownIdReturnsNull()
    {
        Assert.Null(Enumeration.TryFromValue<TestStatus>(99));
    }

    // FromDisplayName casa o Name ignorando case (DRAFT == draft == Draft).
    [Fact]
    public void FromDisplayName_IsCaseInsensitive()
    {
        Assert.Same(TestStatus.Draft, Enumeration.FromDisplayName<TestStatus>("draft"));
        Assert.Same(TestStatus.Draft, Enumeration.FromDisplayName<TestStatus>("DRAFT"));
    }

    // Igualdade entre duas referências da mesma instância de Smart Enum é true e hash code coincide.
    [Fact]
    public void Equals_SameIdAndType_IsEqual()
    {
        var a = Enumeration.FromValue<TestStatus>(1);
        var b = Enumeration.FromValue<TestStatus>(1);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // AbsoluteDifference retorna |Id_a - Id_b| — distância entre duas instâncias do Smart Enum.
    [Fact]
    public void AbsoluteDifference_ReturnsDifferenceOfIds()
    {
        Assert.Equal(2, Enumeration.AbsoluteDifference(TestStatus.Draft, TestStatus.Closed));
    }
}
