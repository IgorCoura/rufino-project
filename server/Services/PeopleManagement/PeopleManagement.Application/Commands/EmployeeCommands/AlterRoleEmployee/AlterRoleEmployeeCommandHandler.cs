
using PeopleManagement.Application.Commands.EmployeeCommands.AlterPersonalInfoEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterRoleEmployee
{
    public sealed class AlterRoleEmployeeCommandHandler : IRequestHandler<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterRoleEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterRoleEmployeeResponse> Handle(AlterRoleEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.RoleId = request.RoleId;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
    public class AlterRoleEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterRoleEmployeeCommand, AlterRoleEmployeeResponse>(mediator, logger)
    {
        protected override AlterRoleEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }

}

