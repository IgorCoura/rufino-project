namespace AccountsPayable.UnitTests.Services;

using AccountsPayable.Domain.InstallmentPlans;
using AccountsPayable.Domain.InstallmentPlans.Enumerations;
using AccountsPayable.Domain.Payables.Enumerations;
using AccountsPayable.Domain.Payables.ValueObjects;
using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Services;
using AccountsPayable.Domain.Suppliers;

public class InstallmentPlanFactoryTests
{
    private static readonly DateTime FIXED_NOW = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly FIRST_DUE_DATE = new(2024, 2, 15);
    private static readonly TenantId TENANT = TenantId.From(new Guid("11111111-1111-1111-1111-111111111111"));
    private static readonly SupplierId SUPPLIER = SupplierId.From(new Guid("22222222-2222-2222-2222-222222222222"));

    private readonly InstallmentPlanFactory _sut = new();

    private InstallmentPlanFactoryResult Build(
        decimal totalAmount = 12_000m,
        int installmentCount = 12,
        InstallmentFrequency? frequency = null) =>
        _sut.Create(
            planId: InstallmentPlanId.New(),
            tenantId: TENANT,
            supplierId: SUPPLIER,
            totalAmount: new Money(totalAmount, Currency.Brl),
            installmentCount: installmentCount,
            firstDueDate: FIRST_DUE_DATE,
            frequency: frequency ?? InstallmentFrequency.Monthly,
            description: new Description("Aluguel anual"),
            occurredAt: FIXED_NOW);

    public class WhenBuilding
    {
        // Factory cria um InstallmentPlan + N Payables, todos com PlanId/InstallmentNumber populados.
        [Fact]
        public void Create_WithValidInputs_ShouldProduceLinkedPlanAndPayables()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(12_000m, Currency.Brl),
                installmentCount: 12,
                firstDueDate: FIRST_DUE_DATE,
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Aluguel anual"),
                occurredAt: FIXED_NOW);

            Assert.Equal(12, result.Payables.Count);
            Assert.Equal(12, result.Plan.PayableIds.Count);
            Assert.All(result.Payables, p => Assert.Equal(result.Plan.Id, p.InstallmentPlanId));
            // InstallmentNumber é 1-based e cobre [1..12].
            Assert.Equal(
                Enumerable.Range(1, 12),
                result.Payables.Select(p => p.InstallmentNumber!.Value).OrderBy(n => n));
            // Cada PayableId está registrado no plano.
            Assert.All(result.Payables, p => Assert.Contains(p.Id, result.Plan.PayableIds));
        }

        // Critério de aceite Sprint 8: soma das parcelas == total quando divide certo.
        [Fact]
        public void Create_WithExactDivision_ShouldDistributeEvenly()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(12_000m, Currency.Brl),
                installmentCount: 12,
                firstDueDate: FIRST_DUE_DATE,
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Aluguel anual"),
                occurredAt: FIXED_NOW);

            Assert.All(result.Payables, p => Assert.Equal(1_000m, p.Amount.Amount));
            Assert.Equal(12_000m, result.Payables.Sum(p => p.Amount.Amount));
        }

        // Critério de aceite Sprint 8: 1000/3 → [333.33, 333.33, 333.34] (resíduo de centavo na ÚLTIMA parcela).
        [Fact]
        public void Create_WithInexactDivision_ShouldPlaceCentResidueOnLastInstallment()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(1_000m, Currency.Brl),
                installmentCount: 3,
                firstDueDate: FIRST_DUE_DATE,
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Aluguel trimestral"),
                occurredAt: FIXED_NOW);

            var ordered = result.Payables.OrderBy(p => p.InstallmentNumber).ToList();
            Assert.Equal(333.33m, ordered[0].Amount.Amount);
            Assert.Equal(333.33m, ordered[1].Amount.Amount);
            Assert.Equal(333.34m, ordered[2].Amount.Amount);
            Assert.Equal(1_000m, ordered.Sum(p => p.Amount.Amount));
        }

        // Frequência mensal: dueDates espaçados em meses a partir de firstDueDate.
        [Fact]
        public void Create_WithMonthlyFrequency_ShouldSpaceDueDatesByOneMonth()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(3_000m, Currency.Brl),
                installmentCount: 3,
                firstDueDate: new DateOnly(2024, 1, 31),
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Plano X"),
                occurredAt: FIXED_NOW);

            var ordered = result.Payables.OrderBy(p => p.InstallmentNumber).ToList();
            Assert.Equal(new DateOnly(2024, 1, 31), ordered[0].DueDate.Value);
            Assert.Equal(new DateOnly(2024, 2, 29), ordered[1].DueDate.Value); // ano bissexto — fevereiro com 29
            Assert.Equal(new DateOnly(2024, 3, 31), ordered[2].DueDate.Value);
        }

        // Frequência semanal: dueDates espaçados em 7 dias a partir de firstDueDate.
        [Fact]
        public void Create_WithWeeklyFrequency_ShouldSpaceDueDatesBySevenDays()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(800m, Currency.Brl),
                installmentCount: 4,
                firstDueDate: new DateOnly(2024, 2, 1),
                frequency: InstallmentFrequency.Weekly,
                description: new Description("Plano semanal"),
                occurredAt: FIXED_NOW);

            var ordered = result.Payables.OrderBy(p => p.InstallmentNumber).ToList();
            Assert.Equal(new DateOnly(2024, 2, 1), ordered[0].DueDate.Value);
            Assert.Equal(new DateOnly(2024, 2, 8), ordered[1].DueDate.Value);
            Assert.Equal(new DateOnly(2024, 2, 15), ordered[2].DueDate.Value);
            Assert.Equal(new DateOnly(2024, 2, 22), ordered[3].DueDate.Value);
        }

        // Descrição de cada Payable é prefixada com "(N/M)" — UX clara para o usuário no app.
        [Fact]
        public void Create_ShouldAppendInstallmentSuffixToEachPayableDescription()
        {
            var factory = new InstallmentPlanFactory();
            var result = factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(600m, Currency.Brl),
                installmentCount: 3,
                firstDueDate: FIRST_DUE_DATE,
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Aluguel"),
                occurredAt: FIXED_NOW);

            var ordered = result.Payables.OrderBy(p => p.InstallmentNumber).ToList();
            Assert.Equal("Aluguel (1/3)", ordered[0].Description.Value);
            Assert.Equal("Aluguel (2/3)", ordered[1].Description.Value);
            Assert.Equal("Aluguel (3/3)", ordered[2].Description.Value);
        }

        // Factory propaga AP.IPL01 quando InstallmentCount < 2 (validação acontece dentro de InstallmentPlan.Create).
        [Fact]
        public void Create_WithCountBelowMinimum_ShouldPropagateInstallmentCountError()
        {
            var factory = new InstallmentPlanFactory();

            var ex = Assert.Throws<DomainException>(() => factory.Create(
                planId: InstallmentPlanId.New(),
                tenantId: TENANT,
                supplierId: SUPPLIER,
                totalAmount: new Money(1_000m, Currency.Brl),
                installmentCount: 1,
                firstDueDate: FIRST_DUE_DATE,
                frequency: InstallmentFrequency.Monthly,
                description: new Description("Plano inválido"),
                occurredAt: FIXED_NOW));

            Assert.Equal("AP.IPL01", ex.Id);
        }
    }
}
