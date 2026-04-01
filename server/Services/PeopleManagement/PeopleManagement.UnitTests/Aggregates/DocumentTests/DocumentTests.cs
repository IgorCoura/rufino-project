using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    public class DocumentTests
    {
        private static Document CreateDocument(bool usePreviousPeriod = false)
        {
            return Document.Create(
                Guid.NewGuid(),
                employeeId: Guid.NewGuid(),
                companyId: Guid.NewGuid(),
                requiredDocumentId: Guid.NewGuid(),
                documentTemplateId: Guid.NewGuid(),
                name: "Documento Teste",
                description: "Descrição do documento",
                usePreviousPeriod: usePreviousPeriod
            );
        }

        [Fact]
        public void NewDocumentUnit_WithoutPeriod_ShouldCreateNewUnit()
        {
            var document = CreateDocument();
            var unitId = Guid.NewGuid();

            var result = document.NewDocumentUnit(unitId);

            Assert.NotNull(result);
            Assert.Equal(unitId, result.Id);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithoutPeriod_WhenPendingExists_ShouldReturnExisting()
        {
            var document = CreateDocument();
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();

            var first = document.NewDocumentUnit(firstUnitId);
            var second = document.NewDocumentUnit(secondUnitId);

            Assert.Same(first, second);
            Assert.Equal(firstUnitId, second.Id);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithPeriod_ShouldCreateNewUnit()
        {
            var document = CreateDocument();
            var unitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;

            var result = document.NewDocumentUnit(unitId, PeriodType.Monthly, referenceDate);

            Assert.NotNull(result);
            Assert.Equal(unitId, result.Id);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithSamePeriod_WhenPendingExists_ShouldReturnExisting()
        {
            var document = CreateDocument();
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, referenceDate);

            Assert.Same(first, second);
            Assert.Equal(firstUnitId, second.Id);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithDifferentPeriod_ShouldCreateNewUnit()
        {
            var document = CreateDocument();
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, differentReferenceDate);

            Assert.NotSame(first, second);
            Assert.Equal(2, document.DocumentsUnits.Count);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        public void NewDocumentUnit_WithSamePeriodType_WhenPendingExists_ShouldReturnExisting(int periodTypeId)
        {
            PeriodType periodType = periodTypeId;
            var document = CreateDocument();
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;

            var first = document.NewDocumentUnit(firstUnitId, periodType, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, periodType, referenceDate);

            Assert.Same(first, second);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithSamePeriod_UsePreviousPeriod_WhenPendingExists_ShouldReturnExisting()
        {
            var document = CreateDocument(usePreviousPeriod: true);
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, referenceDate);

            Assert.Same(first, second);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithPeriod_ShouldInvalidateDuplicatePending()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, referenceDate);
            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, differentReferenceDate);

            Assert.Equal(2, document.DocumentsUnits.Count);

            // Atualiza o segundo para ter o mesmo period do primeiro
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(referenceDate), TimeSpan.Zero, "", PeriodType.Monthly);

            Assert.Equal(DocumentUnitStatus.Invalid, firstUnit.Status);
            Assert.Equal(DocumentUnitStatus.Pending, secondUnit.Status);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithPeriod_ShouldNotInvalidateNonPending()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, referenceDate);
            firstUnit.InsertWithoutRequireValidation("arquivo", "pdf");

            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, differentReferenceDate);

            // Atualiza o segundo para ter o mesmo period do primeiro (que já é OK)
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(referenceDate), TimeSpan.Zero, "", PeriodType.Monthly);

            Assert.NotEqual(DocumentUnitStatus.Invalid, firstUnit.Status);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithDifferentPeriod_ShouldNotInvalidateAnything()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, referenceDate);
            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, differentReferenceDate);

            // Atualiza o segundo mantendo um period diferente do primeiro
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(differentReferenceDate), TimeSpan.Zero, "", PeriodType.Monthly);

            Assert.Equal(DocumentUnitStatus.Pending, firstUnit.Status);
            Assert.Equal(DocumentUnitStatus.Pending, secondUnit.Status);
        }
    }
}
