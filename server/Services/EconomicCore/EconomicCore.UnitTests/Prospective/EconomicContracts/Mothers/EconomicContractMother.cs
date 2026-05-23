namespace EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicContractMother
{
    public static readonly DateTime FixedOccurredAt = new(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly TenantId FixedTenantId = TenantId.From(new Guid("11111111-1111-7111-8111-111111111111"));
    public static readonly EconomicContractId FixedContractId = EconomicContractId.From(new Guid("88888888-8888-7888-8888-888888888888"));
    public static readonly EconomicAgentId FixedCounterpartyId = EconomicAgentId.From(new Guid("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa"));
    public static readonly CommitmentId OutflowCommitmentIdSlot1 = CommitmentId.From(new Guid("aaaa0001-0001-7001-8001-aaaaaaaaaaaa"));
    public static readonly CommitmentId InflowCommitmentIdSlot1 = CommitmentId.From(new Guid("bbbb0001-0001-7001-8001-bbbbbbbbbbbb"));
    public static readonly CommitmentId OutflowCommitmentIdSlot2 = CommitmentId.From(new Guid("aaaa0002-0002-7002-8002-aaaaaaaaaaaa"));
    public static readonly CommitmentId InflowCommitmentIdSlot2 = CommitmentId.From(new Guid("bbbb0002-0002-7002-8002-bbbbbbbbbbbb"));

    public static Money DefaultExpectedAmount() => new(1000m, Currency.BRL);

    public static RecurrencePattern DefaultRecurrence() => new(Periodicity.Monthly, anchorDay: 5);

    public static CommitmentTerms DefaultTerms() => new(DefaultExpectedAmount(), tolerancePercent: 0.05m, windowDays: 15);

    public static CompetencePeriod October2025() => new(2025, 10);
    public static CompetencePeriod November2025() => new(2025, 11);

    private EconomicContractId _id = FixedContractId;
    private TenantId _tenantId = FixedTenantId;
    private EconomicAgentId _counterpartyId = FixedCounterpartyId;
    private ContractDirection _direction = ContractDirection.Acquisition;
    private RecurrencePattern _recurrence = DefaultRecurrence();
    private CommitmentTerms _terms = DefaultTerms();
    private DateTime _occurredAt = FixedOccurredAt;

    public static EconomicContractMother New() => new();

    public EconomicContractMother WithId(EconomicContractId id)
    {
        _id = id;
        return this;
    }

    public EconomicContractMother WithDirection(ContractDirection direction)
    {
        _direction = direction;
        return this;
    }

    public EconomicContractMother WithRecurrence(RecurrencePattern recurrence)
    {
        _recurrence = recurrence;
        return this;
    }

    public EconomicContractMother WithTerms(CommitmentTerms terms)
    {
        _terms = terms;
        return this;
    }

    public EconomicContract Build()
        => EconomicContract.Create(_id, _tenantId, _counterpartyId, _direction, _recurrence, _terms, _occurredAt);
}
