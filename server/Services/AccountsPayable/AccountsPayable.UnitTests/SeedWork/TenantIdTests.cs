namespace AccountsPayable.UnitTests.SeedWork;

using AccountsPayable.Domain.SeedWork;

public class TenantIdTests
{
    // TenantId.New() gera Ids não-vazios e distintos a cada chamada.
    [Fact]
    public void New_GeneratesNonEmptyDistinctIds()
    {
        var a = TenantId.New();
        var b = TenantId.New();

        Assert.NotEqual(TenantId.Empty, a);
        Assert.NotEqual(a, b);
    }

    // TenantId.From(guid) preserva o Guid original em Value (sem transformação).
    [Fact]
    public void From_PreservesUnderlyingGuid()
    {
        var guid = Guid.NewGuid();
        var id = TenantId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    // TenantId.Empty.Value é Guid.Empty — representa "id ainda não definido".
    [Fact]
    public void Empty_HasGuidEmpty()
    {
        Assert.Equal(Guid.Empty, TenantId.Empty.Value);
    }

    // Dois TenantId com mesmo Value são iguais (igualdade por valor do record struct).
    [Fact]
    public void Equality_IsByValue()
    {
        var guid = Guid.NewGuid();
        Assert.Equal(TenantId.From(guid), TenantId.From(guid));
        Assert.NotEqual(TenantId.From(guid), TenantId.From(Guid.NewGuid()));
    }
}
