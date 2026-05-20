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

    // AP.PAY07 (RequiresApproval) foi removido — o threshold saiu do Aggregate e a decisão de exigir
    // aprovação passa a viver na Application (ApprovalRequirementCalculator + transição AwaitingApproval).
    // Slot reservado: não reutilizar sem confirmar que nenhum consumidor externo depende do Id antigo.

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

    // InstallmentNumberMustBePositive retorna AP.PAY12 com o número recebido — Sprint 8.
    [Fact]
    public void InstallmentNumberMustBePositive_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("InstallmentNumberMustBePositive", 0);

        Assert.Equal("AP.PAY12", error.Id);
        Assert.Single(error.Parameters);
        Assert.Equal(0, error.Parameters[0]);
    }

    // MultiApprovalRequiredCountTooLow retorna AP.PAY13 — Sprint 10.
    [Fact]
    public void MultiApprovalRequiredCountTooLow_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("MultiApprovalRequiredCountTooLow", 0);
        Assert.Equal("AP.PAY13", error.Id);
        Assert.Single(error.Parameters);
    }

    // MultiApprovalEligibleRolesRequired retorna AP.PAY14 — Sprint 10.
    [Fact]
    public void MultiApprovalEligibleRolesRequired_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("MultiApprovalEligibleRolesRequired");
        Assert.Equal("AP.PAY14", error.Id);
        Assert.Empty(error.Parameters);
    }

    // ApproverRoleNotEligible retorna AP.PAY15 com a role — Sprint 10.
    [Fact]
    public void ApproverRoleNotEligible_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("ApproverRoleNotEligible", "FINANCE");
        Assert.Equal("AP.PAY15", error.Id);
        Assert.Equal("FINANCE", error.Parameters[0]);
    }

    // ApproverAlreadyRecorded retorna AP.PAY16 com o approverId — Sprint 10.
    [Fact]
    public void ApproverAlreadyRecorded_ShouldHaveCorrectIdAndParameter()
    {
        var error = Invoke("ApproverAlreadyRecorded", Guid.Empty);
        Assert.Equal("AP.PAY16", error.Id);
        Assert.Single(error.Parameters);
    }

    // SingleApprovalNotAllowedInMultiMode retorna AP.PAY17 — Sprint 10.
    [Fact]
    public void SingleApprovalNotAllowedInMultiMode_ShouldHaveCorrectIdAndNoParameters()
    {
        var error = Invoke("SingleApprovalNotAllowedInMultiMode");
        Assert.Equal("AP.PAY17", error.Id);
        Assert.Empty(error.Parameters);
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
            Invoke("RequiresClassificationBeforeApproval").Id,
            Invoke("RejectionReasonRequired").Id,
            Invoke("PaymentFailureReasonRequired").Id,
            Invoke("PaymentOrderIdMismatch", Guid.Empty, Guid.Empty).Id,
            Invoke("InstallmentNumberMustBePositive", 0).Id,
            Invoke("MultiApprovalRequiredCountTooLow", 0).Id,
            Invoke("MultiApprovalEligibleRolesRequired").Id,
            Invoke("ApproverRoleNotEligible", "X").Id,
            Invoke("ApproverAlreadyRecorded", Guid.Empty).Id,
            Invoke("SingleApprovalNotAllowedInMultiMode").Id,
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
