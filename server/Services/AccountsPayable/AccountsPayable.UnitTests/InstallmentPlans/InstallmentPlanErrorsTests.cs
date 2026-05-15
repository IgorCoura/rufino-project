namespace AccountsPayable.UnitTests.InstallmentPlans;

using System.Reflection;
using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Factory de erros do InstallmentPlan (AP.IPL##). Acessada via reflection porque é internal.
/// </summary>
public class InstallmentPlanErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(InstallmentPlan).Assembly.GetType("AccountsPayable.Domain.Errors.InstallmentPlanErrors")
        ?? throw new InvalidOperationException("InstallmentPlanErrors type not found.");

    // InstallmentCountTooLow retorna AP.IPL01 com o count recebido.
    [Fact]
    public void InstallmentCountTooLow_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("InstallmentCountTooLow", 1);

        Assert.Equal("AP.IPL01", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(1, error.Parameters[0]);
    }

    // InstallmentNumberOutOfRange retorna AP.IPL02 com o número fora do range + o teto.
    [Fact]
    public void InstallmentNumberOutOfRange_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("InstallmentNumberOutOfRange", 13, 12);

        Assert.Equal("AP.IPL02", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal(13, error.Parameters[0]);
        Assert.Equal(12, error.Parameters[1]);
    }

    // InstallmentNumberAlreadyRegistered retorna AP.IPL03 com o número duplicado.
    [Fact]
    public void InstallmentNumberAlreadyRegistered_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("InstallmentNumberAlreadyRegistered", 5);

        Assert.Equal("AP.IPL03", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(5, error.Parameters[0]);
    }

    // AlreadyCancelled retorna AP.IPL04 sem parâmetros.
    [Fact]
    public void AlreadyCancelled_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("AlreadyCancelled");

        Assert.Equal("AP.IPL04", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CancellationReasonRequired retorna AP.IPL05 sem parâmetros.
    [Fact]
    public void CancellationReasonRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("CancellationReasonRequired");

        Assert.Equal("AP.IPL05", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CannotRegisterOnCancelled retorna AP.IPL06 sem parâmetros.
    [Fact]
    public void CannotRegisterOnCancelled_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("CannotRegisterOnCancelled");

        Assert.Equal("AP.IPL06", error.Id);
        Assert.Empty(error.Parameters);
    }

    // AmountSumMismatch retorna AP.IPL07 com expectedTotal e actualSum.
    [Fact]
    public void AmountSumMismatch_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("AmountSumMismatch", 1_000m, 999.99m);

        Assert.Equal("AP.IPL07", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal(1_000m, error.Parameters[0]);
        Assert.Equal(999.99m, error.Parameters[1]);
    }

    // Todos os Ids são únicos (proteção contra duplicidade acidental).
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("InstallmentCountTooLow", 1).Id,
            Invoke("InstallmentNumberOutOfRange", 1, 1).Id,
            Invoke("InstallmentNumberAlreadyRegistered", 1).Id,
            Invoke("AlreadyCancelled").Id,
            Invoke("CancellationReasonRequired").Id,
            Invoke("CannotRegisterOnCancelled").Id,
            Invoke("AmountSumMismatch", 1m, 1m).Id,
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
