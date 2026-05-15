namespace AccountsPayable.UnitTests.ExpenseClassificationRules;

using System.Reflection;
using AccountsPayable.Domain.ExpenseClassificationRules;
using AccountsPayable.Domain.SeedWork;

public class ExpenseClassificationRuleErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(ExpenseClassificationRule).Assembly.GetType("AccountsPayable.Domain.Errors.ExpenseClassificationRuleErrors")
        ?? throw new InvalidOperationException("ExpenseClassificationRuleErrors type not found.");

    // PriorityMustBePositive retorna AP.ECR01 com o priority recebido.
    [Fact]
    public void PriorityMustBePositive_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("PriorityMustBePositive", 0);

        Assert.Equal("AP.ECR01", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(0, error.Parameters[0]);
    }

    // AlreadyActive retorna AP.ECR02 sem parâmetros.
    [Fact]
    public void AlreadyActive_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("AlreadyActive");

        Assert.Equal("AP.ECR02", error.Id);
        Assert.Empty(error.Parameters);
    }

    // AlreadyInactive retorna AP.ECR03 sem parâmetros.
    [Fact]
    public void AlreadyInactive_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("AlreadyInactive");

        Assert.Equal("AP.ECR03", error.Id);
        Assert.Empty(error.Parameters);
    }

    // Todos os Ids são únicos (proteção contra duplicidade acidental).
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("PriorityMustBePositive", 0).Id,
            Invoke("AlreadyActive").Id,
            Invoke("AlreadyInactive").Id,
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
