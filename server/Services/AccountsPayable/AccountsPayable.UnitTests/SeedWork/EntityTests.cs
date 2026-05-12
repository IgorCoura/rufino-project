namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.UnitTests.SeedWork._TestDoubles;

public class EntityTests
{
    // Ctor sem argumentos gera Id automaticamente via TId.New() e a Entity já não fica transient.
    [Fact]
    public void DefaultConstructor_AutoGeneratesNonEmptyId()
    {
        var entity = new TestEntity();
        Assert.False(entity.IsTransient());
        Assert.NotEqual(TestId.Empty, entity.Id);
    }

    // Ctor Entity(TId) aceita Id não-vazio e o atribui à propriedade Id.
    [Fact]
    public void IdConstructor_AcceptsValidId()
    {
        var id = TestId.New();
        var entity = new TestEntity(id);
        Assert.Equal(id, entity.Id);
    }

    // Construir Entity com TId.Empty lança SWK01 (invariante de identidade do SeedWork).
    [Fact]
    public void IdConstructor_RejectsEmptyId()
    {
        var ex = Assert.Throws<DomainException>(() => new TestEntity(TestId.Empty));
        Assert.Equal("SWK01", ex.Id);
    }

    // O setter de Id também valida contra TId.Empty e lança SWK01 (defesa em profundidade).
    [Fact]
    public void AssignId_RejectsEmpty()
    {
        var entity = new TestEntity();
        var ex = Assert.Throws<DomainException>(() => entity.AssignId(TestId.Empty));
        Assert.Equal("SWK01", ex.Id);
    }

    // Duas Entities do mesmo tipo com mesmo Id são iguais (Equals e ==) e têm o mesmo hash code.
    [Fact]
    public void Equality_TwoInstancesWithSameId_AreEqual()
    {
        var id = TestId.New();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Entities com Ids diferentes não são iguais (Equals false, != true).
    [Fact]
    public void Equality_DifferentIds_AreNotEqual()
    {
        var a = new TestEntity(TestId.New());
        var b = new TestEntity(TestId.New());

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }
}
