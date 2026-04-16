
using Microsoft.EntityFrameworkCore;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate;
using PeopleManagement.Domain.AggregatesModel.DocumentAggregate.Interfaces;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.DocumentCommands.GeneratePdf
{
    public class GeneratePdfCommandHandler(
        IDocumentService documentService,
        IDocumentRepository documentRepository,
        IEmployeeRepository employeeRepository) : IRequestHandler<GeneratePdfCommand, GeneratePdfResponse>
    {
        private readonly IDocumentService _documentService = documentService;
        private readonly IDocumentRepository _documentRepository = documentRepository;
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        public async Task<GeneratePdfResponse> Handle(GeneratePdfCommand request, CancellationToken cancellationToken)
        {
            var pdf = await _documentService.GeneratePdf(request.DocumentUnitId, request.DocumentId, request.EmployeeId, request.CompanyId, cancellationToken);

            var document = await _documentRepository.FirstOrDefaultAsync(
                x => x.Id == request.DocumentId && x.EmployeeId == request.EmployeeId && x.CompanyId == request.CompanyId,
                include: x => x.Include(y => y.DocumentsUnits),
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Document), request.DocumentId.ToString()));

            var employee = await _employeeRepository.FirstOrDefaultAsync(
                e => e.Id == request.EmployeeId && e.CompanyId == request.CompanyId,
                cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            var unit = document.DocumentsUnits.First(x => x.Id == request.DocumentUnitId);

            return new GeneratePdfResponse(request.DocumentUnitId, pdf, employee.Name.Value, document.Name.ToString(), unit.Date);
        }
    }
    public class CreateDocumentIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<GeneratePdfCommand, GeneratePdfResponse>(mediator, logger, requestManager)
    {
        protected override GeneratePdfResponse CreateResultForDuplicateRequest() => new(Guid.Empty, [], string.Empty, string.Empty, default);

    }
}
