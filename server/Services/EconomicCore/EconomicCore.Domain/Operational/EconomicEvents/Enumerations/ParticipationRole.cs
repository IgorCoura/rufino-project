namespace EconomicCore.Domain.Operational.EconomicEvents.Enumerations;

using EconomicCore.Domain.SeedWork;

public sealed class ParticipationRole : Enumeration
{
    public static readonly ParticipationRole Provider = new(1, "PROVIDER");
    public static readonly ParticipationRole Recipient = new(2, "RECIPIENT");

    private ParticipationRole(int id, string name) : base(id, name) { }
}
