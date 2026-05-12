namespace AccountsPayable.UnitTests.ChartOfAccounts;

using System.Reflection;
using AccountsPayable.Domain.ChartOfAccounts;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Factory de erros do ChartOfAccounts (AP.COA##). Cobre 8 IDs únicos.
/// </summary>
public class ChartOfAccountsErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(ChartOfAccounts).Assembly.GetType("AccountsPayable.Domain.Errors.ChartOfAccountsErrors")
        ?? throw new InvalidOperationException("ChartOfAccountsErrors type not found.");

    // Cada factory retorna DomainException com o Id esperado e MessageTemplate não vazio.
    [Theory]
    [InlineData("AccountTypeRequired", "AP.COA01")]
    [InlineData("DuplicatedAccountCode", "AP.COA02")]
    [InlineData("ParentNotFound", "AP.COA03")]
    [InlineData("ParentInactive", "AP.COA04")]
    [InlineData("MaxDepthExceeded", "AP.COA05")]
    [InlineData("AccountNotFound", "AP.COA06")]
    [InlineData("AccountAlreadyInactive", "AP.COA07")]
    [InlineData("CannotDeactivateAccountWithActiveChildren", "AP.COA08")]
    public void EachErrorFactory_ReturnsExpectedId(string methodName, string expectedId)
    {
        var error = InvokeWithSampleArgs(methodName);

        Assert.Equal(expectedId, error.Id);
        Assert.NotEmpty(error.MessageTemplate);
        Assert.NotEmpty(error.SourcePath);
    }

    // Todos os Ids são únicos (proteção contra duplicidade ao adicionar erro novo).
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var methods = new[]
        {
            "AccountTypeRequired",
            "DuplicatedAccountCode",
            "ParentNotFound",
            "ParentInactive",
            "MaxDepthExceeded",
            "AccountNotFound",
            "AccountAlreadyInactive",
            "CannotDeactivateAccountWithActiveChildren",
        };
        var ids = methods.Select(m => InvokeWithSampleArgs(m).Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    private static DomainException InvokeWithSampleArgs(string methodName)
    {
        object?[] args = methodName switch
        {
            "DuplicatedAccountCode" => new object?[] { "4.01" },
            "ParentNotFound" => new object?[] { Guid.NewGuid() },
            "ParentInactive" => new object?[] { Guid.NewGuid() },
            "MaxDepthExceeded" => new object?[] { 5 },
            "AccountNotFound" => new object?[] { Guid.NewGuid() },
            "AccountAlreadyInactive" => new object?[] { Guid.NewGuid() },
            _ => Array.Empty<object?>(),
        };

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
