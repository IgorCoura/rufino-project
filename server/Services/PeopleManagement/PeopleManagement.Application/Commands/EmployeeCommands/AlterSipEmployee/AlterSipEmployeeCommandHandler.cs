
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
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            employee.Sip = request.SipNumber;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
