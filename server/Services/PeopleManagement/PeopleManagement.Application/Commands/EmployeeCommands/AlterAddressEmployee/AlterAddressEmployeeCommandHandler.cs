using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Interfaces;
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
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.Address = request.ToAddress();

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }
    public class AlterAddressEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterAddressEmployeeCommand, AlterAddressEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterAddressEmployeeCommand, AlterAddressEmployeeResponse>(mediator, logger)
    {
        protected override AlterAddressEmployeeResponse CreateResultForDuplicateRequest() => new (Guid.Empty);
        
    }
}


