using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.DocumentCommands.BatchGeneratePdf
{
    public class BatchGeneratePdfCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository,
        IEmployeeRepository employeeRepository
    ) : IRequestHandler<BatchGeneratePdfCommand, BatchGeneratePdfResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<BatchGeneratePdfResponse> Handle(BatchGeneratePdfCommand request, CancellationToken cancellationToken)
        {
            var allResults = new List<BatchGeneratePdfResponseItem>();

            var groupedByEmployee = request.Items
                .GroupBy(i => i.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var (employeeId, items) in groupedByEmployee)
            {
                var employee = await _employeeRepository.FirstOrDefaultAsync(
                    e => e.Id == employeeId && e.CompanyId == request.CompanyId,
                    cancellation: cancellationToken)
                    ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), employeeId.ToString()));

                var rangeItems = items
                    .GroupBy(i => i.DocumentId)
                    .Select(g => (g.Key, g.Select(i => i.DocumentUnitId).AsEnumerable()));

                var results = await _documentService.GeneratePdfRange(
                    rangeItems, employeeId, request.CompanyId, cancellationToken);

                foreach (var r in results)
                {
                    allResults.Add(new BatchGeneratePdfResponseItem(
                        r.DocumentUnitId, r.DocumentId,
                        employee.Name.Value, r.DocumentName, r.DocumentUnitDate, r.Pdf));
                }
            }

            return new BatchGeneratePdfResponse(allResults);
        }
    }
}
