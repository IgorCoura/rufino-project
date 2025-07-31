using Microsoft.Extensions.Logging;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Events;
using PeopleManagement.Domain.AggregatesModel.RequireDocumentsAggregate.Interfaces;

namespace PeopleManagement.Services.Services
{
    public class RecurringDocumentService(
        IRequireDocumentsRepository requireDocumentsRepository, 
        IDocumentRepository documentRepository, 
        IEmployeeRepository employeeRepository, 
        ISignService signService,
        ILogger<RecurringDocumentService> logger) 
        : IRecurringDocumentService
    {
        private readonly IRequireDocumentsRepository _requireDocumentsRepository = requireDocumentsRepository;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        private readonly ISignService _signService = signService;
        private readonly ILogger<RecurringDocumentService> _logger = logger;

        public async Task RecurringCreateDocumentUnits(RecurringEvents recurringEvent, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Recurring CreateDocument Units Init - event: {recurringEvent}");

            var requiedDocuments = await _requireDocumentsRepository.GetAllWithEventId(recurringEvent.Id, cancellationToken);

            foreach (var requiedDocument in requiedDocuments)
            {
                var documents = await _documentRepository.GetDataAsync(x => x.RequiredDocumentId == requiedDocument.Id,
                    cancellation: cancellationToken);

                foreach (var document in documents) {
           

                    Employee? employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == document.EmployeeId 
                        && x.CompanyId == document.CompanyId, cancellation: cancellationToken);

                    if(employee is null)
                    {
                        _logger.LogError("Employee with ID {EmployeeId} not found for document {DocumentId}. Skipping document creation.",
                            document.EmployeeId, document.Id);
                        continue;
                    }

                    var isAccepted = requiedDocument.StatusIsAccepted(recurringEvent.Id, employee.Status.Id);

                    if (isAccepted == false)
                        continue;

                    var documentUnitId = Guid.NewGuid();

                    document!.NewDocumentUnit(documentUnitId);

                }
                
            }
            await _documentRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Recurring CreateDocument Units Complete - event: {recurringEvent}");

        }

        public async Task RefreshWebHook(CancellationToken cancellationToken = default)
        {
            await _signService.RefreshWebHookDocSignedEvent(cancellationToken);
        }
    }
}
