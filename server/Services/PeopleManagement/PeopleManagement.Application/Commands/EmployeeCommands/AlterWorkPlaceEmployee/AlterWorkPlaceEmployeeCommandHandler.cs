
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterWorkPlaceEmployee
{
    public sealed class AlterWorkPlaceEmployeeCommandHandler : IRequestHandler<AlterWorkPlaceEmployeeCommand, AlterWorkPlaceEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterWorkPlaceEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterWorkPlaceEmployeeResponse> Handle(AlterWorkPlaceEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            employee.WorkPlaceId = request.WorkPlaceId;

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
