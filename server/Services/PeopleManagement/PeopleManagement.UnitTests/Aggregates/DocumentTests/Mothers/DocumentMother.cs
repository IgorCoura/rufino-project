using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers
{
    public static class DocumentMother
    {
        public static Document Simple(bool usePreviousPeriod = false)
            => Document.Create(
                Guid.NewGuid(),
                employeeId: Guid.NewGuid(),
                companyId: Guid.NewGuid(),
                requiredDocumentId: Guid.NewGuid(),
                documentTemplateId: Guid.NewGuid(),
                name: "Documento Teste",
                description: "Descrição do documento",
                usePreviousPeriod: usePreviousPeriod);
    }
}
