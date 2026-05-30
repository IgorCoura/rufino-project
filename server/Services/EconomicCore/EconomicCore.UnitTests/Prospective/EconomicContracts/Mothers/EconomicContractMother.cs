namespace EconomicCore.UnitTests.Prospective.EconomicContracts.Mothers;

using System.Reflection;
using EconomicCore.Domain.Operational.EconomicAgents;
using EconomicCore.Domain.Operational.EconomicResources;
using EconomicCore.Domain.Prospective.EconomicContracts;
using EconomicCore.Domain.Prospective.EconomicContracts.Enumerations;
using EconomicCore.Domain.Prospective.EconomicContracts.ValueObjects;
using EconomicCore.Domain.SharedKernel;

public sealed class EconomicContractMother
{
    public static readonly DateTime FixedOccurredAt = new(2025, 10, 1, 10, 0, 0, DateTimeKind.Utc);
    public static readonly DateOnly FixedStartDate = new(2025, 10, 1);
    public static readonly TenantId FixedTenantId = TenantId.From(new Guid("11111111-1111-7111-8111-111111111111"));
    public static readonly EconomicContractId FixedContractId = EconomicContractId.From(new Guid("88888888-8888-7888-8888-888888888888"));
    public static readonly EconomicAgentId FixedCounterpartyId = EconomicAgentId.From(new Guid("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa"));
    public static readonly EconomicResourceId FixedResourceId = EconomicResourceId.From(new Guid("cccccccc-cccc-7ccc-8ccc-cccccccccccc"));
    public static readonly EconomicResourceId CondominiumResourceId = EconomicResourceId.From(new Guid("dddddddd-dddd-7ddd-8ddd-dddddddddddd"));
    public static readonly EconomicResourceId PropertyTaxResourceId = EconomicResourceId.From(new Guid("eeeeeeee-eeee-7eee-8eee-eeeeeeeeeeee"));
    public static readonly EconomicAgentId CondominiumRecipientId = EconomicAgentId.From(new Guid("ffffffff-ffff-7fff-8fff-ffffffffffff"));
    public static readonly EconomicAgentId MunicipalityRecipientId = EconomicAgentId.From(new Guid("a1a1a1a1-a1a1-7a1a-8a1a-a1a1a1a1a1a1"));
    public static readonly CommitmentId OutflowCommitmentIdSlot1 = CommitmentId.From(new Guid("aaaa0001-0001-7001-8001-aaaaaaaaaaaa"));
    public static readonly CommitmentId InflowCommitmentIdSlot1 = CommitmentId.From(new Guid("bbbb0001-0001-7001-8001-bbbbbbbbbbbb"));
    public static readonly CommitmentId OutflowCommitmentIdSlot2 = CommitmentId.From(new Guid("aaaa0002-0002-7002-8002-aaaaaaaaaaaa"));
    public static readonly CommitmentId InflowCommitmentIdSlot2 = CommitmentId.From(new Guid("bbbb0002-0002-7002-8002-bbbbbbbbbbbb"));

    public const int DEFAULT_TERM_MONTHS = 12;

    public static Money DefaultExpectedAmount() => new(1000m, Currency.BRL);

    public static RecurrencePattern DefaultRecurrence() => new(Periodicity.Monthly, anchorDay: 5);

    public static CommitmentTerms DefaultTerms() => new(DefaultExpectedAmount(), tolerancePercent: 0.05m, windowDays: 15);

    public static CompetencePeriod October2025() => new(2025, 10);
    public static CompetencePeriod November2025() => new(2025, 11);

    public static ContractCharge CondominiumCharge(decimal amount = 300m, bool collectedByCounterparty = true)
        => new(CommitmentPurpose.Condominium, new Money(amount, Currency.BRL), CondominiumResourceId, CondominiumRecipientId, collectedByCounterparty);

    public static ContractCharge PropertyTaxCharge(decimal amount = 200m, bool collectedByCounterparty = false)
        => new(CommitmentPurpose.PropertyTax, new Money(amount, Currency.BRL), PropertyTaxResourceId, MunicipalityRecipientId, collectedByCounterparty);

    // Decompõe um ContractCharge (VO) nos primitivos do método rico AddCharge — mantém os testes legíveis.
    public static void AddChargeFrom(EconomicContract contract, ContractCharge charge, DateTime occurredAt)
        => contract.AddCharge(
            charge.Purpose, charge.ExpectedAmount.Amount, charge.ExpectedAmount.Currency,
            charge.ResourceId, charge.RecipientAgentId, charge.CollectedByCounterparty, occurredAt);

    private EconomicContractId _id = FixedContractId;
    private TenantId _tenantId = FixedTenantId;
    private EconomicAgentId _counterpartyId = FixedCounterpartyId;
    private EconomicResourceId _resourceId = FixedResourceId;
    private ContractDirection _direction = ContractDirection.Acquisition;
    private RecurrencePattern _recurrence = DefaultRecurrence();
    private CommitmentTerms _terms = DefaultTerms();
    private int _termMonths = DEFAULT_TERM_MONTHS;
    private DateOnly _startDate = FixedStartDate;
    private DateTime _occurredAt = FixedOccurredAt;
    private readonly List<ContractCharge> _extraCharges = [];

    public static EconomicContractMother New() => new();

    public EconomicContractMother WithId(EconomicContractId id)
    {
        _id = id;
        return this;
    }

    public EconomicContractMother WithResourceId(EconomicResourceId resourceId)
    {
        _resourceId = resourceId;
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

    public EconomicContractMother WithTermMonths(int termMonths)
    {
        _termMonths = termMonths;
        return this;
    }

    public EconomicContractMother WithStartDate(DateOnly startDate)
    {
        _startDate = startDate;
        return this;
    }

    public EconomicContractMother WithCharge(ContractCharge charge)
    {
        _extraCharges.Add(charge);
        return this;
    }

    public EconomicContract Build()
    {
        var contract = EconomicContract.Create(_id, _tenantId, _counterpartyId, _resourceId, _direction, _recurrence, _terms, _termMonths, _startDate, _occurredAt);
        foreach (var charge in _extraCharges)
            contract.AddCharge(
                charge.Purpose, charge.ExpectedAmount.Amount, charge.ExpectedAmount.Currency,
                charge.ResourceId, charge.RecipientAgentId, charge.CollectedByCounterparty, _occurredAt);
        return contract;
    }

    // Atalho de teste: cria contrato em Draft e força status Active via reflection,
    // sem materializar commitments. Útil para isolar testes de GenerateCommitmentsFor,
    // MarkFulfilled, Suspend/Resume/Terminate sem depender de Activate().
    public EconomicContract BuildActiveEmpty()
    {
        var contract = Build();
        var statusProp = typeof(EconomicContract).GetProperty(
            nameof(EconomicContract.Status),
            BindingFlags.Public | BindingFlags.Instance)!;
        statusProp.SetValue(contract, ContractStatus.Active);
        return contract;
    }
}
