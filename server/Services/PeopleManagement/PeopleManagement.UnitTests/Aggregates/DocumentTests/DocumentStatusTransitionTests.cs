using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// Cobre as transições de estado do <see cref="DocumentUnitStatus"/> e como elas
    /// se refletem no <see cref="DocumentStatus"/> derivado do <see cref="Document"/>.
    ///
    /// DocumentUnitStatus (9): Pending, OK, Deprecated, Invalid, RequiresValidation,
    ///                         NotApplicable, AwaitingSignature, Warning, Expired.
    /// DocumentStatus (7):     RequiresDocument, RequiresValidation, OK, Deprecated,
    ///                         AwaitingSignature, Warning, Expired.
    /// </summary>
    public class DocumentStatusTransitionTests
    {
        // Datas fixas (não usar DateTime.UtcNow em teste): estão no passado, logo
        // satisfazem as guardas de "data oficial <= hoje" e "validade > agora".
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);
        private static readonly DateTime ReferenceDate = new(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

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

        /// <summary>
        /// Cria um Document com uma única DocumentUnit sem período e com uma data
        /// oficial válida (hoje) já preenchida, de modo que a unit possa avançar
        /// para os estados que exigem data (AwaitingSignature, RequiresValidation, OK).
        /// A unit começa em Pending → o Document começa em RequiresDocument.
        /// </summary>
        private static (Document doc, DocumentUnit unit, Guid unitId) CreateDocumentWithDatedUnit()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();
            doc.NewDocumentUnit(unitId);
            doc.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "");
            return (doc, doc.GetDocumentUnit(unitId), unitId);
        }

        // -----------------------------------------------------------------
        // Mapeamento foco a foco: cada DocumentUnitStatus (sem período)
        // → DocumentStatus esperado no Document.
        // -----------------------------------------------------------------

        [Fact]
        public void Document_WithNoUnits_ShouldBeOk()
        {
            var doc = CreateDocument();

            Assert.Empty(doc.DocumentsUnits);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        [Fact]
        public void PendingUnit_ShouldMakeDocumentRequiresDocument()
        {
            var (doc, unit, _) = CreateDocumentWithDatedUnit();

            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
        }

        [Fact]
        public void NotApplicableUnit_ShouldMakeDocumentOk()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();

            var changed = doc.MarkAsNotApplicableDocumentUnit(unitId);

            Assert.True(changed);
            Assert.Equal(DocumentUnitStatus.NotApplicable, unit.Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        [Fact]
        public void InvalidUnit_ShouldMakeDocumentRequiresDocument()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();

            doc.MarkAsInvalidDocumentUnit(unitId);

            Assert.Equal(DocumentUnitStatus.Invalid, unit.Status);
            // Invalid não é tratado por nenhum caso do GetStatusFromGroup → cai em RequiresDocument.
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
        }

        [Fact]
        public void AwaitingSignatureUnit_ShouldMakeDocumentAwaitingSignature()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();

            doc.MarkAsAwaitingDocumentUnitSignature(unitId);

            Assert.Equal(DocumentUnitStatus.AwaitingSignature, unit.Status);
            Assert.Equal(DocumentStatus.AwaitingSignature, doc.Status);
        }

        [Fact]
        public void RequiresValidationUnit_ShouldMakeDocumentRequiresValidation()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();

            doc.InsertUnitWithRequireValidation(unitId, "arquivo", "pdf");

            Assert.Equal(DocumentUnitStatus.RequiresValidation, unit.Status);
            Assert.Equal(DocumentStatus.RequiresValidation, doc.Status);
        }

        [Fact]
        public void OkUnit_ShouldMakeDocumentOk()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();

            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");

            Assert.Equal(DocumentUnitStatus.OK, unit.Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        [Fact]
        public void WarningUnit_ShouldMakeDocumentWarning()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf"); // OK é pré-requisito para Warning

            var changed = doc.MakeAsWarning(unitId);

            Assert.True(changed);
            Assert.Equal(DocumentUnitStatus.Warning, unit.Status);
            Assert.Equal(DocumentStatus.Warning, doc.Status);
        }

        [Fact]
        public void DeprecatedUnit_ShouldMakeDocumentDeprecated()
        {
            var (doc, unit, unitId) = CreateDocumentWithDatedUnit();
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf"); // OK → Deprecated

            doc.MakeAsDeprecated();

            Assert.Equal(DocumentUnitStatus.Deprecated, unit.Status);
            Assert.Equal(DocumentStatus.Deprecated, doc.Status);
        }

        // -----------------------------------------------------------------
        // Ciclo de vida completo de uma única unit sem período, verificando
        // a cada passo tanto o DocumentUnitStatus quanto o DocumentStatus.
        //
        // Pending → AwaitingSignature → RequiresValidation → OK → Warning → Deprecated
        // -----------------------------------------------------------------

        [Fact]
        public void FullLifecycle_NonPeriodUnit_ShouldTransitionUnitAndDocumentThroughAllStatuses()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            // 1. Pending → Document RequiresDocument
            doc.NewDocumentUnit(unitId);
            var unit = doc.GetDocumentUnit(unitId);
            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);

            // Data oficial válida para permitir avançar de estado.
            doc.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "");
            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);

            // 2. AwaitingSignature → Document AwaitingSignature
            doc.MarkAsAwaitingDocumentUnitSignature(unitId);
            Assert.Equal(DocumentUnitStatus.AwaitingSignature, unit.Status);
            Assert.Equal(DocumentStatus.AwaitingSignature, doc.Status);

            // 3. RequiresValidation → Document RequiresValidation
            doc.InsertUnitWithRequireValidation(unitId, "arquivo", "pdf");
            Assert.Equal(DocumentUnitStatus.RequiresValidation, unit.Status);
            Assert.Equal(DocumentStatus.RequiresValidation, doc.Status);

            // 4. OK → Document OK (MaskAsValid usa Name/Extension setados no passo 3)
            doc.MarkAsValidDocumentUnit(unitId);
            Assert.Equal(DocumentUnitStatus.OK, unit.Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);

            // 5. Warning → Document Warning
            doc.MakeAsWarning(unitId);
            Assert.Equal(DocumentUnitStatus.Warning, unit.Status);
            Assert.Equal(DocumentStatus.Warning, doc.Status);

            // 6. Deprecated → Document Deprecated
            doc.MakeAsDeprecated();
            Assert.Equal(DocumentUnitStatus.Deprecated, unit.Status);
            Assert.Equal(DocumentStatus.Deprecated, doc.Status);
        }

        // -----------------------------------------------------------------
        // Regra "OK domina o grupo": um grupo com pelo menos uma unit OK
        // resulta em Document OK, mesmo com outras units em estados piores.
        // -----------------------------------------------------------------

        [Fact]
        public void Group_WithOkAndPendingUnits_ShouldBeOk()
        {
            var doc = CreateDocument();

            var okId = Guid.NewGuid();
            doc.NewDocumentUnit(okId);
            doc.UpdateDocumentUnitDetails(okId, OfficialDate, TimeSpan.Zero, "");
            doc.InsertUnitWithoutRequireValidation(okId, "arquivo", "pdf"); // OK

            var pendingId = Guid.NewGuid();
            doc.NewDocumentUnit(pendingId); // Pending

            Assert.Equal(DocumentUnitStatus.OK, doc.GetDocumentUnit(okId).Status);
            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(pendingId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        // -----------------------------------------------------------------
        // Caminho por período (RefreshDocumentStatus agrupa por Period).
        // Neste ramo apenas RequiresDocument / RequiresValidation / Warning / OK
        // são considerados; AwaitingSignature e Deprecated colapsam para OK.
        // -----------------------------------------------------------------

        [Fact]
        public void PeriodDocument_WithPendingUnit_ShouldBeRequiresDocument()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            var unit = doc.NewDocumentUnit(unitId, PeriodType.Monthly, false, ReferenceDate);

            Assert.Equal(DocumentUnitStatus.Pending, unit.Status);
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
        }

        [Fact]
        public void PeriodDocument_TwoGroups_OneOkOnePending_ShouldPrioritizeRequiresDocument()
        {
            var doc = CreateDocument();
            var referenceDate = ReferenceDate;

            // Grupo A (mês corrente): unit OK
            var okId = Guid.NewGuid();
            doc.NewDocumentUnit(okId, PeriodType.Monthly, false, referenceDate);
            doc.InsertUnitWithoutRequireValidation(okId, "arquivo", "pdf");

            // Grupo B (mês seguinte): unit Pending
            var pendingId = Guid.NewGuid();
            doc.NewDocumentUnit(pendingId, PeriodType.Monthly, false, referenceDate.AddMonths(1));

            Assert.Equal(DocumentUnitStatus.OK, doc.GetDocumentUnit(okId).Status);
            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(pendingId).Status);
            // Grupo B resolve para RequiresDocument, que tem prioridade sobre o OK do grupo A.
            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
        }

        [Fact]
        public void PeriodDocument_WithRequiresValidationUnit_ShouldBeRequiresValidation()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            doc.NewDocumentUnit(unitId, PeriodType.Monthly, false, ReferenceDate);
            doc.InsertUnitWithRequireValidation(unitId, "arquivo", "pdf");

            Assert.Equal(DocumentUnitStatus.RequiresValidation, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.RequiresValidation, doc.Status);
        }

        [Fact]
        public void PeriodDocument_WithOkUnit_ShouldBeOk()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            doc.NewDocumentUnit(unitId, PeriodType.Monthly, false, ReferenceDate);
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");

            Assert.Equal(DocumentUnitStatus.OK, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }

        // Regressão do ramo por competência: ele tinha uma precedência própria que só olhava
        // RequiresDocument/RequiresValidation/Warning e mandava todo o resto para OK. Documento periodizado
        // aguardando assinatura aparecia como OK, e o mesmo valia para vencido e depreciado.
        [Fact]
        public void PeriodDocument_WithAwaitingSignatureUnit_ShouldMakeDocumentAwaitingSignature()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            doc.NewDocumentUnit(unitId, PeriodType.Monthly, false, ReferenceDate);
            doc.MarkAsAwaitingDocumentUnitSignature(unitId);

            Assert.Equal(DocumentUnitStatus.AwaitingSignature, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.AwaitingSignature, doc.Status);
        }

        [Fact]
        public void PeriodDocument_WithExpiredUnit_ShouldMakeDocumentExpired()
        {
            var doc = CreateDocument();
            var unitId = Guid.NewGuid();

            doc.NewDocumentUnit(unitId, PeriodType.Monthly, false, ReferenceDate);
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", "pdf");
            doc.ExpireDocumentUnit(unitId);

            Assert.Equal(DocumentUnitStatus.Expired, doc.GetDocumentUnit(unitId).Status);
            Assert.Equal(DocumentStatus.Expired, doc.Status);
        }

        // A renovação de um documento por competência cai na competência SEGUINTE, então é ela que supera a
        // vencida — exigir mesma competência deixaria a vencida esperando para sempre.
        [Fact]
        public void PeriodDocument_WhenNextPeriodIsDelivered_ShouldDeprecateTheExpiredUnit()
        {
            var doc = CreateDocument();
            var expiredUnitId = Guid.NewGuid();
            var okUnitId = Guid.NewGuid();

            doc.NewDocumentUnit(expiredUnitId, PeriodType.Monthly, false, ReferenceDate);
            doc.InsertUnitWithoutRequireValidation(expiredUnitId, "arquivo", "pdf");
            doc.ExpireDocumentUnit(expiredUnitId);

            doc.NewDocumentUnit(okUnitId, PeriodType.Monthly, false, ReferenceDate.AddMonths(1));
            doc.InsertUnitWithoutRequireValidation(okUnitId, "arquivo2", "pdf");

            // A entrega da competência seguinte é o substituto que a vencida esperava.
            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(expiredUnitId).Status);
            Assert.Equal(DocumentStatus.OK, doc.Status);
        }
    }
}
