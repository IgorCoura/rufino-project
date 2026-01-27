using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using PeopleManagement.Infra.Idempotency;

namespace PeopleManagement.Application.Commands.EmployeeCommands.MarkEmployeeAsInactive
{
    public sealed class MarkEmployeeAsInactiveCommandHandler : IRequestHandler<MarkEmployeeAsInactiveCommand, MarkEmployeeAsInactiveResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public MarkEmployeeAsInactiveCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<MarkEmployeeAsInactiveResponse> Handle(MarkEmployeeAsInactiveCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.MarkAsInactive();

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class MarkEmployeeAsInactiveIdentifiedCommandHandler(IMediator mediator, 
        ILogger<IdentifiedCommandHandler<MarkEmployeeAsInactiveCommand, MarkEmployeeAsInactiveResponse>> logger, IRequestManager requestManager) 
        : IdentifiedCommandHandler<MarkEmployeeAsInactiveCommand, MarkEmployeeAsInactiveResponse>(mediator, logger, requestManager)
    {
        protected override MarkEmployeeAsInactiveResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}

