namespace EconomicCore.UnitTests.SharedKernel;

using EconomicCore.Domain.SharedKernel;

public class TenantIdTests
{
    // TenantId é representativo do padrão IEntityId<TSelf> aplicado a todos os strongly-typed IDs do BC.
    // Os demais IDs (EconomicAgentId, EconomicRoleId, etc.) seguem o mesmo template — não duplicar testes.

    // New() gera ID com Guid não-vazio (v7 — ordenação temporal).
    [Fact]
    public void New_ShouldGenerateNonEmptyId()
    {
        var id = TenantId.New();

        Assert.NotEqual(Guid.Empty, id.Value);
    }

    // From(guid) reconstrói o ID preservando o Value (caminho de reidratação a partir do banco).
    [Fact]
    public void From_WithGuid_ShouldStoreValue()
    {
        var guid = new Guid("11111111-1111-7111-8111-111111111111");

        var id = TenantId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    // Empty é o estado transiente (equivale a Guid.Empty) usado por Entity.IsTransient().
    [Fact]
    public void Empty_ShouldEqualGuidEmpty()
    {
        Assert.Equal(Guid.Empty, TenantId.Empty.Value);
    }

    // Dois TenantIds com o mesmo Value são iguais (igualdade automática do record struct).
    [Fact]
    public void Equality_SameValue_ShouldBeTrue()
    {
        var guid = Guid.CreateVersion7();
        var a = TenantId.From(guid);
        var b = TenantId.From(guid);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    // Dois TenantIds com Values diferentes não são iguais.
    [Fact]
    public void Equality_DifferentValue_ShouldBeFalse()
    {
        var a = TenantId.New();
        var b = TenantId.New();

        Assert.NotEqual(a, b);
    }

    // ToString delega ao Guid.ToString — útil em logs e serialização padrão.
    [Fact]
    public void ToString_ShouldReturnGuidStringRepresentation()
    {
        var guid = new Guid("11111111-1111-7111-8111-111111111111");
        var id = TenantId.From(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }
}
