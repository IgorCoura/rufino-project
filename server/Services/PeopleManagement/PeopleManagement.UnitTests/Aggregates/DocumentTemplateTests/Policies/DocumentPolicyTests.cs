using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Modelo de policies (Fase 2): comportamento das policies e round-trip pela forma persistida
    /// (DocumentPolicyFactory) — serialização em jsonb (Opção 1) preserva os parâmetros.
    /// </summary>
    public class DocumentPolicyTests
    {
        [Fact]
        public void ExpirationPolicy_CanRenew_IsAlwaysTrue()
        {
            var policy = new ExpirationPolicy(TimeSpan.FromDays(365));

            Assert.True(policy.CanRenew(0));
            Assert.True(policy.CanRenew(100));
        }

        [Fact]
        public void ToPersistence_ExpirationPolicy_UsesExpirationDiscriminator()
        {
            var record = DocumentPolicyFactory.ToPersistence(new ExpirationPolicy(TimeSpan.FromDays(30)));

            Assert.Equal(PolicyType.Expiration, record.Type);
        }

        [Fact]
        public void RoundTrip_ExpirationPolicy_PreservesDuration()
        {
            var record = DocumentPolicyFactory.ToPersistence(new ExpirationPolicy(TimeSpan.FromDays(365)));

            var restored = Assert.IsType<ExpirationPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(TimeSpan.FromDays(365), restored.Duration);
        }

        [Fact]
        public void RoundTrip_PeriodPolicy_PreservesPeriodTypeAndUsePreviousPeriod()
        {
            var record = DocumentPolicyFactory.ToPersistence(new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: true));

            var restored = Assert.IsType<PeriodPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(PeriodType.Monthly, restored.PeriodType);
            Assert.True(restored.UsePreviousPeriod);
        }

        [Fact]
        public void RoundTrip_WorkloadPolicy_PreservesWorkload()
        {
            var record = DocumentPolicyFactory.ToPersistence(new WorkloadPolicy(TimeSpan.FromHours(8)));

            var restored = Assert.IsType<WorkloadPolicy>(DocumentPolicyFactory.ToPolicy(record));
            Assert.Equal(TimeSpan.FromHours(8), restored.Workload);
        }

        [Fact]
        public void ToPolicy_WithUnhandledDiscriminator_Throws()
        {
            var record = new DocumentPolicy(PolicyType.Generation, "{}");

            Assert.Throws<DomainException>(() => DocumentPolicyFactory.ToPolicy(record));
        }

        [Fact]
        public void ToPersistence_WithUnsupportedPolicy_Throws()
        {
            Assert.Throws<DomainException>(() => DocumentPolicyFactory.ToPersistence(new UnsupportedPolicy()));
        }

        // Policy que não implementa nenhuma capacidade conhecida — exercita o arm de fallback da factory.
        private sealed class UnsupportedPolicy : IDocumentPolicy
        {
        }
    }
}
