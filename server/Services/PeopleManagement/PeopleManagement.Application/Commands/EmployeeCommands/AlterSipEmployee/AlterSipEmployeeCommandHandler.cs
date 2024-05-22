
using PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterSipEmployee
{
    public class AlterSipEmployeeCommandHandler : IRequestHandler<AlterSipEmployeeCommand, AlterSipEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterSipEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterSipEmployeeResponse> Handle(AlterSipEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.Sip = request.SipNumber;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterRoleEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>(mediator, logger)
    {
        protected override AlterRoleEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}
