namespace AccountsPayable.UnitTests.Payables;

using System.Reflection;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;

/// <summary>
/// Factory de erros do Payable (AP.PAY##). Acessada via reflection porque a factory é internal.
/// </summary>
public class PayableErrorsTests
{
    private static readonly Type ERRORS_TYPE =
        typeof(Payable).Assembly.GetType("AccountsPayable.Domain.Errors.PayableErrors")
        ?? throw new InvalidOperationException("PayableErrors type not found.");

    // InvalidStatusTransition retorna AP.PAY01 com 2 parâmetros (status atual + alvo).
    [Fact]
    public void InvalidStatusTransition_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("InvalidStatusTransition", "DRAFT", "PAID");

        Assert.Equal("AP.PAY01", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.NotEmpty(error.MessageTemplate);
        Assert.NotEmpty(error.SourcePath);
    }

    // DueDateInPast retorna AP.PAY02 com dueDate + today nos parâmetros.
    [Fact]
    public void DueDateInPast_ShouldHaveCorrectIdAndParameters()
    {
        var dueDate = new DateOnly(2023, 1, 1);
        var today = new DateOnly(2024, 5, 1);

        var error = Invoke("DueDateInPast", dueDate, today);

        Assert.Equal("AP.PAY02", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal(dueDate, error.Parameters[0]);
        Assert.Equal(today, error.Parameters[1]);
    }

    // ReasonRequired retorna AP.PAY03 sem parâmetros.
    [Fact]
    public void ReasonRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("ReasonRequired");

        Assert.Equal("AP.PAY03", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CannotPayCancelled retorna AP.PAY04 sem parâmetros — erro dedicado conforme critério de aceite da Sprint 2.
    [Fact]
    public void CannotPayCancelled_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("CannotPayCancelled");

        Assert.Equal("AP.PAY04", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CannotScheduleWithoutClassification retorna AP.PAY05 sem parâmetros — Sprint 4.
    [Fact]
    public void CannotScheduleWithoutClassification_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("CannotScheduleWithoutClassification");

        Assert.Equal("AP.PAY05", error.Id);
        Assert.Empty(error.Parameters);
    }

    // CannotClassifyTerminalPayable retorna AP.PAY06 com o status atual como parâmetro — Sprint 4.
    [Fact]
    public void CannotClassifyTerminalPayable_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("CannotClassifyTerminalPayable", "PAID");

        Assert.Equal("AP.PAY06", error.Id);
        Assert.Equal("PAID", error.Parameters[0]);
    }

    // RequiresApproval retorna AP.PAY07 com amount + threshold no payload — Sprint 5.
    [Fact]
    public void RequiresApproval_ShouldHaveCorrectIdAndParameters()
    {
        var error = Invoke("RequiresApproval", 1500m, 1000m);

        Assert.Equal("AP.PAY07", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal(1500m, error.Parameters[0]);
        Assert.Equal(1000m, error.Parameters[1]);
    }

    // RequiresClassificationBeforeApproval retorna AP.PAY08 sem parâmetros — Sprint 5.
    [Fact]
    public void RequiresClassificationBeforeApproval_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("RequiresClassificationBeforeApproval");

        Assert.Equal("AP.PAY08", error.Id);
        Assert.Empty(error.Parameters);
    }

    // RejectionReasonRequired retorna AP.PAY09 sem parâmetros — Sprint 5.
    [Fact]
    public void RejectionReasonRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("RejectionReasonRequired");

        Assert.Equal("AP.PAY09", error.Id);
        Assert.Empty(error.Parameters);
    }

    // PaymentFailureReasonRequired retorna AP.PAY10 sem parâmetros — Sprint 6.
    [Fact]
    public void PaymentFailureReasonRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("PaymentFailureReasonRequired");

        Assert.Equal("AP.PAY10", error.Id);
        Assert.Empty(error.Parameters);
    }

    // PaymentOrderIdMismatch retorna AP.PAY11 com expected + received PaymentOrder Guids — Sprint 6.
    [Fact]
    public void PaymentOrderIdMismatch_ShouldHaveCorrectIdAndParameters()
    {
        var expected = new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var received = new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var error = Invoke("PaymentOrderIdMismatch", expected, received);

        Assert.Equal("AP.PAY11", error.Id);
        Assert.Equal(2, error.Parameters.Count);
        Assert.Equal(expected, error.Parameters[0]);
        Assert.Equal(received, error.Parameters[1]);
    }

    // Todos os Ids são únicos (proteção contra duplicidade acidental).
    [Fact]
    public void AllErrors_ShouldHaveUniqueIds()
    {
        var ids = new[]
        {
            Invoke("InvalidStatusTransition", "A", "B").Id,
            Invoke("DueDateInPast", new DateOnly(2023, 1, 1), new DateOnly(2024, 1, 1)).Id,
            Invoke("ReasonRequired").Id,
            Invoke("CannotPayCancelled").Id,
            Invoke("CannotScheduleWithoutClassification").Id,
            Invoke("CannotClassifyTerminalPayable", "PAID").Id,
            Invoke("RequiresApproval", 1m, 1m).Id,
            Invoke("RequiresClassificationBeforeApproval").Id,
            Invoke("RejectionReasonRequired").Id,
            Invoke("PaymentFailureReasonRequired").Id,
            Invoke("PaymentOrderIdMismatch", Guid.Empty, Guid.Empty).Id,
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
