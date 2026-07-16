using PeopleManagement.Application.Commands.DocumentTemplateCommands;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Contrato B da API (Fase 2.4): cada bloco é opcional e a presença dele = regra ativa.
    /// </summary>
    public class PoliciesModelTests
    {
        [Fact]
        public void ToPolicies_WithExpirationBlock_YieldsExpirationPolicyWithDuration()
        {
            var model = new PoliciesModel(Expiration: new ExpirationPolicyModel(DurationInDays: 30));

            var policy = Assert.IsType<ExpirationPolicy>(Assert.Single(model.ToPolicies()));
            Assert.Equal(TimeSpan.FromDays(30), policy.Duration);
        }

        [Fact]
        public void ToPolicies_WithWorkloadBlock_YieldsWorkloadPolicyWithHours()
        {
            var model = new PoliciesModel(Workload: new WorkloadPolicyModel(Hours: 8));

            var policy = Assert.IsType<WorkloadPolicy>(Assert.Single(model.ToPolicies()));
            Assert.Equal(TimeSpan.FromHours(8), policy.Workload);
        }

        [Fact]
        public void ToPolicies_WithBothBlocks_YieldsBothPolicies()
        {
            var model = new PoliciesModel(new ExpirationPolicyModel(30), new WorkloadPolicyModel(8));

            var policies = model.ToPolicies().ToList();

            Assert.Equal(2, policies.Count);
            Assert.Contains(policies, x => x is ExpirationPolicy);
            Assert.Contains(policies, x => x is WorkloadPolicy);
        }

        // "policies": {} = nenhuma regra ativa. Distinto de omitir "policies", que mantém o caminho legado.
        [Fact]
        public void ToPolicies_WithNoBlocks_YieldsEmptySet()
        {
            var model = new PoliciesModel();

            Assert.Empty(model.ToPolicies());
        }

        [Fact]
        public void ToPolicies_WithPeriodBlock_YieldsPeriodPolicyWithTypeAndFlag()
        {
            var model = new PoliciesModel(Period: new PeriodPolicyModel(PeriodType.Monthly.Id, UsePreviousPeriod: true));

            var policy = Assert.IsType<PeriodPolicy>(Assert.Single(model.ToPolicies()));
            Assert.Equal(PeriodType.Monthly, policy.PeriodType);
            Assert.True(policy.UsePreviousPeriod);
        }

        [Fact]
        public void ToPolicies_WithAllThreeBlocks_YieldsAllPolicies()
        {
            var model = new PoliciesModel(
                new ExpirationPolicyModel(30),
                new WorkloadPolicyModel(8),
                new PeriodPolicyModel(PeriodType.Yearly.Id, UsePreviousPeriod: false));

            var policies = model.ToPolicies().ToList();

            Assert.Equal(3, policies.Count);
            Assert.Contains(policies, x => x is PeriodPolicy);
        }
    }
}
