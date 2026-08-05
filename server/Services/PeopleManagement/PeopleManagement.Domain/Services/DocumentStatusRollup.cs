using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;

namespace PeopleManagement.Domain.Services
{
    /// <summary>
    /// A regra ÚNICA que resume um conjunto de documentos num único indicador de conformidade.
    ///
    /// Usada em três lugares que precisam concordar: o status materializado do funcionário
    /// (<see cref="IEmployeeDocumentStatusService"/>), o status do grupo de documentos e o do documento exigido.
    /// Existiam duas implementações desta mesma regra e elas discordavam sobre
    /// <see cref="DocumentStatus.Deprecated"/> — o funcionário aparecia Okay enquanto o grupo aparecia como
    /// problema para exatamente o mesmo documento.
    ///
    /// Cruza dois aggregates (lê Document, devolve indicador do Employee), então mora em Services e recebe
    /// valores, nunca entidades.
    /// </summary>
    public static class DocumentStatusRollup
    {
        /// <summary>
        /// Resume [documentStatuses] no indicador do funcionário. Sem documentos não há o que cobrar: Okay.
        ///
        /// <see cref="DocumentStatus.Deprecated"/> conta como Okay porque, por definição, uma unidade depreciada
        /// já tem substituto — o que representa risco é <see cref="DocumentStatus.Expired"/>, que é a mesma saída
        /// de vigência sem substituto.
        /// </summary>
        public static EmployeeDocumentStatus Summarize(IEnumerable<DocumentStatus>? documentStatuses)
        {
            if (documentStatuses is null)
                return EmployeeDocumentStatus.Okay;

            var worst = documentStatuses.MinBy(DocumentStatus.Severity);

            if (worst is null)
                return EmployeeDocumentStatus.Okay;

            if (worst == DocumentStatus.Expired ||
                worst == DocumentStatus.RequiresDocument ||
                worst == DocumentStatus.RequiresValidation)
                return EmployeeDocumentStatus.RequiresAttention;

            if (worst == DocumentStatus.Warning)
                return EmployeeDocumentStatus.Warning;

            // OK, AwaitingSignature (fluxo em curso) e Deprecated (histórico com substituto).
            return EmployeeDocumentStatus.Okay;
        }
    }
}
