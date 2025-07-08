using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.DocumentSigningOptions
{
    public class DocumentSigningOptionsCommandHandler(IEmployeeRepository employeeRepository) : IRequestHandler<DocumentSigningOptionsCommand, DocumentSigningOptionsResponse>
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;
        public async Task<DocumentSigningOptionsResponse> Handle(DocumentSigningOptionsCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));
            employee.DocumentSigningOptions = request.DocumentSigningOptions;
            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);
            return employee.Id;
        }
    }
    public class DocumentSigningOptionsIdentifiedCommandHandler(IMediator mediator,
        ILogger<IdentifiedCommandHandler<DocumentSigningOptionsCommand, DocumentSigningOptionsResponse>> logger, IRequestManager requestManager)
        : IdentifiedCommandHandler<DocumentSigningOptionsCommand, DocumentSigningOptionsResponse>(mediator, logger, requestManager)
    {
        protected override DocumentSigningOptionsResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}
