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
    /// Também cobre o contador de renovações do documento — base da renovação limitada — e o vínculo entre a
    /// unidade substituta e a substituída, que é o que permite renovar ANTES do vencimento sem que o documento
    /// passe a cobrar duas vezes a mesma coisa.
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

        private static Guid AddNotApplicableUnit(Document doc)
        {
            var unitId = AddPendingUnit(doc);
            doc.MarkAsNotApplicableDocumentUnit(unitId);
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

        // --- Contador de renovações -------------------------------------------

        [Fact]
        public void RenewalCount_ShouldStartAtZero()
        {
            Assert.Equal(0, CreateDocument().RenewalCount);
        }

        // Vencer não é renovar. O contador mede cota de renovação consumida, e um documento que venceu e ficou
        // abandonado não consumiu renovação nenhuma — se vencer contasse, ele queimaria a cota sem nunca ter
        // sido renovado.
        [Fact]
        public void RenewalCount_ShouldNotCountExpiration()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);

            doc.ExpireDocumentUnit(unitId);

            Assert.Equal(0, doc.RenewalCount);
        }

        [Fact]
        public void RenewalCount_ShouldCountEachRegisteredRenewal()
        {
            var doc = CreateDocument();

            doc.RegisterRenewal();
            doc.RegisterRenewal();

            Assert.Equal(2, doc.RenewalCount);
        }

        // O contador é o que separa renovação de substituição: corrigir um documento por reenvio deprecia a
        // unidade antiga, e isso não pode consumir uma renovação.
        [Fact]
        public void RenewalCount_ShouldNotCountSupersession()
        {
            var doc = CreateDocument();
            AddDeliveredUnit(doc);

            AddDeliveredUnit(doc, "arquivo-corrigido");

            Assert.Equal(0, doc.RenewalCount);
        }

        // --- Renovação: o vínculo entre substituta e substituída ---------------

        [Theory]
        [InlineData(false)] // OK: renovação antecipada
        [InlineData(true)]  // Vencida: renovação atrasada
        public void NewReplacementUnit_FromInForceOrExpiredUnit_ShouldStampTheLink(bool expireFirst)
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            if (expireFirst)
                doc.ExpireDocumentUnit(replacedId);

            var replacement = doc.NewReplacementUnit(Guid.NewGuid(), replacedId);

            Assert.True(replacement.IsReplacement);
            Assert.Equal(replacedId, replacement.ReplacesDocumentUnitId);
        }

        [Fact]
        public void NewReplacementUnit_FromPendingUnit_ShouldThrow()
        {
            var doc = CreateDocument();
            var pendingId = AddPendingUnit(doc);

            var exception = Assert.Throws<DomainException>(() => doc.NewReplacementUnit(Guid.NewGuid(), pendingId));

            AssertHasErrorCode(exception, "PMD.DOC25");
        }

        // A substituta em voo não piora o status: o documento continua contando com a cobertura que ainda vale.
        // Sem essa regra, pedir a renovação de um documento A Vencer o rebaixava para "Falta Entregar" — pedir a
        // renovação no prazo não pode deixar o documento pior do que ignorá-la.
        [Fact]
        public void DocumentStatus_WithRenewalInFlight_ShouldKeepReportingTheCoverageItStillHas()
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            doc.MakeAsWarning(replacedId);

            doc.NewReplacementUnit(Guid.NewGuid(), replacedId);

            Assert.Equal(DocumentStatus.Warning, doc.Status);
        }

        // Quando a substituída vence ela deixa de cobrir, a substituta volta a contar, e o documento passa a
        // cobrar — que é o que "Vencido" comunica.
        [Fact]
        public void DocumentStatus_WhenTheReplacedUnitExpires_ShouldReportExpired()
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            doc.NewReplacementUnit(Guid.NewGuid(), replacedId);

            doc.ExpireDocumentUnit(replacedId);

            Assert.Equal(DocumentStatus.Expired, doc.Status);
        }

        // Entregar a substituta ANTES do vencimento é o caminho bom, e é o que nem a competência nem a regra da
        // vencida alcançariam: a substituída ainda está OK. Sem o vínculo ela ficava viva até vencer sozinha, e
        // o documento cobrava duas vezes a mesma coisa.
        [Fact]
        public void WhenTheReplacementIsDelivered_TheReplacedUnitShouldBecomeHistory()
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            var replacementId = doc.NewReplacementUnit(Guid.NewGuid(), replacedId).Id;

            doc.UpdateDocumentUnitDetails(replacementId, OfficialDate, TimeSpan.Zero, "");
            doc.InsertUnitWithoutRequireValidation(replacementId, "arquivo-renovado", "pdf");

            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(replacedId).Status);
            Assert.Equal(DocumentUnitStatus.OK, doc.GetDocumentUnit(replacementId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        [Fact]
        public void LiveReplacementFor_WhenTheRenewalWasNotAsked_ShouldReturnNull()
        {
            var doc = CreateDocument();
            var unitId = AddDeliveredUnit(doc);

            Assert.Null(doc.LiveReplacementFor(unitId));
        }

        [Fact]
        public void LiveReplacementFor_WhenTheRenewalWasAsked_ShouldReturnIt()
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            var replacementId = doc.NewReplacementUnit(Guid.NewGuid(), replacedId).Id;

            Assert.Equal(replacementId, doc.LiveReplacementFor(replacedId)?.Id);
        }

        // Uma substituta descartada não trava a renovação: o RH pode pedir de novo depois de um engano.
        [Fact]
        public void LiveReplacementFor_WhenTheReplacementWasDiscarded_ShouldReturnNull()
        {
            var doc = CreateDocument();
            var replacedId = AddDeliveredUnit(doc);
            var replacementId = doc.NewReplacementUnit(Guid.NewGuid(), replacedId).Id;

            doc.MarkAsInvalidDocumentUnit(replacementId);

            Assert.Null(doc.LiveReplacementFor(replacedId));
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

        // Dispensar o documento é decisão administrativa, não prova de cobertura: quando ele volta a ser exigido,
        // invalidar a dispensa não apaga período nenhum e devolve a exigência para o RH.
        [Fact]
        public void MarkAsInvalidDocumentUnit_FromNotApplicable_ShouldMakeUnitInvalidAndRequireDocumentAgain()
        {
            var doc = CreateDocument();
            var unitId = AddNotApplicableUnit(doc);

            doc.MarkAsInvalidDocumentUnit(unitId);

            Assert.Equal(DocumentUnitStatus.Invalid, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
        }

        // Regressão: MarkAsInvalid aceita NotApplicable, mas a supersessão NÃO — só a decisão explícita do RH
        // desfaz a dispensa. Uma entrega qualquer não pode revogá-la de carona.
        [Fact]
        public void WhenAnotherUnitIsDelivered_NotApplicableUnitShouldSurvive()
        {
            var doc = CreateDocument();
            var notApplicableUnitId = AddNotApplicableUnit(doc);

            AddDeliveredUnit(doc);

            Assert.Equal(DocumentUnitStatus.NotApplicable, doc.GetDocumentUnit(notApplicableUnitId).Status);
        }

        [Fact]
        public void MakeAsDeprecated_ShouldNotDiscardNotApplicableUnit()
        {
            var doc = CreateDocument();
            var notApplicableUnitId = AddNotApplicableUnit(doc);

            doc.MakeAsDeprecated();

            Assert.Equal(DocumentUnitStatus.NotApplicable, doc.GetDocumentUnit(notApplicableUnitId).Status);
        }
    }
}
