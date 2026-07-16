using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests
{
    /// <summary>
    /// Caracterização do aggregate <see cref="DocumentTemplate"/> antes da refatoração para policies:
    /// conversão de durações, edição, invariante de assinatura e propriedades derivadas.
    /// </summary>
    public class DocumentTemplateAggregateTests
    {
        [Fact]
        public void Create_WithDaysAndHours_ConvertsToTimeSpan()
        {
            var template = DocumentTemplateMother.Simple(validityDays: 365, workloadHours: 8);

            Assert.Equal(TimeSpan.FromDays(365), template.DocumentValidityDuration);
            Assert.Equal(TimeSpan.FromHours(8), template.Workload);
        }

        [Fact]
        public void Create_WithNullDurations_LeavesDurationsNull()
        {
            var template = DocumentTemplateMother.Simple(validityDays: null, workloadHours: null);

            Assert.Null(template.DocumentValidityDuration);
            Assert.Null(template.Workload);
        }

        [Fact]
        public void Edit_UpdatesAllFields()
        {
            var template = DocumentTemplateMother.Simple(usePreviousPeriod: false);

            template.Edit("NEW NAME", "New description", 10, 4,
                DocumentTemplateMother.ValidFileInfo(), true, [], Guid.NewGuid(), usePreviousPeriod: true);

            Assert.Equal("NEW NAME", template.Name.Value);
            Assert.Equal(TimeSpan.FromDays(10), template.DocumentValidityDuration);
            Assert.Equal(TimeSpan.FromHours(4), template.Workload);
            Assert.True(template.UsePreviousPeriod);
        }

        [Fact]
        public void Create_WhenPlaceSignaturesButNotAcceptsSignature_Throws()
        {
            Assert.Throws<DomainException>(() =>
                DocumentTemplateMother.Simple(acceptsSignature: false, placeSignatures: [DocumentTemplateMother.Place()]));
        }

        [Fact]
        public void Create_WhenPlaceSignaturesAndAcceptsSignature_Succeeds()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true, placeSignatures: [DocumentTemplateMother.Place()]);

            Assert.Single(template.PlaceSignatures);
        }

        [Fact]
        public void CanGenerateDocuments_WithValidFileInfo_IsTrue()
        {
            var template = DocumentTemplateMother.Simple(includeFileInfo: true);

            Assert.True(template.CanGenerateDocuments);
        }

        [Fact]
        public void CanGenerateDocuments_WithoutFileInfo_IsFalse()
        {
            var template = DocumentTemplateMother.Simple(includeFileInfo: false);

            Assert.False(template.CanGenerateDocuments);
        }

        [Fact]
        public void IsSignable_WhenAcceptsSignature_IsTrue()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: true);

            Assert.True(template.IsSignable);
        }

        [Fact]
        public void IsSignable_WhenNotAcceptsSignature_IsFalse()
        {
            var template = DocumentTemplateMother.Simple(acceptsSignature: false);

            Assert.False(template.IsSignable);
        }
    }
}
