namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.ExpectedRecurringBills.Enumerations;
using AccountsPayable.Domain.Services;
using AccountsPayable.UnitTests.Contracts.Mothers;

public class RecurringBillGeneratorTests
{
    private static readonly DateTime FIXED_NOW = ContractMother.DEFAULT_OCCURRED_AT;
    private readonly RecurringBillGenerator _sut = new();

    // Critério de aceite Sprint 11: Contract gera 12 ExpectedRecurringBill no primeiro ano a partir da StartDate.
    [Fact]
    public void Generate_ForOneYear_ShouldProduceTwelveBills()
    {
        var contract = ContractMother.Active(monthlyAmount: 5_000m, paymentDay: 10);

        var bills = _sut.Generate(
            contract,
            fromMonth: new DateOnly(2024, 1, 1),
            monthCount: 12,
            occurredAt: FIXED_NOW);

        Assert.Equal(12, bills.Count);
        Assert.All(bills, b => Assert.Equal(ExpectedRecurringBillStatus.Pending, b.Status));
        Assert.All(bills, b => Assert.Equal(5_000m, b.ExpectedAmount.Amount));
        Assert.All(bills, b => Assert.Equal(10, b.ExpectedDueDate.Day)); // paymentDay 10
        Assert.All(bills, b => Assert.Equal(contract.Id, b.ContractId));
    }

    // DueDate é clampado ao último dia do mês quando PaymentDay > dias-no-mês (ex.: 31 em fevereiro).
    [Fact]
    public void Generate_WithPaymentDay31_ShouldClampToLastDayOfShortMonths()
    {
        var contract = ContractMother.Active(paymentDay: 31);

        var bills = _sut.Generate(
            contract,
            fromMonth: new DateOnly(2024, 1, 1), // 2024 é ano bissexto
            monthCount: 3,
            occurredAt: FIXED_NOW);

        Assert.Equal(new DateOnly(2024, 1, 31), bills[0].ExpectedDueDate);
        Assert.Equal(new DateOnly(2024, 2, 29), bills[1].ExpectedDueDate); // fev bissexto = 29
        Assert.Equal(new DateOnly(2024, 3, 31), bills[2].ExpectedDueDate);
    }

    // monthCount=0 (ou negativo) retorna lista vazia sem efeitos colaterais.
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Generate_WithNonPositiveMonthCount_ShouldReturnEmptyList(int count)
    {
        var contract = ContractMother.Active();
        var bills = _sut.Generate(contract, new DateOnly(2024, 1, 1), count, FIXED_NOW);
        Assert.Empty(bills);
    }

    // Cada bill recebe TenantId/SupplierId do Contract.
    [Fact]
    public void Generate_ShouldPropagateTenantAndSupplierFromContract()
    {
        var contract = ContractMother.Active();

        var bills = _sut.Generate(contract, new DateOnly(2024, 1, 1), 1, FIXED_NOW);

        var bill = Assert.Single(bills);
        Assert.Equal(contract.TenantId, bill.TenantId);
        Assert.Equal(contract.SupplierId, bill.SupplierId);
    }
}
