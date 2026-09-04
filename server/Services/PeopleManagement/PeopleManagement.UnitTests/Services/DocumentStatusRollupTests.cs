using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.UnitTests.Services
{
    /// <summary>
    /// Cobre a regra única que resume documentos num indicador de conformidade — a mesma consumida pelo status
    /// materializado do funcionário, pelo status do grupo de documentos e pelo do documento exigido.
    ///
    /// Antes existiam duas implementações e elas discordavam sobre Deprecated: o funcionário aparecia Okay
    /// enquanto o grupo aparecia como problema, para exatamente o mesmo documento.
    /// </summary>
    public class DocumentStatusRollupTests
    {
        [Fact]
        public void Summarize_WithoutDocuments_ShouldBeOkay()
        {
            Assert.Equal(EmployeeDocumentStatus.Okay, DocumentStatusRollup.Summarize([]));
        }

        [Fact]
        public void Summarize_WithNull_ShouldBeOkay()
        {
            Assert.Equal(EmployeeDocumentStatus.Okay, DocumentStatusRollup.Summarize(null));
        }

        [Theory]
        [InlineData(nameof(DocumentStatus.Expired))]
        [InlineData(nameof(DocumentStatus.RequiresDocument))]
        [InlineData(nameof(DocumentStatus.RequiresValidation))]
        public void Summarize_WithUncoveredDocument_ShouldRequireAttention(string statusName)
        {
            var status = (DocumentStatus)statusName;

            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, DocumentStatusRollup.Summarize([status]));
        }

        [Fact]
        public void Summarize_WithWarningDocument_ShouldWarn()
        {
            Assert.Equal(EmployeeDocumentStatus.Warning, DocumentStatusRollup.Summarize([DocumentStatus.Warning]));
        }

        [Theory]
        [InlineData(nameof(DocumentStatus.OK))]
        [InlineData(nameof(DocumentStatus.AwaitingSignature))]
        [InlineData(nameof(DocumentStatus.Deprecated))]
        public void Summarize_WithCoveredOrSettledDocument_ShouldBeOkay(string statusName)
        {
            var status = (DocumentStatus)statusName;

            Assert.Equal(EmployeeDocumentStatus.Okay, DocumentStatusRollup.Summarize([status]));
        }

        // O ponto que as duas implementações antigas contradiziam: um documento vencido sem renovação deixava o
        // funcionário Okay na listagem enquanto o grupo dele aparecia como problema.
        [Fact]
        public void Summarize_WithExpiredAmongOkDocuments_ShouldRequireAttention()
        {
            var statuses = new[] { DocumentStatus.OK, DocumentStatus.Expired, DocumentStatus.OK };

            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, DocumentStatusRollup.Summarize(statuses));
        }

        [Fact]
        public void Summarize_ShouldTakeTheWorstDocument()
        {
            var statuses = new[] { DocumentStatus.OK, DocumentStatus.Warning, DocumentStatus.RequiresDocument };

            Assert.Equal(EmployeeDocumentStatus.RequiresAttention, DocumentStatusRollup.Summarize(statuses));
        }

        [Fact]
        public void Summarize_WithWarningAmongOkDocuments_ShouldWarn()
        {
            var statuses = new[] { DocumentStatus.OK, DocumentStatus.Warning, DocumentStatus.Deprecated };

            Assert.Equal(EmployeeDocumentStatus.Warning, DocumentStatusRollup.Summarize(statuses));
        }
    }
}
