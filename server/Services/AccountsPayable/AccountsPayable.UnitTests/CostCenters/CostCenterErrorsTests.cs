namespace AccountsPayable.UnitTests.CostCenters;

using System.Reflection;
using AccountsPayable.Domain.CostCenters;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Factory de erros do CostCenter (AP.CCT##).
/// </summary>
public class CostCenterErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(CostCenter).Assembly.GetType("AccountsPayable.Domain.Errors.CostCenterErrors")
        ?? throw new InvalidOperationException("CostCenterErrors type not found.");

    // AlreadyActive retorna AP.CCT01 sem parâmetros.
    [Fact]
    public void AlreadyActive_ShouldHaveCorrectId()
    {
        var error = Invoke("AlreadyActive");

        Assert.Equal("AP.CCT01", error.Id);
        Assert.Empty(error.Parameters);
        Assert.NotEmpty(error.MessageTemplate);
    }

    // AlreadyInactive retorna AP.CCT02 sem parâmetros.
    [Fact]
    public void AlreadyInactive_ShouldHaveCorrectId()
    {
        var error = Invoke("AlreadyInactive");

        Assert.Equal("AP.CCT02", error.Id);
        Assert.Empty(error.Parameters);
    }

    // Ids únicos.
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[] { Invoke("AlreadyActive").Id, Invoke("AlreadyInactive").Id };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    private static DomainException Invoke(string methodName)
    {
        var method = ERRORS_TYPE.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName);
        var allParams = method.GetParameters();
        var fullArgs = new object?[allParams.Length];
        for (var i = 0; i < allParams.Length; i++)
            fullArgs[i] = Type.Missing;

        var result = method.Invoke(null, BindingFlags.OptionalParamBinding, binder: null, fullArgs, culture: null);
        return (DomainException)result!;
    }
}
