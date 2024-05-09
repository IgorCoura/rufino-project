
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

            return employee.Id;
        }
    }
}
