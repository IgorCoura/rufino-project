using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Cobre a criação manual da unidade de UMA competência — o RH preenchendo à mão o período que ficou sem
    /// unidade num documento por competência.
    ///
    /// A diferença para <c>NewDocumentUnit</c> é a recusa: lá a pendente equivalente é reaproveitada em silêncio,
    /// aqui competência ocupada é erro (PMD.DOC27), porque quem pede a criação manual quer uma unidade A MAIS.
    /// Só inválida e depreciada desocupam a competência — as duas são justamente as que já saíram de cena.
    /// </summary>
    public class DocumentPeriodCreationTests
    {
        private const string PeriodOccupiedErrorCode = "PMD.DOC27";

        // Data no passado: MarkAsAwaitingSignature recusa unidade com data futura.
        private static readonly DateTime March2024 = new(2024, 3, 10);
        private static readonly DateTime April2024 = new(2024, 4, 10);

        private static Guid CreateMonthlyUnit(Document document, DateTime referenceDate, bool usePreviousPeriod = false)
        {
            var unitId = Guid.NewGuid();
            document.NewDocumentUnitForPeriod(unitId, PeriodType.Monthly, usePreviousPeriod, referenceDate);
            return unitId;
        }

        private static void AssertHasErrorCode(DomainException exception, string code)
        {
            var codes = exception.Errors.Values
                .SelectMany(errors => errors)
                .OfType<Error>()
                .Select(error => error.Code);

            Assert.Contains(code, codes);
        }

        // --- Competência livre ------------------------------------------------

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodIsFree_ShouldCreatePendingUnitOnThatPeriod()
        {
            var document = DocumentMother.Simple();

            var unitId = CreateMonthlyUnit(document, March2024);

            var unit = document.GetDocumentUnit(unitId);
            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(Period.CreateMonthly(2024, 3), unit.Period);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodIsFree_ShouldUseTheInformedDateAsTheUnitDate()
        {
            var document = DocumentMother.Simple();

            var unitId = CreateMonthlyUnit(document, March2024);

            Assert.Equal(DateOnly.FromDateTime(March2024), document.GetDocumentUnit(unitId).Date);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenTemplateUsesPreviousPeriod_ShouldLandOnThePreviousCompetency()
        {
            var document = DocumentMother.Simple();

            var unitId = CreateMonthlyUnit(document, March2024, usePreviousPeriod: true);

            Assert.Equal(Period.CreateMonthly(2024, 2), document.GetDocumentUnit(unitId).Period);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenAnotherPeriodIsOccupied_ShouldCreateTheUnitAnyway()
        {
            var document = DocumentMother.Simple();
            CreateMonthlyUnit(document, March2024);

            var aprilUnitId = CreateMonthlyUnit(document, April2024);

            Assert.Equal(Period.CreateMonthly(2024, 4), document.GetDocumentUnit(aprilUnitId).Period);
            Assert.Equal(2, document.DocumentsUnits.Count);
        }

        // --- Competência ocupada ----------------------------------------------

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasAPendingUnit_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            CreateMonthlyUnit(document, March2024);

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
            Assert.Single(document.DocumentsUnits);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasADeliveredUnit_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasAnExpiringUnit_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");
            document.MakeAsWarning(unitId);

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        // Vencida ocupa: houve cobertura no período e ela ainda espera substituto. A saída ali é renovar, não
        // criar outra unidade solta na mesma competência.
        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasAnExpiredUnit_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");
            document.ExpireDocumentUnit(unitId);

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasAUnitAwaitingValidation_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.InsertUnitWithRequireValidation(unitId, "arquivo", "pdf");

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasAUnitAwaitingSignature_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.MarkAsAwaitingDocumentUnitSignature(unitId);

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        // Não aplicável cobre a exigência tanto quanto uma entrega — é dispensa deliberada, não falta.
        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodHasANotApplicableUnit_ShouldThrow()
        {
            var document = DocumentMother.Simple();
            var unitId = CreateMonthlyUnit(document, March2024);
            document.MarkAsNotApplicableDocumentUnit(unitId);

            var exception = Assert.Throws<DomainException>(() => CreateMonthlyUnit(document, March2024));

            AssertHasErrorCode(exception, PeriodOccupiedErrorCode);
        }

        // --- Competência desocupada -------------------------------------------

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodOnlyHasAnInvalidUnit_ShouldCreateTheUnit()
        {
            var document = DocumentMother.Simple();
            var invalidatedUnitId = CreateMonthlyUnit(document, March2024);
            document.MarkAsInvalidDocumentUnit(invalidatedUnitId);

            var unitId = CreateMonthlyUnit(document, March2024);

            Assert.NotEqual(invalidatedUnitId, unitId);
            Assert.Equal(DocumentUnitStatus.Pending, document.GetDocumentUnit(unitId).Status);
        }

        [Fact]
        public void NewDocumentUnitForPeriod_WhenPeriodOnlyHasADeprecatedUnit_ShouldCreateTheUnit()
        {
            var document = DocumentMother.Simple();
            var deprecatedUnitId = CreateMonthlyUnit(document, March2024);
            document.InsertUnitWithoutRequireValidation(deprecatedUnitId, "arquivo", "pdf");
            document.DeprecateDocumentUnit(deprecatedUnitId);

            var unitId = CreateMonthlyUnit(document, March2024);

            Assert.NotEqual(deprecatedUnitId, unitId);
            Assert.Equal(Period.CreateMonthly(2024, 3), document.GetDocumentUnit(unitId).Period);
        }

        // A pendente que espera data vive na competência mínima, que não é competência nenhuma — não ocupa
        // março, e por isso a criação manual passa por cima dela em vez de ser recusada.
        [Fact]
        public void NewDocumentUnitForPeriod_WhenOnlyAMinimumPeriodPendingExists_ShouldCreateTheUnit()
        {
            var document = DocumentMother.Simple();
            var waitingForDateUnitId = Guid.NewGuid();
            document.NewDocumentUnit(waitingForDateUnitId, PeriodType.Monthly);

            var unitId = CreateMonthlyUnit(document, March2024);

            Assert.NotEqual(waitingForDateUnitId, unitId);
            Assert.Equal(Period.CreateMonthly(2024, 3), document.GetDocumentUnit(unitId).Period);
        }
    }
}
