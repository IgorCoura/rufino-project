namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.ExpectedRecurringBills;
using AccountsPayable.Domain.Payables;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers;
using AccountsPayable.UnitTests.ExpectedRecurringBills.Mothers;
using AccountsPayable.UnitTests.Payables.Mothers;

public class RecurringBillMatcherTests
{
    private readonly RecurringBillMatcher _sut = new(); // ±5% default

    // Sem candidatos retorna null.
    [Fact]
    public void FindMatch_WithNoCandidates_ShouldReturnNull()
    {
        var payable = PayableMother.Draft();

        var match = _sut.FindMatch(payable, Array.Empty<ExpectedRecurringBill>());

        Assert.Null(match);
    }

    // Casa quando supplier+ano-mês+valor batem (dentro da tolerância).
    [Fact]
    public void FindMatch_WithExactSupplierMonthAmount_ShouldReturnTheBill()
    {
        var bill = ExpectedRecurringBillMother.Pending(
            expectedAmount: 5_000m,
            dueDate: new DateOnly(2024, 2, 10),
            supplierId: PayableMother.DEFAULT_SUPPLIER,
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft(amount: 5_000m, dueDate: new DateOnly(2024, 2, 15));

        var match = _sut.FindMatch(payable, new[] { bill });

        Assert.NotNull(match);
        Assert.Equal(bill.Id, match!.Id);
    }

    // SupplierId diferente não casa.
    [Fact]
    public void FindMatch_WithDifferentSupplier_ShouldReturnNull()
    {
        var otherSupplier = SupplierId.From(new Guid("99999999-9999-9999-9999-999999999999"));
        var bill = ExpectedRecurringBillMother.Pending(
            supplierId: otherSupplier,
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft();

        Assert.Null(_sut.FindMatch(payable, new[] { bill }));
    }

    // Mês diferente não casa (mesmo se valor e supplier coincidem).
    [Fact]
    public void FindMatch_WithDifferentYearMonth_ShouldReturnNull()
    {
        var bill = ExpectedRecurringBillMother.Pending(
            dueDate: new DateOnly(2024, 3, 10),
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft(dueDate: new DateOnly(2024, 4, 15));

        Assert.Null(_sut.FindMatch(payable, new[] { bill }));
    }

    // Critério de aceite Sprint 11: matching tolera ±5% de variação no valor.
    [Theory]
    [InlineData(5_000, true)]    // exato
    [InlineData(4_750, true)]    // -5% (limite)
    [InlineData(5_250, true)]    // +5% (limite)
    [InlineData(4_700, false)]   // -6% (fora)
    [InlineData(5_300, false)]   // +6% (fora)
    public void FindMatch_ShouldRespectAmountTolerance(decimal payableAmount, bool shouldMatch)
    {
        var bill = ExpectedRecurringBillMother.Pending(
            expectedAmount: 5_000m,
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft(amount: payableAmount, dueDate: ExpectedRecurringBillMother.DEFAULT_DUE_DATE);

        var match = _sut.FindMatch(payable, new[] { bill });

        Assert.Equal(shouldMatch, match is not null);
    }

    // Bill não-Pending (Matched/Missed/Cancelled) é ignorado mesmo se daria match.
    [Fact]
    public void FindMatch_WithNonPendingBill_ShouldReturnNull()
    {
        var bill = ExpectedRecurringBillMother.Pending(tenantId: PayableMother.DEFAULT_TENANT);
        bill.MarkMissed(ExpectedRecurringBillMother.DEFAULT_OCCURRED_AT.AddMinutes(5));
        var payable = PayableMother.Draft();

        Assert.Null(_sut.FindMatch(payable, new[] { bill }));
    }

    // Bill de outro tenant é ignorado (proteção anti-IDOR).
    [Fact]
    public void FindMatch_WithDifferentTenant_ShouldReturnNull()
    {
        var otherTenant = TenantId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var bill = ExpectedRecurringBillMother.Pending(tenantId: otherTenant);
        var payable = PayableMother.Draft();

        Assert.Null(_sut.FindMatch(payable, new[] { bill }));
    }

    // Entre múltiplos matches válidos, ganha o de menor diferença absoluta de valor.
    [Fact]
    public void FindMatch_WithMultipleMatches_ShouldReturnClosestByAmount()
    {
        var billA = ExpectedRecurringBillMother.Pending(
            id: ExpectedRecurringBillId.From(new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")),
            expectedAmount: 5_100m, // diff vs 4980 = 120
            tenantId: PayableMother.DEFAULT_TENANT);
        var billB = ExpectedRecurringBillMother.Pending(
            id: ExpectedRecurringBillId.From(new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")),
            expectedAmount: 5_000m, // diff vs 4980 = 20 (mais próximo)
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft(amount: 4_980m, dueDate: ExpectedRecurringBillMother.DEFAULT_DUE_DATE);

        var match = _sut.FindMatch(payable, new[] { billA, billB });

        Assert.Equal(billB.Id, match!.Id);
    }

    // Tolerância customizada respeita o valor passado no construtor.
    [Fact]
    public void FindMatch_WithCustomToleranceTenPercent_ShouldExpandAcceptedRange()
    {
        var matcher = new RecurringBillMatcher(amountTolerance: 0.10m);
        var bill = ExpectedRecurringBillMother.Pending(
            expectedAmount: 5_000m,
            tenantId: PayableMother.DEFAULT_TENANT);
        var payable = PayableMother.Draft(amount: 5_400m, dueDate: ExpectedRecurringBillMother.DEFAULT_DUE_DATE);

        // 5400/5000 = +8% → fora dos 5% default, dentro dos 10%.
        Assert.NotNull(matcher.FindMatch(payable, new[] { bill }));
        Assert.Null(_sut.FindMatch(payable, new[] { bill })); // default sut com 5% rejeita
    }
}
