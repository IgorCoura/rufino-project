namespace AccountsPayable.UnitTests.Contracts;

using System.Reflection;
using AccountsPayable.Domain.Contracts;
using AccountsPayable.Domain.SeedWork;

public class ContractErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(Contract).Assembly.GetType("AccountsPayable.Domain.Errors.ContractErrors")
        ?? throw new InvalidOperationException("ContractErrors type not found.");

    [Fact]
    public void InvalidStatusTransition_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("InvalidStatusTransition", "DRAFT", "ACTIVE");
        Assert.Equal("AP.CTR01", error.Id);
        Assert.Equal(2, error.Parameters.Count);
    }

    [Fact]
    public void PaymentDayOutOfRange_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("PaymentDayOutOfRange", 32);
        Assert.Equal("AP.CTR02", error.Id);
        Assert.Equal(32, error.Parameters[0]);
    }

    [Fact]
    public void EndDateBeforeStartDate_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("EndDateBeforeStartDate", new DateOnly(2024, 6, 1), new DateOnly(2024, 3, 1));
        Assert.Equal("AP.CTR03", error.Id);
        Assert.Equal(2, error.Parameters.Count);
    }

    [Fact]
    public void TerminationReasonRequired_ShouldHaveCorrectId()
    {
        var error = Invoke("TerminationReasonRequired");
        Assert.Equal("AP.CTR04", error.Id);
        Assert.Empty(error.Parameters);
    }

    [Fact]
    public void SuspensionReasonRequired_ShouldHaveCorrectId()
    {
        var error = Invoke("SuspensionReasonRequired");
        Assert.Equal("AP.CTR05", error.Id);
        Assert.Empty(error.Parameters);
    }

    [Fact]
    public void AmountUnchanged_ShouldHaveCorrectId()
    {
        var error = Invoke("AmountUnchanged");
        Assert.Equal("AP.CTR06", error.Id);
    }

    [Fact]
    public void CurrencyCannotChange_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("CurrencyCannotChange", "BRL", "USD");
        Assert.Equal("AP.CTR07", error.Id);
        Assert.Equal("BRL", error.Parameters[0]);
        Assert.Equal("USD", error.Parameters[1]);
    }

    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("InvalidStatusTransition", "A", "B").Id,
            Invoke("PaymentDayOutOfRange", 0).Id,
            Invoke("EndDateBeforeStartDate", new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 1)).Id,
            Invoke("TerminationReasonRequired").Id,
            Invoke("SuspensionReasonRequired").Id,
            Invoke("AmountUnchanged").Id,
            Invoke("CurrencyCannotChange", "A", "B").Id,
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
