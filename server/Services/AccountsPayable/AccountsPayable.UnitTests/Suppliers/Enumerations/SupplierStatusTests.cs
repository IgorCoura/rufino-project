namespace AccountsPayable.UnitTests.Suppliers.Enumerations;

using AccountsPayable.Domain.SeedWork;
using AccountsPayable.Domain.Suppliers.Enumerations;

public class SupplierStatusTests
{
    // Matriz de transições: cada par (origem, destino) tem resultado esperado conforme regra de SupplierStatus.
    // Transições válidas: Active→Inactive, Active→Blocked, Inactive→Active, Blocked→Active.
    // Todas as demais (incluindo "para si mesmo") devem retornar false.
    [Theory]
    [InlineData(1, 2, true)]   // Active -> Inactive
    [InlineData(1, 3, true)]   // Active -> Blocked
    [InlineData(2, 1, true)]   // Inactive -> Active
    [InlineData(3, 1, true)]   // Blocked -> Active
    [InlineData(1, 1, false)]  // Active -> Active
    [InlineData(2, 2, false)]  // Inactive -> Inactive
    [InlineData(3, 3, false)]  // Blocked -> Blocked
    [InlineData(2, 3, false)]  // Inactive -> Blocked
    [InlineData(3, 2, false)]  // Blocked -> Inactive
    public void CanTransitionTo_ShouldReturnExpectedResult(int fromId, int toId, bool expected)
    {
        var from = Enumeration.FromValue<SupplierStatus>(fromId);
        var to = Enumeration.FromValue<SupplierStatus>(toId);

        var result = from.CanTransitionTo(to);

        Assert.Equal(expected, result);
    }

    // GetAll retorna exatamente as 3 instâncias declaradas (Active, Inactive, Blocked).
    [Fact]
    public void GetAll_ShouldReturnAllThreeStatuses()
    {
        var all = Enumeration.GetAll<SupplierStatus>().ToList();

        Assert.Equal(3, all.Count);
        Assert.Contains(SupplierStatus.Active, all);
        Assert.Contains(SupplierStatus.Inactive, all);
        Assert.Contains(SupplierStatus.Blocked, all);
    }
}
