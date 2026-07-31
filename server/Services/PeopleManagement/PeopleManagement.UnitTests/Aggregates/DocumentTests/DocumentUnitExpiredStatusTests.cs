using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.ErrorTools;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Cobre a separação entre VENCIDO e DEPRECIADO, que antes eram o mesmo status.
    ///
    /// Vencido = saiu de vigência e ainda não há substituto (risco de conformidade).
    /// Depreciado = saiu de vigência E já tem substituto (histórico, vale como prova).
    ///
    /// Também cobre o contador de vencimentos do documento, que substituiu a contagem de unidades depreciadas
    /// como base da renovação limitada.
    /// </summary>
    public class DocumentUnitExpiredStatusTests
    {
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);

        private static Document CreateDocument()
            => Document.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "Documento Teste", "Descrição do documento");

        private static Guid AddDeliveredUnit(Document doc, string fileName = "arquivo")
        {
            var unitId = Guid.NewGuid();
            doc.NewDocumentUnit(unitId);
            doc.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "");
            doc.InsertUnitWithoutRequireValidation(unitId, fileName, "pdf");
            return unitId;
        }

        private static Guid AddPendingUnit(Document doc)
        {
            var unitId = Guid.NewGuid();
            doc.NewDocumentUnit(unitId);
            doc.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "");
            return unitId;
        }

        // Os erros ficam num dicionário por origem, com os Error boxed em object.
        private static void AssertHasErrorCode(DomainException exception, string code)
        {
            var codes = exception.Errors.Values
                .SelectMany(errors => errors)
                .OfType<Error>()
                .Select(error => error.Code);

            Assert.Contains(code, codes);
        }

        // --- Vencimento -------------------------------------------------------

        [Fact]
        public void ExpireDocumentUnit_FromOk_ShouldMakeUnitExpiredAndDocumentExpired()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);

            var expired = doc.ExpireDocumentUnit(unitId);

            Assert.True(expired);
            Assert.Equal(DocumentUnitStatus.Expired, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.Expired, doc.Status);
        }

        [Fact]
        public void ExpireDocumentUnit_FromWarning_ShouldMakeUnitExpired()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);
            doc.MakeAsWarning(unitId);

            Assert.True(doc.ExpireDocumentUnit(unitId));
            Assert.Equal(DocumentUnitStatus.Expired, doc.GetDocumentUnit(unitId).Status);
        }

        // Vencer é o fim de uma vigência: uma pendente nunca chegou a valer, então não tem o que vencer.
        [Fact]
        public void ExpireDocumentUnit_FromPending_ShouldDoNothing()
        {
            var doc = CreateDocument();
            var unitId = AddPendingUnit(doc);

            Assert.False(doc.ExpireDocumentUnit(unitId));
            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(unitId).Status);
        }

        // --- Vencido -> Depreciado quando o substituto chega ------------------

        [Fact]
        public void WhenReplacementIsDelivered_ExpiredUnitShouldBecomeDeprecated()
        {
            var doc = CreateDocument();
            var expiredUnitId = AddDeliveredUnit(doc);
            doc.ExpireDocumentUnit(expiredUnitId);

            AddDeliveredUnit(doc, "arquivo2");

            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(expiredUnitId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        // A pendente da renovação, sozinha, não resolve nada — só a entrega resolve.
        [Fact]
        public void WhileReplacementIsStillPending_UnitShouldStayExpired()
        {
            var doc = CreateDocument();
            var expiredUnitId = AddDeliveredUnit(doc);
            doc.ExpireDocumentUnit(expiredUnitId);

            AddPendingUnit(doc);

            Assert.Equal(DocumentUnitStatus.Expired, doc.GetDocumentUnit(expiredUnitId).Status);
            Assert.Equal(DocumentStatus.Expired, doc.Status);
        }

        [Fact]
        public void MarkAsNotApplicable_ShouldAlsoSupersedeAnExpiredUnit()
        {
            var doc = CreateDocument();
            var expiredUnitId = AddDeliveredUnit(doc);
            doc.ExpireDocumentUnit(expiredUnitId);
            var pendingUnitId = AddPendingUnit(doc);

            doc.MarkAsNotApplicableDocumentUnit(pendingUnitId);

            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(expiredUnitId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        // --- Contador de vencimentos ------------------------------------------

        [Fact]
        public void ExpirationCount_ShouldStartAtZero()
        {
            Assert.Equal(0, CreateDocument().ExpirationCount);
        }

        [Fact]
        public void ExpirationCount_ShouldIncrementOnlyWhenTheUnitActuallyExpires()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);

            doc.ExpireDocumentUnit(unitId);
            doc.ExpireDocumentUnit(unitId); // já vencida: não conta de novo

            Assert.Equal(1, doc.ExpirationCount);
        }

        // O contador é o que separa vencimento de substituição: corrigir um documento por reenvio deprecia a
        // unidade antiga, e isso não pode consumir uma renovação.
        [Fact]
        public void ExpirationCount_ShouldNotCountSupersession()
        {
            var doc = CreateDocument();
            AddDeliveredUnit(doc);

            AddDeliveredUnit(doc, "arquivo-corrigido");

            Assert.Equal(0, doc.ExpirationCount);
        }

        // --- Guardas de invalidação e depreciação manual -----------------------

        [Fact]
        public void DeprecateDocumentUnit_FromOk_ShouldMakeUnitDeprecated()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);

            doc.DeprecateDocumentUnit(unitId);

            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(unitId).Status);
        }

        [Fact]
        public void DeprecateDocumentUnit_FromPending_ShouldThrow()
        {
            var doc = CreateDocument();
            var unitId = AddPendingUnit(doc);

            var exception = Assert.Throws<DomainException>(() => doc.DeprecateDocumentUnit(unitId));

            AssertHasErrorCode(exception, "PMD.DOC23");
        }

        [Theory]
        [InlineData(true)]  // pendente
        [InlineData(false)] // entregue
        public void MarkAsInvalidDocumentUnit_FromPendingOrOk_ShouldMakeUnitInvalid(bool pending)
        {
            var doc = CreateDocument();
            var unitId = pending ? AddPendingUnit(doc) : AddDeliveredUnit(doc);

            doc.MarkAsInvalidDocumentUnit(unitId);

            Assert.Equal(DocumentUnitStatus.Invalid, doc.GetDocumentUnit(unitId).Status);
        }

        // Depreciada e vencida são a prova de que houve documento válido no período — invalidar apagaria isso.
        [Fact]
        public void MarkAsInvalidDocumentUnit_FromExpired_ShouldThrow()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);
            doc.ExpireDocumentUnit(unitId);

            var exception = Assert.Throws<DomainException>(() => doc.MarkAsInvalidDocumentUnit(unitId));

            AssertHasErrorCode(exception, "PMD.DOC24");
            Assert.Equal(DocumentUnitStatus.Expired, doc.GetDocumentUnit(unitId).Status);
        }

        [Fact]
        public void MarkAsInvalidDocumentUnit_FromDeprecated_ShouldThrow()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);
            doc.DeprecateDocumentUnit(unitId);

            var exception = Assert.Throws<DomainException>(() => doc.MarkAsInvalidDocumentUnit(unitId));

            AssertHasErrorCode(exception, "PMD.DOC24");
        }
    }
}
