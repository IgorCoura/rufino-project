using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Fase 2.4: o caller pode informar as policies explicitamente. Quando informa, elas mandam e os campos
    /// escalares legados viram projeção delas; quando não informa, vale o caminho legado (derivar dos campos).
    /// </summary>
    public class DocumentTemplateExplicitPoliciesTests
    {
        // acceptsSignature: false mantém o conjunto restrito às regras sob teste — a assinatura tem caminho
        // próprio (parâmetros, não o bloco de policies) e é coberta em DocumentTemplateSignaturePolicyTests.
        private static DocumentTemplate CreateWith(IEnumerable<IDocumentPolicy>? policies, double? validityDays = 365, double? workloadHours = 8)
            => DocumentTemplate.Create(
                Guid.NewGuid(), "NR01", "Description NR01", Guid.NewGuid(),
                validityDays, workloadHours, DocumentTemplateMother.ValidFileInfo(),
                acceptsSignature: false, placeSignatures: [], documentGroupId: Guid.NewGuid(),
                usePreviousPeriod: false, policies: policies);

        [Fact]
        public void Create_WithExplicitPolicies_PoliciesWinOverScalarFields()
        {
            var template = CreateWith([new ExpirationPolicy(TimeSpan.FromDays(30))], validityDays: 365, workloadHours: 8);

            Assert.Equal(TimeSpan.FromDays(30), template.GetPolicy<IExpirationPolicy>()!.Duration);
        }

        [Fact]
        public void Create_WithExplicitPolicies_MirrorsScalarFieldsFromPolicies()
        {
            var template = CreateWith([new ExpirationPolicy(TimeSpan.FromDays(30))], validityDays: 365, workloadHours: 8);

            Assert.Equal(TimeSpan.FromDays(30), template.DocumentValidityDuration);
            Assert.Null(template.Workload);
        }

        [Fact]
        public void Create_WithEmptyPolicySet_ClearsRulesAndScalarFields()
        {
            var template = CreateWith([], validityDays: 365, workloadHours: 8);

            Assert.Empty(template.Policies);
            Assert.Null(template.DocumentValidityDuration);
            Assert.Null(template.Workload);
        }

        [Fact]
        public void Create_WithoutPolicies_KeepsLegacyDerivationFromScalarFields()
        {
            var template = CreateWith(policies: null, validityDays: 365, workloadHours: 8);

            Assert.Equal(TimeSpan.FromDays(365), template.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(8), template.GetPolicy<IWorkloadPolicy>()!.Workload);
            Assert.Equal(TimeSpan.FromDays(365), template.DocumentValidityDuration);
        }

        [Fact]
        public void Create_WithBothPolicies_ComposesBoth()
        {
            var template = CreateWith([new ExpirationPolicy(TimeSpan.FromDays(30)), new WorkloadPolicy(TimeSpan.FromHours(4))]);

            Assert.Equal(2, template.Policies.Count);
            Assert.Equal(TimeSpan.FromDays(30), template.DocumentValidityDuration);
            Assert.Equal(TimeSpan.FromHours(4), template.Workload);
        }

        [Fact]
        public void Edit_WithExplicitPolicies_ReplacesRulesAndMirrorsScalarFields()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            template.Edit("NR01", "Description NR01", 365, 8, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid(),
                usePreviousPeriod: false, policies: [new WorkloadPolicy(TimeSpan.FromHours(4))]);

            Assert.False(template.HasPolicy<IExpirationPolicy>());
            Assert.Null(template.DocumentValidityDuration);
            Assert.Equal(TimeSpan.FromHours(4), template.Workload);
        }

        [Fact]
        public void Edit_WithoutPolicies_KeepsLegacyDerivationFromScalarFields()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            template.Edit("NR01", "Description NR01", 30, 4, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid());

            Assert.Equal(TimeSpan.FromDays(30), template.GetPolicy<IExpirationPolicy>()!.Duration);
            Assert.Equal(TimeSpan.FromHours(4), template.GetPolicy<IWorkloadPolicy>()!.Workload);
        }

        [Fact]
        public void Edit_WithEmptyPolicySet_ClearsRulesEvenWhenScalarFieldsInformed()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            template.Edit("NR01", "Description NR01", 365, 8, DocumentTemplateMother.ValidFileInfo(), false, [], Guid.NewGuid(),
                usePreviousPeriod: false, policies: []);

            Assert.Empty(template.Policies);
            Assert.Null(template.DocumentValidityDuration);
            Assert.Null(template.Workload);
        }
    }
}
