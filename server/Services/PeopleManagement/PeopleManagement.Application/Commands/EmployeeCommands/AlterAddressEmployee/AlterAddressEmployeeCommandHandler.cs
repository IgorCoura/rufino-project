
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using EmployeeAddress = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Address;

namespace PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee
{
    public sealed class AlterAddressEmployeeCommandHandler : IRequestHandler<AlterAddressEmployeeCommand, AlterAddressEmployeeResponse>
    {
        private readonly IEmployeeRepository _employeeRepository;

        public AlterAddressEmployeeCommandHandler(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository;
        }

        public async Task<AlterAddressEmployeeResponse> Handle(AlterAddressEmployeeCommand request, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(request.Id, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.Id.ToString()));

            employee.Address = EmployeeAddress.Create(
                request.ZipCode, 
                request.Street, 
                request.Number, 
                request.Complement, 
                request.Neighborhood, 
                request.City, 
                request.State, 
                request.Country);

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
}
