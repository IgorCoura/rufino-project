namespace AccountsPayable.UnitTests.Services;

using System.Reflection;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;

/// <summary>
/// Factory de erros do Domain Service (AP.SUC##). Acessada via reflection porque a factory é internal.
/// </summary>
public class SupplierUniquenessCheckerErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(SupplierUniquenessChecker).Assembly
            .GetType("AccountsPayable.Domain.Errors.SupplierUniquenessCheckerErrors")
        ?? throw new InvalidOperationException("SupplierUniquenessCheckerErrors type not found.");

    // TaxIdAlreadyRegistered produz DomainException com Id AP.SUC01, 2 parâmetros e SourcePath preenchido.
    [Fact]
    public void TaxIdAlreadyRegistered_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("TaxIdAlreadyRegistered", "CNPJ", "**.***.***/0001-61");

        Assert.Equal("AP.SUC01", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal("CNPJ", error.Parameters[0]);
        Assert.Equal("**.***.***/0001-61", error.Parameters[1]);
        Assert.NotEmpty(error.MessageTemplate);
        Assert.NotEmpty(error.SourcePath);
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
