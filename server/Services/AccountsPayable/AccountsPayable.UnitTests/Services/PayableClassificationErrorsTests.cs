namespace AccountsPayable.UnitTests.Services;

using System.Reflection;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;

/// <summary>
/// Factory de erros do Domain Service PayableClassificationValidator (AP.PCL##).
/// </summary>
public class PayableClassificationErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(PayableClassificationValidator).Assembly
            .GetType("AccountsPayable.Domain.Errors.PayableClassificationErrors")
        ?? throw new InvalidOperationException("PayableClassificationErrors type not found.");

    // Cada factory retorna DomainException com o Id esperado e SourcePath preenchido.
    [Theory]
    [InlineData("AccountNotFound", "AP.PCL01")]
    [InlineData("AccountInactive", "AP.PCL02")]
    [InlineData("AccountTypeNotAllowed", "AP.PCL03")]
    [InlineData("CostCenterInactive", "AP.PCL04")]
    [InlineData("TenantMismatch", "AP.PCL05")]
    public void EachErrorFactory_ReturnsExpectedId(string methodName, string expectedId)
    {
        var error = InvokeWithSampleArgs(methodName);

        Assert.Equal(expectedId, error.Id);
        Assert.NotEmpty(error.MessageTemplate);
        Assert.NotEmpty(error.SourcePath);
    }

    // Todos os Ids são únicos.
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var methods = new[]
        {
            "AccountNotFound", "AccountInactive", "AccountTypeNotAllowed",
            "CostCenterInactive", "TenantMismatch",
        };
        var ids = methods.Select(m => InvokeWithSampleArgs(m).Id).ToList();

        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    private static DomainException InvokeWithSampleArgs(string methodName)
    {
        object?[] args = methodName switch
        {
            "AccountNotFound" => new object?[] { Guid.NewGuid() },
            "AccountInactive" => new object?[] { Guid.NewGuid() },
            "AccountTypeNotAllowed" => new object?[] { "ASSET" },
            "CostCenterInactive" => new object?[] { Guid.NewGuid() },
            "TenantMismatch" => new object?[] { "ChartOfAccounts" },
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
