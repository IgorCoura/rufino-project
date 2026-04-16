using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdfRange
{
    public class GeneratePdfRangeCommandHandler(
        IDocumentService documentService,
        IEmployeeRepository employeeRepository)
        : IRequestHandler<GeneratePdfRangeCommand, GeneratePdfRangeResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<GeneratePdfRangeResponse> Handle(GeneratePdfRangeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(
                e => e.Id == request.EmployeeId && e.CompanyId == request.CompanyId,
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var results = await _documentService.GeneratePdfRange(
                request.Items.Select(x => (x.DocumentId, x.DocumentUnitIds)),
                request.EmployeeId,
                request.CompanyId,
                cancellationToken);

            return new GeneratePdfRangeResponse(
                employee.Name.Value,
                results.Select(r => new GeneratePdfRangeResponseItem(r.DocumentUnitId, r.DocumentId, r.DocumentName, r.DocumentUnitDate, r.Pdf)).ToList());
        }
    }
}
