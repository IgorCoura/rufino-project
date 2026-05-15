namespace AccountsPayable.UnitTests.AutoApprovalPolicies.ValueObjects;

using AccountsPayable.Domain.AutoApprovalPolicies.ValueObjects;
using AccountsPayable.Domain.SeedWork;

public class ApproverRolesTests
{
    // Construtor com lista vazia lança AP.APR01.
    [Fact]
    public void Construct_WithEmptyList_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ApproverRoles(Array.Empty<string>()));
        Assert.Equal("AP.APR01", ex.Id);
    }

    // Role vazia/whitespace lança AP.APR02.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construct_WithBlankRole_ShouldThrowDomainException(string role)
    {
        var ex = Assert.Throws<DomainException>(() => new ApproverRoles(new[] { "OWNER", role }));
        Assert.Equal("AP.APR02", ex.Id);
    }

    // Role acima de MAX_ROLE_LENGTH lança AP.APR03.
    [Fact]
    public void Construct_WithRoleAboveMaxLength_ShouldThrowDomainException()
    {
        var huge = new string('x', ApproverRoles.MAX_ROLE_LENGTH + 1);
        var ex = Assert.Throws<DomainException>(() => new ApproverRoles(new[] { huge }));
        Assert.Equal("AP.APR03", ex.Id);
    }

    // Role duplicada (após normalização para uppercase) lança AP.APR04.
    [Fact]
    public void Construct_WithDuplicateRoleAfterNormalization_ShouldThrowDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new ApproverRoles(new[] { "Owner", "OWNER" }));
        Assert.Equal("AP.APR04", ex.Id);
    }

    // Roles são trimadas e uppercase normalizadas.
    [Fact]
    public void Construct_ShouldTrimAndUppercaseRoles()
    {
        var roles = new ApproverRoles(new[] { "  Owner  ", "partner" });

        Assert.Equal(new[] { "OWNER", "PARTNER" }, roles.Roles);
    }

    // Contains é case-insensitive (compara após normalização).
    [Fact]
    public void Contains_ShouldBeCaseInsensitive()
    {
        var roles = new ApproverRoles(new[] { "OWNER", "PARTNER" });

        Assert.True(roles.Contains("owner"));
        Assert.True(roles.Contains("Partner"));
        Assert.False(roles.Contains("FINANCE"));
        Assert.False(roles.Contains(""));
    }

    // Igualdade estrutural — duas instâncias com mesmas roles são iguais.
    [Fact]
    public void Equality_ShouldBeStructural()
    {
        var a = new ApproverRoles(new[] { "OWNER", "PARTNER" });
        var b = new ApproverRoles(new[] { "owner", "partner" });

        Assert.Equal(a, b);
    }
}
