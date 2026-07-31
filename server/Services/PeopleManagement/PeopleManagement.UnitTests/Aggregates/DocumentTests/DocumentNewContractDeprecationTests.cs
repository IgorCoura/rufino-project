using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.UnitTests.Aggregates.DocumentTests.Mothers;

namespace PeopleManagement.UnitTests.Aggregates.DocumentTests
{
    /// <summary>
    /// <see cref="Document.DeprecateDeliveredUnits"/>: a depreciação disparada pelo início de um novo contrato
    /// de trabalho. Entregue = OK ou Warning; o que ainda está em curso (pendente, aguardando assinatura,
    /// requer validação) atravessa intacto, e o que já saiu de cena não é mexido de novo.
    /// </summary>
    public class DocumentNewContractDeprecationTests
    {
        // Data fixa no passado: satisfaz as guardas de "data oficial <= hoje" das transições usadas aqui.
        private static readonly DateOnly OfficialDate = new(2024, 1, 15);

        /// <summary>
        /// Documento com uma unidade entregue (OK). A unidade OK nasce <b>primeiro</b> de propósito:
        /// InsertUnitWithoutRequireValidation deprecia as demais unidades do documento, então qualquer
        /// companheira criada antes dela seria depreciada no setup e o teste mediria a coisa errada.
        /// </summary>
        private static (Document doc, Guid unitId) DocumentWithOkUnit()
        {
            var doc = DocumentMother.Simple();
            var unitId = AddDatedUnit(doc);
            doc.InsertUnitWithoutRequireValidation(unitId, "arquivo", Extension.PDF);
            return (doc, unitId);
        }

        private static Guid AddDatedUnit(Document doc)
        {
            var unitId = Guid.NewGuid();
            doc.NewDocumentUnit(unitId);
            doc.UpdateDocumentUnitDetails(unitId, OfficialDate, TimeSpan.Zero, "");
            return unitId;
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithOkUnit_ShouldDeprecateItAndReportOne()
        {
            var (doc, unitId) = DocumentWithOkUnit();

            var deprecated = doc.DeprecateDeliveredUnits();

            Assert.Equal(1, deprecated);
            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(unitId).Status);
        }

        // Warning é uma unidade OK que está vencendo — se sobrevivesse ao novo contrato, ao vencer seria
        // renovada já sob o vínculo errado.
        [Fact]
        public void DeprecateDeliveredUnits_WithWarningUnit_ShouldDeprecateIt()
        {
            var (doc, unitId) = DocumentWithOkUnit();
            doc.MakeAsWarning(unitId);

            var deprecated = doc.DeprecateDeliveredUnits();

            Assert.Equal(1, deprecated);
            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(unitId).Status);
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithPendingUnit_ShouldLeaveItPending()
        {
            var (doc, okUnitId) = DocumentWithOkUnit();
            var pendingUnitId = AddDatedUnit(doc);

            var deprecated = doc.DeprecateDeliveredUnits();

            Assert.Equal(1, deprecated);
            Assert.Equal(DocumentUnitStatus.Deprecated, doc.GetDocumentUnit(okUnitId).Status);
            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(pendingUnitId).Status);
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithUnitAwaitingSignature_ShouldLeaveItAwaiting()
        {
            var (doc, _) = DocumentWithOkUnit();
            var awaitingUnitId = AddDatedUnit(doc);
            doc.MarkAsAwaitingDocumentUnitSignature(awaitingUnitId);

            doc.DeprecateDeliveredUnits();

            Assert.Equal(DocumentUnitStatus.AwaitingSignature, doc.GetDocumentUnit(awaitingUnitId).Status);
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithUnitRequiringValidation_ShouldLeaveItRequiringValidation()
        {
            var (doc, _) = DocumentWithOkUnit();
            var toValidateUnitId = AddDatedUnit(doc);
            doc.InsertUnitWithRequireValidation(toValidateUnitId, "enviado", Extension.PDF);

            doc.DeprecateDeliveredUnits();

            Assert.Equal(DocumentUnitStatus.RequiresValidation, doc.GetDocumentUnit(toValidateUnitId).Status);
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithoutDeliveredUnits_ShouldReportZero()
        {
            var doc = DocumentMother.Simple();
            AddDatedUnit(doc);

            var deprecated = doc.DeprecateDeliveredUnits();

            Assert.Equal(0, deprecated);
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithNoUnits_ShouldReportZero()
        {
            var doc = DocumentMother.Simple();

            Assert.Equal(0, doc.DeprecateDeliveredUnits());
        }

        // Uma segunda admissão não pode recontar o que já foi depreciado na primeira: o contador alimenta o log
        // e, se inflasse, mentiria sobre quantos documentos o novo contrato invalidou.
        [Fact]
        public void DeprecateDeliveredUnits_CalledTwice_ShouldReportZeroOnSecondCall()
        {
            var (doc, _) = DocumentWithOkUnit();

            doc.DeprecateDeliveredUnits();

            Assert.Equal(0, doc.DeprecateDeliveredUnits());
        }

        [Fact]
        public void DeprecateDeliveredUnits_WithOnlyDeliveredUnit_ShouldLeaveDocumentDeprecated()
        {
            var (doc, _) = DocumentWithOkUnit();

            doc.DeprecateDeliveredUnits();

            Assert.Equal(DocumentStatus.Deprecated, doc.Status);
        }

        // Com uma pendente ao lado da depreciada o documento fica RequiresDocument: depreciada é histórico com
        // substituto, e o substituto aqui é justamente a pendente do novo contrato — o que falta é entregá-la.
        // Diferente de uma unidade VENCIDA sem substituto, que deixa o documento Expired.
        [Fact]
        public void DeprecateDeliveredUnits_WithPendingLeftBehind_ShouldLeaveDocumentRequiringTheNewOne()
        {
            var (doc, _) = DocumentWithOkUnit();
            var pendingUnitId = AddDatedUnit(doc);

            doc.DeprecateDeliveredUnits();

            Assert.Equal(DocumentStatus.RequiresDocument, doc.Status);
            Assert.Equal(DocumentUnitStatus.Pending, doc.GetDocumentUnit(pendingUnitId).Status);
        }
    }
}
