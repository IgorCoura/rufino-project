using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee
{
    public sealed class AlterContactEmployeeCommandHandler : IRequestHandler<AlterContactEmployeeCommand, AlterContactEmployeeResponse>
    {
        public readonly IEmployeeRepository _employeeRepository;

        public AlterContactEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterContactEmployeeResponse> Handle(AlterContactEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            employee.Contact = Contact.Create(request.Email, request.CellPhone);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
