using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Fase 2.2: o DocumentTemplate compõe um conjunto de policies (Composite). Presença = regra ativa.
    /// Nesta fase as policies são derivadas dos campos escalares legados, que seguem sendo a fonte da verdade.
    /// </summary>
    public class DocumentTemplatePoliciesTests
    {
        [Fact]
        public void Create_WithValidityDuration_DerivesExpirationPolicy()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365);

            var policy = template.GetPolicy<IExpirationPolicy>();

            Assert.NotNull(policy);
            Assert.Equal(TimeSpan.FromDays(365), policy!.Duration);
        }

        [Fact]
        public void Create_WithoutValidityDuration_DoesNotDeriveExpirationPolicy()
        {
            var template = DocumentTemplateMother.Simple(validityDays: null);

            Assert.Null(template.GetPolicy<IExpirationPolicy>());
            Assert.False(template.HasPolicy<IExpirationPolicy>());
        }

        [Fact]
        public void Create_WithWorkload_DerivesWorkloadPolicy()
        {
            var template = DocumentTemplateMother.Simple(workloadHours: 8);

            var policy = template.GetPolicy<IWorkloadPolicy>();

            Assert.NotNull(policy);
            Assert.Equal(TimeSpan.FromHours(8), policy!.Workload);
        }

        [Fact]
        public void Create_WithoutWorkload_DoesNotDeriveWorkloadPolicy()
        {
            var template = DocumentTemplateMother.Simple(workloadHours: null);

            Assert.Null(template.GetPolicy<IWorkloadPolicy>());
        }

        // A competência ainda é decidida pelo fluxo do evento, não pelo template — derivar PeriodPolicy aqui
        // seria mudança de comportamento. Fica para a Fase 3.
        [Fact]
        public void Create_DoesNotDerivePeriodPolicy()
        {
            var template = DocumentTemplateMother.Simple(usePreviousPeriod: true);

            Assert.False(template.HasPolicy<IPeriodPolicy>());
        }

        [Fact]
        public void Create_WithoutValidityAndWorkload_HasNoPolicies()
        {
            var template = DocumentTemplateMother.Simple(validityDays: null, workloadHours: null);

            Assert.Empty(template.Policies);
        }

        [Fact]
        public void Edit_ChangingValidityDuration_RederivesExpirationPolicy()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365);

            template.Edit("NR01", "Description NR01", 30, 8, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid());

            Assert.Equal(TimeSpan.FromDays(30), template.GetPolicy<IExpirationPolicy>()!.Duration);
        }

        [Fact]
        public void Edit_RemovingValidityDuration_RemovesExpirationPolicy()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365);

            template.Edit("NR01", "Description NR01", null, 8, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid());

            Assert.False(template.HasPolicy<IExpirationPolicy>());
            Assert.True(template.HasPolicy<IWorkloadPolicy>());
        }

        [Fact]
        public void AddPolicy_WithSameType_ReplacesPreviousPolicy()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365);

            template.AddPolicy(new ExpirationPolicy(TimeSpan.FromDays(90)));

            Assert.Single(template.Policies, x => x.Type.Equals(PolicyType.Expiration));
            Assert.Equal(TimeSpan.FromDays(90), template.GetPolicy<IExpirationPolicy>()!.Duration);
        }

        [Fact]
        public void AddPolicy_WithNewType_ComposesAlongsideExisting()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: null);

            template.AddPolicy(new PeriodPolicy(PeriodType.Monthly, usePreviousPeriod: true));

            Assert.Equal(2, template.Policies.Count);
            Assert.True(template.HasPolicy<IExpirationPolicy>());
            Assert.Equal(PeriodType.Monthly, template.GetPolicy<IPeriodPolicy>()!.PeriodType);
        }

        [Fact]
        public void RemovePolicy_DropsOnlyTheRequestedType()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            template.RemovePolicy(PolicyType.Expiration);

            Assert.False(template.HasPolicy<IExpirationPolicy>());
            Assert.True(template.HasPolicy<IWorkloadPolicy>());
        }

        [Fact]
        public void Policies_IsExposedAsReadOnly()
        {
            var template = DocumentTemplateMother.Simple();

            Assert.IsNotType<List<DocumentPolicy>>(template.Policies);
        }
    }
}
