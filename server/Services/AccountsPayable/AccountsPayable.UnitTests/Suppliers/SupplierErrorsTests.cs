namespace AccountsPayable.UnitTests.Suppliers;

using System.Reflection;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers;

/// <summary>
/// Factory de erros do Supplier (AP.SUP##). Verifica que cada método produz a DomainException
/// com Id correto, MessageTemplate não-vazio, Parameters consistente e SourcePath preenchido.
/// SupplierErrors é <c>internal static</c> — acessamos via reflection neste assembly de testes.
/// </summary>
public class SupplierErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(Supplier).Assembly.GetType("AccountsPayable.Domain.Errors.SupplierErrors")
        ?? throw new InvalidOperationException("SupplierErrors type not found.");

    // InvalidStatusTransition retorna AP.SUP01 com 2 parâmetros (status atual e alvo).
    [Fact]
    public void InvalidStatusTransition_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("InvalidStatusTransition", "ACTIVE", "BLOCKED");

        Assert.Equal("AP.SUP01", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.NotEmpty(error.MessageTemplate);
        Assert.NotEmpty(error.SourcePath);
    }

    // ReasonRequired retorna AP.SUP02 sem parâmetros.
    [Fact]
    public void ReasonRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("ReasonRequired");

        Assert.Equal("AP.SUP02", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CannotRemoveLastBankAccountWhileActive retorna AP.SUP03 sem parâmetros.
    [Fact]
    public void CannotRemoveLastBankAccountWhileActive_ShouldHaveCorrectId()
    {
        var error = Invoke("CannotRemoveLastBankAccountWhileActive");

        Assert.Equal("AP.SUP03", error.Id);
    }

    // BankAccountNotFound retorna AP.SUP04 com o Guid recebido como parâmetro.
    [Fact]
    public void BankAccountNotFound_ShouldHaveCorrectIdAndParameter()
    {
        var id = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var error = Invoke("BankAccountNotFound", id);

        Assert.Equal("AP.SUP04", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(id, error.Parameters[0]);
    }

    // DuplicatedBankAccount retorna AP.SUP05 com 3 parâmetros (bankCode/branch/accountNumber).
    [Fact]
    public void DuplicatedBankAccount_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("DuplicatedBankAccount", "001", "0001", "123456-7");

        Assert.Equal("AP.SUP05", error.Id);
        Assert.Equal(3, error.Parameters.Count);
    }

    // Todos os Ids da factory são únicos (proteção contra duplicidade acidental ao adicionar erro novo).
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("InvalidStatusTransition", "A", "B").Id,
            Invoke("ReasonRequired").Id,
            Invoke("CannotRemoveLastBankAccountWhileActive").Id,
            Invoke("BankAccountNotFound", Guid.NewGuid()).Id,
            Invoke("DuplicatedBankAccount", "1", "2", "3").Id,
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
