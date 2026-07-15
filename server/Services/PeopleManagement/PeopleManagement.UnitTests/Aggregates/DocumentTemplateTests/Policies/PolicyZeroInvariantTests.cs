using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate.Policies;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Policies
{
    /// <summary>
    /// Invariante das policies: presença = regra ativa, logo uma regra com valor zerado não pode existir —
    /// seria ausência disfarçada de presença. Cobre os três caminhos que podem criar uma policy:
    /// construção direta, derivação dos escalares legados e conjunto explícito vindo da API.
    /// </summary>
    public class PolicyZeroInvariantTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ExpirationPolicy_WithNonPositiveDuration_Throws(int days)
        {
            Assert.Throws<DomainException>(() => new ExpirationPolicy(TimeSpan.FromDays(days)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void WorkloadPolicy_WithNonPositiveWorkload_Throws(int hours)
        {
            Assert.Throws<DomainException>(() => new WorkloadPolicy(TimeSpan.FromHours(hours)));
        }

        [Fact]
        public void ExpirationPolicy_WithPositiveDuration_IsCreated()
        {
            var policy = new ExpirationPolicy(TimeSpan.FromDays(1));

            Assert.Equal(TimeSpan.FromDays(1), policy.Duration);
        }

        // Derivação: escalar zerado é ausência de regra. Precisa PULAR, não lançar — templates legados
        // gravaram 00:00:00 na coluna e não podem quebrar ao serem editados.
        [Fact]
        public void Create_WithZeroScalarFields_DerivesNoPolicies()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 0, workloadHours: 0);

            Assert.Empty(template.Policies);
        }

        [Fact]
        public void Edit_WithZeroScalarFields_RemovesPreviouslyDerivedPolicies()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            template.Edit("NR01", "Description NR01", 0, 0, DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid());

            Assert.Empty(template.Policies);
        }

        // Caminho explícito (o que a API usa): pedir uma regra com zero é erro do caller, não silêncio.
        [Fact]
        public void Create_WithExplicitZeroExpirationPolicy_Throws()
        {
            Assert.Throws<DomainException>(() => DocumentTemplate.Create(
                Guid.NewGuid(), "NR01", "Description NR01", Guid.NewGuid(), (double?)null, null,
                DocumentTemplateMother.ValidFileInfo(), acceptsSignature: true, placeSignatures: [],
                documentGroupId: Guid.NewGuid(), usePreviousPeriod: false,
                policies: [new ExpirationPolicy(TimeSpan.Zero)]));
        }

        [Fact]
        public void Create_WithExplicitZeroWorkloadPolicy_Throws()
        {
            Assert.Throws<DomainException>(() => DocumentTemplate.Create(
                Guid.NewGuid(), "NR01", "Description NR01", Guid.NewGuid(), (double?)null, null,
                DocumentTemplateMother.ValidFileInfo(), acceptsSignature: true, placeSignatures: [],
                documentGroupId: Guid.NewGuid(), usePreviousPeriod: false,
                policies: [new WorkloadPolicy(TimeSpan.Zero)]));
        }

        // Round-trip pela forma persistida: o jsonb nunca pode reidratar uma policy zerada.
        [Fact]
        public void ToPolicy_WithZeroDurationPayload_Throws()
        {
            var record = new DocumentPolicy(PolicyType.Expiration, """{"DurationTicks":0}""");

            Assert.Throws<DomainException>(() => DocumentPolicyFactory.ToPolicy(record));
        }
    }
}
