using PeopleManagement.Domain.AggregatesModel.DocumentTemplateAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTemplateTests.Mothers
{
    public static class DocumentTemplateMother
    {
        public static TemplateFileInfo ValidFileInfo()
            => TemplateFileInfo.Create("dir", "index.html", "header.html", "footer.html", [RecoverDataType.Employee]);

        public static PlaceSignature Place()
            => PlaceSignature.Create(1, 1, 10, 10, 20, 20);

        public static DocumentTemplate Simple(
            double? validityDays = 365,
            double? workloadHours = 8,
            bool includeFileInfo = true,
            bool acceptsSignature = true,
            List<PlaceSignature>? placeSignatures = null,
            bool usePreviousPeriod = false)
            => DocumentTemplate.Create(
                Guid.NewGuid(),
                "NR01",
                "Description NR01",
                Guid.NewGuid(),
                validityDays,
                workloadHours,
                includeFileInfo ? ValidFileInfo() : null,
                acceptsSignature,
                placeSignatures ?? [],
                Guid.NewGuid(),
                usePreviousPeriod);
    }
}
