namespace AccountsPayable.UnitTests.ExpectedRecurringBills;

using System.Reflection;
using AccountsPayable.Domain.ExpectedRecurringBills;
using AccountsPayable.Domain.SeedWork;

public class ExpectedRecurringBillErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(ExpectedRecurringBill).Assembly.GetType("AccountsPayable.Domain.Errors.ExpectedRecurringBillErrors")
        ?? throw new InvalidOperationException("ExpectedRecurringBillErrors type not found.");

    [Fact]
    public void NotPending_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("NotPending", "MISSED");
        Assert.Equal("AP.ERB01", error.Id);
        Assert.Equal("MISSED", error.Parameters[0]);
    }

    [Fact]
    public void CancellationReasonRequired_ShouldHaveCorrectId()
    {
        var error = Invoke("CancellationReasonRequired");
        Assert.Equal("AP.ERB02", error.Id);
        Assert.Empty(error.Parameters);
    }

    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("NotPending", "X").Id,
            Invoke("CancellationReasonRequired").Id,
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
