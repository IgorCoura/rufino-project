namespace AccountsPayable.UnitTests.AutoApprovalPolicies;

using System.Reflection;
using AccountsPayable.Domain.AutoApprovalPolicies;
using AccountsPayable.Domain.SeedWork;

public class AutoApprovalPolicyErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(AutoApprovalPolicy).Assembly.GetType("AccountsPayable.Domain.Errors.AutoApprovalPolicyErrors")
        ?? throw new InvalidOperationException("AutoApprovalPolicyErrors type not found.");

    // RequiredApprovalCountTooLow retorna AP.AAP01 com o count recebido.
    [Fact]
    public void RequiredApprovalCountTooLow_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("RequiredApprovalCountTooLow", 0);

        Assert.Equal("AP.AAP01", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(0, error.Parameters[0]);
    }

    // ApprovalRuleNotFound retorna AP.AAP02 com o ruleId procurado.
    [Fact]
    public void ApprovalRuleNotFound_ShouldHaveCorrectIdAndParameter()
    {
        var guid = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var error = Invoke("ApprovalRuleNotFound", guid);

        Assert.Equal("AP.AAP02", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(guid, error.Parameters[0]);
    }

    // ApprovalRuleAlreadyActive retorna AP.AAP03.
    [Fact]
    public void ApprovalRuleAlreadyActive_ShouldHaveCorrectId()
    {
        var error = Invoke("ApprovalRuleAlreadyActive", Guid.Empty);
        Assert.Equal("AP.AAP03", error.Id);
    }

    // ApprovalRuleAlreadyInactive retorna AP.AAP04.
    [Fact]
    public void ApprovalRuleAlreadyInactive_ShouldHaveCorrectId()
    {
        var error = Invoke("ApprovalRuleAlreadyInactive", Guid.Empty);
        Assert.Equal("AP.AAP04", error.Id);
    }

    // Todos os Ids são únicos.
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("RequiredApprovalCountTooLow", 0).Id,
            Invoke("ApprovalRuleNotFound", Guid.Empty).Id,
            Invoke("ApprovalRuleAlreadyActive", Guid.Empty).Id,
            Invoke("ApprovalRuleAlreadyInactive", Guid.Empty).Id,
        };
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    private static DomainException Invoke(string methodName, params object[] args)
    {
        var method = ERRORS_TYPE.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName);
        var allParams = method.GetParameters();
        var fullArgs = new object?[allParams.Length];
        for (var i = 0; i < allParams.Length; i++)
            fullArgs[i] = i < args.Length ? args[i] : Type.Missing;

        var result = method.Invoke(null, BindingFlags.OptionalParamBinding, binder: null, fullArgs, culture: null);
        return (DomainException)result!;
    }
}
