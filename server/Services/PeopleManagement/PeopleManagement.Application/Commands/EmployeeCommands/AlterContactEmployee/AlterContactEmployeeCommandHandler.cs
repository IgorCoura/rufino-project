using PeopleManagement.Application.Commands.EmployeeCommands.AlterAddressEmployee;
using PeopleManagement.Application.Commands.EmployeeCommands.AlterContactEmployee;
using PeopleManagement.Application.Commands.Identified;
using PeopleManagement.Domain.AggregatesModel.EmployeeAggregate;
using PeopleManagement.Domain.ErrorTools;
using PeopleManagement.Domain.ErrorTools.ErrorsMessages;
using Contact = PeopleManagement.Domain.AggregatesModel.EmployeeAggregate.Contact;

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
            var employee = await _employeeRepository.FirstOrDefaultAsync(x => x.Id == request.EmployeeId && x.CompanyId == request.CompanyId, cancellation: cancellationToken)
                ?? throw new DomainException(this, DomainErrors.ObjectNotFound(nameof(Employee), request.EmployeeId.ToString()));

            employee.Contact = request.ToContact();

            await _employeeRepository.UnitOfWork.SaveChangesAsync(cancellationToken);

            return employee.Id;
        }
    }

    public class AlterContactEmployeeIdentifiedCommandHandler(IMediator mediator, ILogger<IdentifiedCommandHandler<AlterContactEmployeeCommand, AlterContactEmployeeResponse>> logger) : IdentifiedCommandHandler<AlterContactEmployeeCommand, AlterContactEmployeeResponse>(mediator, logger)
    {
        protected override AlterContactEmployeeResponse CreateResultForDuplicateRequest() => new(Guid.Empty);
    }
}

