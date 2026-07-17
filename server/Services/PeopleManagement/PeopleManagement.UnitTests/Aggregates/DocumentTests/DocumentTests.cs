using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    // A configuração de competência (periodType/usePreviousPeriod) é passada POR OPERAÇÃO, não guardada no
    // Document: espelha a leitura ao vivo do template — quem chama lê a PeriodPolicy na hora e informa.
    public class DocumentTests
    {
        private static Document CreateDocument()
        {
            return Document.Create(
                Guid.NewGuid(),
                employeeId: Guid.NewGuid(),
                companyId: Guid.NewGuid(),
                requiredDocumentId: Guid.NewGuid(),
                documentTemplateId: Guid.NewGuid(),
                name: "Documento Teste",
                description: "Descrição do documento"
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

            var result = document.NewDocumentUnit(unitId, PeriodType.Monthly, false, referenceDate);

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

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, false, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, false, referenceDate);

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

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, false, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, false, differentReferenceDate);

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

            var first = document.NewDocumentUnit(firstUnitId, periodType, false, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, periodType, false, referenceDate);

            Assert.Same(first, second);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnit_WithSamePeriod_UsePreviousPeriod_WhenPendingExists_ShouldReturnExisting()
        {
            var document = CreateDocument();
            var firstUnitId = Guid.NewGuid();
            var secondUnitId = Guid.NewGuid();
            var referenceDate = DateTime.UtcNow;

            var first = document.NewDocumentUnit(firstUnitId, PeriodType.Monthly, true, referenceDate);
            var second = document.NewDocumentUnit(secondUnitId, PeriodType.Monthly, true, referenceDate);

            Assert.Same(first, second);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithPeriod_ShouldInvalidateDuplicatePending()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, referenceDate);
            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, differentReferenceDate);

            Assert.Equal(2, document.DocumentsUnits.Count);

            // Atualiza o segundo para ter o mesmo period do primeiro
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(referenceDate), TimeSpan.Zero, "", PeriodType.Monthly, false);

            Assert.Equal(DocumentUnitStatus.Invalid, firstUnit.Status);
            Assert.Equal(DocumentUnitStatus.Pending, secondUnit.Status);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithPeriod_ShouldNotInvalidateNonPending()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, referenceDate);
            firstUnit.InsertWithoutRequireValidation("arquivo", "pdf");

            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, differentReferenceDate);

            // Atualiza o segundo para ter o mesmo period do primeiro (que já é OK)
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(referenceDate), TimeSpan.Zero, "", PeriodType.Monthly, false);

            Assert.NotEqual(DocumentUnitStatus.Invalid, firstUnit.Status);
        }

        [Fact]
        public void UpdateDocumentUnitDetails_WithDifferentPeriod_ShouldNotInvalidateAnything()
        {
            var document = CreateDocument();
            var referenceDate = DateTime.UtcNow;
            var differentReferenceDate = referenceDate.AddMonths(1);

            var firstUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, referenceDate);
            var secondUnit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, differentReferenceDate);

            // Atualiza o segundo mantendo um period diferente do primeiro
            document.UpdateDocumentUnitDetails(secondUnit.Id, DateOnly.FromDateTime(differentReferenceDate), TimeSpan.Zero, "", PeriodType.Monthly, false);

            Assert.Equal(DocumentUnitStatus.Pending, firstUnit.Status);
            Assert.Equal(DocumentUnitStatus.Pending, secondUnit.Status);
        }

        // -----------------------------------------------------------------
        // Leitura ao vivo: troca de granularidade e heal de unidade sem competência.
        // -----------------------------------------------------------------

        // A pendente na competência mínima é uma unidade "esperando data": trocar a granularidade do template
        // não pode deixá-la órfã nem criar uma segunda pendente — ela é reaproveitada e re-situada na mínima
        // do tipo novo.
        [Fact]
        public void NewDocumentUnit_GranularityChanged_ShouldReuseAndResituateThePendingAtMinimumPeriod()
        {
            var document = CreateDocument();
            var monthlyPending = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false);
            Assert.True(monthlyPending.Period!.IsMonthly);
            Assert.Equal(Period.MIN_YEAR, monthlyPending.Period.Year);

            var reused = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Yearly, false);

            Assert.Same(monthlyPending, reused);
            Assert.Single(document.DocumentsUnits);
            Assert.True(reused.Period!.IsYearly);
            Assert.Equal(Period.MIN_YEAR, reused.Period.Year);
        }

        [Fact]
        public void NewDocumentUnit_SameGranularityAtMinimum_ShouldReuseWithoutChangingThePeriod()
        {
            var document = CreateDocument();
            var pending = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false);

            var reused = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false);

            Assert.Same(pending, reused);
            Assert.Single(document.DocumentsUnits);
            Assert.True(reused.Period!.IsMonthly);
            Assert.Equal(Period.MIN_YEAR, reused.Period.Year);
        }

        // Heal de legado: unidade nascida SEM competência (template ainda não tinha a PeriodPolicy) é situada
        // quando um update chega com a configuração atual do template.
        [Fact]
        public void UpdateDocumentUnitDetails_UnitWithoutPeriod_WhenConfigInformed_ShouldSituateTheUnit()
        {
            var document = CreateDocument();
            var unit = document.NewDocumentUnit(Guid.NewGuid());
            Assert.Null(unit.Period);
            var date = new DateOnly(2024, 3, 15);

            document.UpdateDocumentUnitDetails(unit.Id, date, TimeSpan.Zero, "", PeriodType.Monthly, false);

            Assert.NotNull(unit.Period);
            Assert.True(unit.Period!.IsMonthly);
            Assert.Equal(2024, unit.Period.Year);
            Assert.Equal(3, unit.Period.Month);
        }

        // O inverso do heal: sem configuração (template sem a regra), a competência existente é história e o
        // update não a recalcula nem a apaga.
        [Fact]
        public void UpdateDocumentUnitDetails_UnitWithPeriod_WhenConfigAbsent_ShouldLeaveThePeriodUntouched()
        {
            var document = CreateDocument();
            var referenceDate = new DateTime(2024, 3, 15, 0, 0, 0, DateTimeKind.Utc);
            var unit = document.NewDocumentUnit(Guid.NewGuid(), PeriodType.Monthly, false, referenceDate);

            document.UpdateDocumentUnitDetails(unit.Id, new DateOnly(2024, 4, 10), TimeSpan.Zero, "");

            Assert.True(unit.Period!.IsMonthly);
            Assert.Equal(2024, unit.Period.Year);
            Assert.Equal(3, unit.Period.Month);
        }
    }
}
