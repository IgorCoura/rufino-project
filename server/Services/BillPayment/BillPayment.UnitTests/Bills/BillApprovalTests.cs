namespace BillPayment.UnitTests.Bills;

using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using BillPayment.UnitTests.Bills.Mothers;
using BillPayment.UnitTests.Services.Mothers;

/// <summary>
/// A decisão humana: aprovar, recusar, cancelar — e as seis guardas que protegem a aprovação.
/// </summary>
public class BillApprovalTests
{
    private static readonly UserId Approver = UserId.From(new Guid("0195a1f0-0000-7000-8000-00000000000a"));
    private static readonly DateTime DecidedAt = new(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 6, 20);
    private static readonly DateOnly ScheduleFor = new(2026, 6, 24);

    // Caminho feliz: o boleto verificado e limpo é aprovado, a decisão fica gravada com quem
    // decidiu, e o evento que a fase 3 consome é emitido.
    [Fact]
    public void Approve_OnACleanBill_ShouldRecordWhoDecidedAndEmitApproved()
    {
        var bill = ReadyForApproval();

        bill.Approve(Approver, ScheduleFor, "confere com o contrato", Policy(), Today, DecidedAt);

        Assert.Equal(BillStatus.Approved, bill.Status);
        Assert.Equal(ScheduleFor, bill.ScheduledFor);
        Assert.Equal(Approver, bill.Approval!.DecidedBy);
        Assert.Equal(ApprovalDecision.Approved, bill.Approval.Decision);
        Assert.Equal("confere com o contrato", bill.Approval.Note);

        var approved = Assert.IsType<BillApprovedDomainEvent>(Assert.Single(bill.PullDomainEvents()));
        Assert.Equal(Approver, approved.ApprovedBy);
        Assert.Equal(ScheduleFor, approved.ScheduleFor);
    }

    // Invariante 3, pelo caminho que existe: boleto recém-capturado, sem verificação nenhuma,
    // não é aprovável. (A guarda BLP.BIL03 — catálogo incompleto — protege o dia em que um
    // CheckType novo for acrescentado e boletos antigos ficarem com 11 de 12; esse estado não
    // tem como ser produzido hoje, porque RecordChecks recusa conjunto parcial.)
    [Fact]
    public void Approve_BeforeAnyValidation_ShouldNotBeAllowed()
    {
        var bill = BillMother.Capture();

        var ex = Assert.Throws<DomainException>(
            () => bill.Approve(Approver, ScheduleFor, null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL25", ex.Id);
        Assert.Empty(bill.Checks);
    }

    // Falha bloqueante não é aprovável. Na prática a defesa que dispara é a de situação: um
    // boleto com bloqueio já está em Rejected, porque RecordChecks o levou para lá. A guarda
    // BLP.BIL04 fica atrás dela, como defesa em profundidade sobre a operação mais perigosa
    // do sistema — o que importa aqui é que a aprovação não acontece.
    [Fact]
    public void Approve_WithABlockingFailure_ShouldNotBeAllowed()
    {
        var bill = ReadyForApproval();
        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.PayeeMatch, CheckReasons.PAYEE_LOOKALIKE)), DecidedAt);
        bill.PullDomainEvents();

        var ex = Assert.Throws<DomainException>(
            () => bill.Approve(Approver, ScheduleFor, null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL25", ex.Id);
        Assert.Equal(BillStatus.Rejected, bill.Status);
        Assert.Null(bill.Approval);
    }

    // Falha apenas advisory NÃO impede a aprovação — o aprovador assume o risco e a decisão
    // fica gravada. É a diferença entre as duas severidades.
    [Fact]
    public void Approve_WithOnlyAnAdvisoryFailure_ShouldBeAllowed()
    {
        var bill = ReadyForApproval();
        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.AmountMatch, CheckReasons.AMOUNT_OUTSIDE_POLICY)), DecidedAt);
        bill.PullDomainEvents();

        bill.Approve(Approver, ScheduleFor, "valor conferido com o fornecedor", Policy(), Today, DecidedAt);

        Assert.Equal(BillStatus.Approved, bill.Status);
    }

    // Invariante 6: aprovar contra retrato velho é consentir com um número que já não é o que
    // será debitado — BLP.BIL06.
    [Fact]
    public void Approve_WithAStaleSnapshot_ShouldThrow_BLP_BIL06()
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            Approver, ScheduleFor, null, Policy(), Today, DecidedAt.AddHours(30)));

        Assert.Equal("BLP.BIL06", ex.Id);
    }

    // Invariante 5: data no passado não é agendável — BLP.BIL05.
    [Fact]
    public void Approve_WithAScheduleDateInThePast_ShouldThrow_BLP_BIL05()
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            Approver, Today.AddDays(-1), null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL05", ex.Id);
    }

    // Invariante 5, o outro lado: o piso do provedor também vale.
    [Fact]
    public void Approve_BeforeTheProviderMinimumDate_ShouldThrow_BLP_BIL05()
    {
        var minimum = Today.AddDays(3);
        var bill = ReadyForApproval(ValidationMother.ConsistentWithBarcode(minimumScheduleDate: minimum));

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            Approver, Today.AddDays(1), null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL05", ex.Id);
    }

    // Alçada: o mesmo boleto pode ser aprovável por uma pessoa e não por outra — BLP.BIL24.
    [Fact]
    public void Approve_AboveTheApproverLimit_ShouldThrow_BLP_BIL24()
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            Approver, ScheduleFor, null, Policy(limit: 100m), Today, DecidedAt));

        Assert.Equal("BLP.BIL24", ex.Id);
    }

    // Teto nulo significa sem teto, não zero.
    [Fact]
    public void Approve_WithoutAnyLimit_ShouldBeAllowedForAnyAmount()
    {
        var bill = ReadyForApproval();

        bill.Approve(Approver, ScheduleFor, null, Policy(), Today, DecidedAt);

        Assert.Equal(BillStatus.Approved, bill.Status);
    }

    // ADR-007: nenhum pagamento sem um UserId identificando quem autorizou — BLP.BIL22.
    [Fact]
    public void Approve_WithoutAnApprover_ShouldThrow_BLP_BIL22()
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.Approve(
            UserId.Empty, ScheduleFor, null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL22", ex.Id);
    }

    // Recusar é terminal e exige motivo — é o desvio, e é dele que alguém vai querer entender
    // a razão depois.
    [Fact]
    public void Deny_ShouldBeTerminalAndRecordTheReason()
    {
        var bill = ReadyForApproval();

        bill.Deny(Approver, "beneficiário não é nosso fornecedor", DecidedAt);

        Assert.Equal(BillStatus.Denied, bill.Status);
        Assert.True(bill.Status.IsTerminal);
        Assert.Equal("beneficiário não é nosso fornecedor", bill.Approval!.Note);
        Assert.IsType<BillDeniedDomainEvent>(Assert.Single(bill.PullDomainEvents()));
    }

    // Recusa sem motivo não é registro de nada — BLP.BIL23.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deny_WithoutAReason_ShouldThrow_BLP_BIL23(string reason)
    {
        var bill = ReadyForApproval();

        var ex = Assert.Throws<DomainException>(() => bill.Deny(Approver, reason, DecidedAt));

        Assert.Equal("BLP.BIL23", ex.Id);
    }

    // Cancelar alcança boleto que nem chegou a ser verificado — é o que resolve importação por
    // engano, e é a diferença para recusar.
    [Fact]
    public void Cancel_OnAJustCapturedBill_ShouldBeAllowed()
    {
        var bill = BillMother.Capture();
        bill.PullDomainEvents();

        bill.Cancel(Approver, "importado por engano", DecidedAt);

        Assert.Equal(BillStatus.Cancelled, bill.Status);
        Assert.IsType<BillCancelledDomainEvent>(Assert.Single(bill.PullDomainEvents()));
    }

    // Cancelar libera a chave natural: o documento pode ser reimportado depois.
    [Fact]
    public void Cancel_ShouldReleaseTheNaturalKey()
    {
        var bill = BillMother.Capture();
        bill.Cancel(Approver, "importado por engano", DecidedAt);

        Assert.False(bill.Status.OccupiesNaturalKey);
    }

    // Boleto já recusado não aceita nova decisão — estado terminal — BLP.BIL25.
    [Fact]
    public void Approve_AfterDenial_ShouldThrow_BLP_BIL25()
    {
        var bill = ReadyForApproval();
        bill.Deny(Approver, "não reconheço este boleto", DecidedAt);

        var ex = Assert.Throws<DomainException>(
            () => bill.Approve(Approver, ScheduleFor, null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL25", ex.Id);
    }

    // Boleto reprovado não é aprovável direto: precisa de revalidação limpa antes — BLP.BIL25.
    [Fact]
    public void Approve_OnARejectedBill_ShouldThrow_BLP_BIL25()
    {
        var bill = ReadyForApproval();
        bill.RecordChecks(AllPassing(CheckResult.Failed(CheckType.Duplicate, CheckReasons.DUPLICATE_SAME_TENANT)), DecidedAt);
        bill.PullDomainEvents();

        var ex = Assert.Throws<DomainException>(
            () => bill.Approve(Approver, ScheduleFor, null, Policy(), Today, DecidedAt));

        Assert.Equal("BLP.BIL25", ex.Id);
    }

    // A validade do retrato é medida a partir da consulta mais recente.
    [Fact]
    public void IsLookupStaleAt_ShouldMeasureFromTheMostRecentConsultation()
    {
        var bill = ReadyForApproval();

        Assert.False(bill.IsLookupStaleAt(ValidationMother.ConsultedAt.AddHours(11), TimeSpan.FromHours(12)));
        Assert.True(bill.IsLookupStaleAt(ValidationMother.ConsultedAt.AddHours(13), TimeSpan.FromHours(12)));
    }

    private static ApprovalPolicy Policy(decimal? limit = null)
        => ApprovalPolicy.Default(limit is null ? null : new Money(limit.Value, Currency.BRL));

    /// <summary>Boleto consultado, verificado e sem nenhuma pendência — pronto para decisão.</summary>
    private static Bill ReadyForApproval(Domain.Lookups.LookupSnapshot? snapshot = null)
    {
        var bill = ValidationMother.BankSlipWithLookup(snapshot);
        bill.RecordChecks(AllPassing(), DecidedAt);
        bill.PullDomainEvents();
        return bill;
    }

    private static List<CheckResult> AllPassing(params CheckResult[] overrides)
    {
        var results = Enumeration.GetAll<CheckType>()
            .Where(type => !overrides.Any(o => o.Type == type))
            .Select(type => CheckResult.Passed(type))
            .ToList();

        results.AddRange(overrides);
        return results;
    }
}
