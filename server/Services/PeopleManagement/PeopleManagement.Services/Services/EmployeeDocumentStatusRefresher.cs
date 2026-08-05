using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.Services;

namespace PeopleManagement.Services.Services
{
    /// <summary>
    /// Recalcula o status de documentação materializado no funcionário a partir dos documentos dele.
    ///
    /// Existe separado dos handlers porque há mais de um caminho que muda o conjunto de documentos: a mudança de
    /// status de um documento e a EXCLUSÃO de documentos que deixaram de ser exigidos (troca de cargo/setor). Sem
    /// o segundo, o funcionário ficava com "requer atenção" por causa de um documento que não existe mais.
    ///
    /// Não salva: quem chama decide. Nos despachos de evento de domínio o SaveChanges do UnitOfWork que disparou
    /// o evento ainda vai acontecer, e salvar aqui re-despacharia os eventos ainda na fila.
    /// </summary>
    public interface IEmployeeDocumentStatusRefresher
    {
        Task RefreshAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default);
    }

    public class EmployeeDocumentStatusRefresher(
        ILogger<EmployeeDocumentStatusRefresher> logger,
        IDocumentRepository documentRepository,
        IEmployeeRepository employeeRepository) : IEmployeeDocumentStatusRefresher
    {
        private readonly ILogger<EmployeeDocumentStatusRefresher> _logger = logger;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task RefreshAsync(Guid employeeId, Guid companyId, CancellationToken cancellationToken = default)
        {
            var documentStatuses = await _documentRepository.GetAllStatusByEmployeeAsync(employeeId, companyId, cancellationToken);

            var employee = await _employeeRepository.FirstOrDefaultAsync(
                x => x.Id == employeeId && x.CompanyId == companyId,
                cancellation: cancellationToken);

            if (employee is null)
            {
                _logger.LogWarning(
                    "Employee not found when trying to update document status. EmployeeId: {EmployeeId}, CompanyId: {CompanyId}",
                    employeeId, companyId);
                return;
            }

            employee.UpdateDocumentRepresentingStatus(DocumentStatusRollup.Summarize(documentStatuses));

            _logger.LogInformation(
                "Employee document representing status updated. EmployeeId: {EmployeeId}, NewStatus: {NewStatus}",
                employeeId, employee.DocumentRepresentingStatus.Name);
        }
    }
}
