namespace EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class ContractDirection : Enumeration
{
    public static readonly ContractDirection Acquisition = new(1, "ACQUISITION");
    public static readonly ContractDirection Provision = new(2, "PROVISION");

    private ContractDirection(int id, string name) : base(id, name) { }
}
